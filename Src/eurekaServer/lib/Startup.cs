
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ace.Application;
using Microsoft.Extensions.Configuration;
using Ace;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.FileProviders;
using System.Reflection;
using System.IO;
using Microsoft.Extensions.DependencyModel;
using System.Text;
using Ace.Web.Mvc;
using Microsoft.AspNetCore.Routing;
using Ace.Web.Mvc.Middlewares;
using log4net;
using log4net.Repository;
using log4net.Config;
using System.IO;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.SpaServices.Webpack;
using FrmLib.Extend;
using Microsoft.AspNetCore.Mvc.Razor.Compilation;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Collections.Concurrent;
using FrmLib.Swagger;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.DataProtection;
using System.Net.WebSockets;
using StackExchange.Redis;
using System.Threading;
using System.Xml;

namespace FrmLib.web
{
    
        public abstract class baseStartup
    {
       
        public static baseStartup instance;
         public static ILoggerRepository repository { get; set; }
        IHostingEnvironment _env;
        public IServiceCollection _allservice;
        protected IDistributedCache _distributedCache;
        private static object _locker = new object();
        private static ConnectionMultiplexer _redis;
        private static IList<TimeDoTask> alltasks;
        private bool enableSwaggerSiteKey = false;
        public   static List<Tag> swaggerDocTags = new List<Tag> {
            new Tag { Name = "Common", Description = "登录与公用参数相关接口" }};
        protected abstract  IList<string> swaggerXmlList { get; set; }
        protected abstract string appName { get; set; }
        protected abstract string deverName { get; set; }
        public baseStartup(IHostingEnvironment env,bool enableSwaggerSiteKey=false)
        {
            this._env = env;
            this.enableSwaggerSiteKey = enableSwaggerSiteKey;
            var builder = new ConfigurationBuilder()
                      .SetBasePath(env.ContentRootPath)
                      .AddJsonFile(Path.Combine("configs", "appsettings.json"), optional: true, reloadOnChange: true)  // Settings for the application
                      //.AddJsonFile(Path.Combine("configs", $"appsettings.{env.EnvironmentName}.json"), optional: true, reloadOnChange: true)
                      //.AddXmlFile(Path.Combine("configs", "access_auth.config"), optional: true, reloadOnChange: true)
                      .AddEnvironmentVariables();                                              // override settings with environment variables set in compose.   

            //if (env.IsDevelopment())
            //{
            //    // For more details on using the user secret store see http://go.microsoft.com/fwlink/?LinkID=532709
            //    builder.AddUserSecrets();
            //}
            string logrepositoryname = Assembly.GetEntryAssembly().GetName().Name;
            repository = LogManager.CreateRepository(Assembly.GetEntryAssembly(),
            typeof(log4net.Repository.Hierarchy.Hierarchy));
            XmlConfigurator.Configure(repository, new FileInfo(Path.Combine(env.ContentRootPath, "configs", "log4net.config")));
            Configuration = builder.Build();
            Globals.Configuration = Configuration;

            
           
           
           
            baseStartup.instance = this;

           

        }
        /// <summary>
        /// 用于扩展Configure中使用的函数
        /// </summary>
        /// <param name="env"></param>
        public virtual void initOtherConfig(IApplicationBuilder app, IHostingEnvironment env)
        {
        }
        /// <summary>
        /// 用于扩展需要在ConfigureServices中使用的功能
        /// </summary>
        /// <param name="services"></param>
        public virtual void initOtherServices(IServiceCollection services)
        {
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            _allservice = services;
            services.AddDistributedRedisCache(options =>
            {
                options.Configuration = Globals.Configuration["redis:connections"];
                options.InstanceName =Assembly.GetEntryAssembly().FullName;

            });
            FrmLib.Log.commLoger.runLoger.InfoFormat("now configure main service");
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            Console.WriteLine(AppDomain.CurrentDomain.BaseDirectory);

            services.AddCors();
            services.AddResponseCaching(); //https://docs.microsoft.com/en-us/aspnet/core/performance/caching/response

            initJwtWithCookie(services); //使用jwt和cookie混合


            string accessConfig = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "configs", "access_auth.config");
            var strtmp = Globals.Configuration["swaggerDoc:enable"];
            bool enableswagger = false;
            if (string.IsNullOrEmpty(strtmp))
                enableswagger = false;
            else
                bool.TryParse(strtmp, out enableswagger);

            if (enableswagger)
                strtmp = Globals.Configuration["swaggerDoc:url"];
            services.AddMvc(options =>
            {
                var policy = new AuthorizationPolicyBuilder(new string[] { CookieAuthenticationDefaults.AuthenticationScheme, JwtBearerDefaults.AuthenticationScheme })
                .RequireAuthenticatedUser()
        .Build();
                options.Filters.Add(new AuthorizeFilter(policy));
                options.Filters.Add(typeof(HttpGlobalExceptionFilter));
                options.Filters.Add(typeof(HttpGlobalActionFilter));
                options.Filters.Add(new HttpGlobalAuthFilter(new AuthorizedFilterOptions
                {
                    isWeb = true,
                    loginUrl = "/Home/Login",
                    errorUrl = "/Home/Error",
                    authorizefilter = new FrmLib.Extend.baseAccess_auth(accessConfig,
                    false, new FrmLib.Extend.IsValidTokenHandle(checkCacheValidToken)),
                    noAuthoReturnok = false,
                    enableSwagger = enableswagger,
                    swaggerUrl = strtmp,
                }));
            }).AddXmlSerializerFormatters()//.AddControllersAsServices()
            .AddRazorOptions(opt =>
            {
                opt.ViewLocationFormats.Add("/Views/Shared/Component/{0}" + RazorViewEngine.ViewExtension);
                
            })
            .ConfigureApplicationPartManager(manager =>
            {
                //移除ASP.NET CORE MVC管理器中默认内置的MetadataReferenceFeatureProvider，该Provider如果不移除，还是会引发InvalidOperationException: Cannot find compilation library location for package 'MyNetCoreLib'这个错误
               // manager.FeatureProviders.Remove(manager.FeatureProviders.First(f => f is MetadataReferenceFeatureProvider));
                //注册我们定义的ReferencesMetadataReferenceFeatureProvider到ASP.NET CORE MVC管理器来代替上面移除的MetadataReferenceFeatureProvider
              //  manager.FeatureProviders.Add(new ReferencesMetadataReferenceFeatureProvider());
            });
            //services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(x =>
            //{
            //    x.ValueLengthLimit = 2147483647;
            //    x.MultipartBodyLengthLimit = 2147483647; //2G
            //});
            services.AddSession();
            initSwagger(services);
            initOtherServices(services);
        }
        protected readonly Action<IRouteBuilder> GetRoutes =
         routes =>
    {
      
        routes.MapRoute(
                  name: "default",
                  template: "{controller=Home}/{action=index}/{id?}");
        routes.MapRoute(
                 name: "swagger",
                 template: "swagger/{controller}/");

        //routes.MapSpaFallbackRoute(
        //    name: "spa-fallback",
        //    defaults: new { controller = "Home", action = "Index" });

    };
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {


            Globals.ServiceProvider = app.ApplicationServices;
           

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                //app.UseWebpackDevMiddleware(new WebpackDevMiddlewareOptions
                //{
                //    HotModuleReplacement = true
                //});
               
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();
            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });
         //   app.UseAuthentication();
            app.UseSession();
            var tmps = Configuration.GetSection("Origins").GetValue<string>("value");
            FrmLib.Log.commLoger.runLoger.DebugFormat(tmps);
            string[] origins = tmps.Split(",", StringSplitOptions.RemoveEmptyEntries);
            app.UseCors(builder =>
                    //  builder.WithOrigins(origins)
                    builder.WithOrigins("*")
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials());
            string accessConfig = Path.Combine(env.ContentRootPath, "configs", "access_auth.config");
            var strtmp = Globals.Configuration["swaggerDoc:enable"];
            bool enableswagger = false;
            if (string.IsNullOrEmpty(strtmp))
                enableswagger = false;
            else
                bool.TryParse(strtmp, out enableswagger);

            if (enableswagger)
                strtmp = Globals.Configuration["swaggerDoc:url"];
            var str = Globals.Configuration["Task:Enable"];
            bool enabletask = true;
            bool.TryParse(str, out enabletask);
            if (enabletask)
            {

                alltasks = initTasklist(Globals.Configuration["Task:TaskConfigFile"]);
                if (alltasks != null)
                    foreach (var otask in alltasks)
                        otask.start();

            }
            app.UseSwagger();

            // Enable middleware to serve swagger-ui (HTML, JS, CSS etc.), specifying the Swagger JSON endpoint.
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "");
            });

            app.UseMvc(GetRoutes);
            initOtherConfig(app, env);
        }
        #region


        public static ConnectionMultiplexer Manager
        {
            get
            {
                if (_redis == null)
                {
                    lock (_locker)
                    {
                        if (_redis != null) return _redis;

                        _redis = GetManager();
                        return _redis;
                    }
                }

                return _redis;
            }
        }

        private static ConnectionMultiplexer GetManager(string connectionString = null)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                connectionString = Globals.Configuration["redis:connections"];
            }

            return ConnectionMultiplexer.Connect(connectionString);
        }

        public static string cfsGetFileBaseUrl { get; set; }
        public static Dictionary<string, classViewData> classView_dic = new Dictionary<string, classViewData>();
        public static void getViewData(string classname, ref JObject jobjDisp,
            ref JObject jobjRules, ref JObject jobjValue, ref JArray disableF)
        {
            var tmpjobj = baseStartup.classView_dic[classname];
            foreach (var obj in tmpjobj.Disp.Properties())
            {
                jobjDisp.Add(obj.Name, obj.Value);
            }
            foreach (var obj in tmpjobj.Rules.Properties())
            {
                jobjRules.Add(obj.Name, obj.Value);
            }
            foreach (var obj in (tmpjobj.DisableFields as JArray))
            {
                disableF.Add(obj);
            }
            var jobj1 = new JObject();
            foreach (var obj in tmpjobj.jsonvalue.Properties())
            {

                jobj1.Add(obj.Name, obj.Value);
            }

            jobjValue.Add(classname, jobj1);
        }
        public bool checkCacheValidToken(string userid)
        {
            return true;
        }
        public virtual bool isValidTokenInChace(string userid, string token)
        {
            bool checkresult = false;
            string msg = "";
            if (_distributedCache == null)
            {
                var sprovider = _allservice.BuildServiceProvider();
                _distributedCache = sprovider.GetService<IDistributedCache>();
            }
            try
            {

                var cachekey = "loginToken_" + userid;

                var jparam = _distributedCache.GetString(cachekey);
                if (string.IsNullOrEmpty(jparam))
                {
                    checkresult = false;
                }
                JObject jobj = JObject.Parse(jparam);

                if (jobj == null)
                    checkresult = false;
                if (!jobj["valid"].ToObject<bool>())
                    checkresult = false;
                if (jobj["tokenId"].ToString() == token)
                    checkresult = true;
                if (!checkresult)
                    FrmLib.Log.commLoger.devLoger.Debug(DateTime.Now.ToString("yyyyMMdd hhmmss") + "  [userid]: " + userid + "[redis]:" + jparam + "[checkToken]:" + token);
                return checkresult;
            }
            catch (Exception e)
            {
                FrmLib.Log.commLoger.runLoger.ErrorFormat("checkCacheValidToken exception:{0}", e.Message);
                return false;

            }

        }
        #endregion
        #region init func
        public virtual void initStateMachine()
        {
            throw new NotImplementedException();
        }

        public virtual void initViewModel(ref Dictionary<string, classViewData> classView_dic)
        {
            throw new NotImplementedException();
        }
        public virtual void initJwtWithCookie(IServiceCollection services)
        { //读取配置文件
            var audienceConfig = Globals.Configuration.GetSection("Audience");
            var symmetricKeyAsBase64 = audienceConfig["Secret"];
            var keyByteArray = Encoding.ASCII.GetBytes(symmetricKeyAsBase64);
            var signingKey = new SymmetricSecurityKey(keyByteArray);
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = signingKey,
                ValidateIssuer = true,
                ValidIssuer = audienceConfig["Issuer"],//发行人
                ValidateAudience = false,
                ValidAudience = audienceConfig["Audience"],//订阅人
                                                           // 是否验证Token有效期，使用当前时间与Token的Claims中的NotBefore和Expires对比
                ValidateLifetime = true,
                // ClockSkew = TimeSpan.Zero,
                // 是否要求Token的Claims中必须包含Expires
                RequireExpirationTime = true,

            };
            var signingCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
            services.AddAuthentication(options =>
            {

                options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme; 
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
                .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
          {
              options.Cookie.Name = "MyCookie";
              options.SlidingExpiration = true;
              options.LoginPath = "/home/login";
              options.Events = new CookieAuthenticationEvents
              {
                  
                };
              
          }).AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, o =>
          {
              //不使用https
              o.RequireHttpsMetadata = false;
              o.TokenValidationParameters = tokenValidationParameters;
              o.SaveToken = true;
              
              o.Events = new JwtBearerEvents()
              {
                  OnMessageReceived = context =>
                  {
                      Console.WriteLine("test jwt OnMessageReceived");
                      //context.Token = context.Request.Headers["Authorization"];
                      // FrmLib.Log.commLoger.devLoger.DebugFormat("jwt received :{0}", context.Request.Headers["Authorization"]);
                      return Task.CompletedTask;
                  },
                  OnTokenValidated = context =>
                  {
                       var token = ((context as TokenValidatedContext).SecurityToken as JwtSecurityToken);
                      Claim clm = null;
                      string userid = null, roleid = null, rowtoken=null;
                      if (token != null)
                      {
                          clm = (from x in token.Claims where x.Type == "UserId" select x).FirstOrDefault();
                          userid = clm == null ? "" : clm.Value;
                          clm = (from x in token.Claims where x.Type == "RoleId" select x).FirstOrDefault();
                          roleid = clm == null ? "" : clm.Value;
                          rowtoken =  token.RawSignature;
                      }
                      
                       
                     
                      if (string.IsNullOrEmpty(userid) ||(roleid.Contains("custUsers") && !instance.isValidTokenInChace(userid, rowtoken)) || (!roleid.Contains("custUsers") && !instance.checkCacheValidToken(userid)))
                      {
                         
                          context.HttpContext.User = null;
                          ClaimsIdentity ci = context.Principal.Identity as ClaimsIdentity;
                          (ci.Claims as List<Claim>).Clear();

                          // context.Fail(new Exception("User is invalid"));

                      }
                      return Task.CompletedTask;
                  },
                  OnAuthenticationFailed = context =>
                  {
                      FrmLib.Log.commLoger.devLoger.Debug("jwt validFail:{0}", context.Exception);
                      context.HttpContext.User = null;
                      if (context != null && context.Principal != null)
                      {
                          ClaimsIdentity ci = context.Principal.Identity as ClaimsIdentity;
                          if(ci!=null)
                             (ci.Claims as List<Claim>).Clear();
                      }
                      return Task.CompletedTask;

                  },
                  OnChallenge = context =>
                  {
                      context.HttpContext.Response.StatusCode = 401;
                      context.HttpContext.Response.WriteAsync("invalid token");
                      return Task.CompletedTask;
                  }
              };
          })
              ;
        }

        public virtual void initJwt(IServiceCollection services)
        {
            //读取配置文件
            var audienceConfig = Globals.Configuration.GetSection("Audience");
            var symmetricKeyAsBase64 = audienceConfig["Secret"];
            var keyByteArray = Encoding.ASCII.GetBytes(symmetricKeyAsBase64);
            var signingKey = new SymmetricSecurityKey(keyByteArray);
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = signingKey,
                ValidateIssuer = true,
                ValidIssuer = audienceConfig["Issuer"],//发行人
                ValidateAudience = false,
                ValidAudience = audienceConfig["Audience"],//订阅人
                                                           // 是否验证Token有效期，使用当前时间与Token的Claims中的NotBefore和Expires对比
                ValidateLifetime = true,
                // ClockSkew = TimeSpan.Zero,
                // 是否要求Token的Claims中必须包含Expires
                RequireExpirationTime = true,

            };
            var signingCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

            services.AddAuthentication(options =>
            {

                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(o =>
            {
                //不使用https
                o.RequireHttpsMetadata = false;

                o.TokenValidationParameters = tokenValidationParameters;
                o.Events = new JwtBearerEvents()
                {
                    OnMessageReceived = context =>
                    {
                        Console.WriteLine("test jwt OnMessageReceived");
                        //context.Token = context.Request.Headers["Authorization"];
                        // FrmLib.Log.commLoger.devLoger.DebugFormat("jwt received :{0}", context.Request.Headers["Authorization"]);
                        return Task.CompletedTask;
                    },
                OnTokenValidated = context =>
                {
                        //   FrmLib.Log.commLoger.devLoger.DebugFormat("jwt sucess:{0}", context.SecurityToken);
                    var token = ((context as TokenValidatedContext).SecurityToken as JwtSecurityToken);
                    var clm = (from x in token.Claims where x.Type == "UserId" select x).FirstOrDefault();
                    var userid = clm == null ? "" : clm.Value;
                   
                    clm = (from x in token.Claims where x.Type == "RoleId" select x).FirstOrDefault();
                    var roleid = clm == null ? "" : clm.Value;
                    var rowtoken = token.RawSignature;
                    if (string.IsNullOrEmpty(userid) || (roleid.Contains("custUsers") && !instance.isValidTokenInChace(userid, rowtoken)) || (!roleid.Contains("custUsers") && !instance.checkCacheValidToken(userid))) //检查userid和token的匹配，对应用户限制一个账号只能同时一个在线，后台账号不做唯一性限制
                    {
                        context.HttpContext.User = null;
                        ClaimsIdentity ci = context.Principal.Identity as ClaimsIdentity;
                        (ci.Claims as List<Claim>).Clear();

                        //  context.Fail(new Exception("token is invalid"));

                    }
                    return Task.CompletedTask;
                },
                    OnAuthenticationFailed = context =>
                    {
                        FrmLib.Log.commLoger.devLoger.DebugFormat("jwt validFail:{0}", context.Exception);

                        context.HttpContext.User = null;
                        ClaimsIdentity ci = context.Principal.Identity as ClaimsIdentity;
                        (ci.Claims as List<Claim>).Clear();
                        return Task.CompletedTask;

                    },
                    OnChallenge = context => {
                        context.HttpContext.Response.StatusCode = 401;
                        context.HttpContext.Response.WriteAsync("invalid token");
                        return Task.CompletedTask;
                    }
                };


            });
        }
        public virtual void initCookie(IServiceCollection services)
        {
            services.AddDataProtection().PersistKeysToFileSystem(
            new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory));
            services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme; //cookieScheme的值

            }
            ).AddCookie(options =>
            {
                options.Cookie.Name = "MyCookie";
                options.Events = new CookieAuthenticationEvents
                {
                    //OnValidatePrincipal = LastChangedValidator.ValidateAsync
                };
                options.DataProtectionProvider = DataProtectionProvider.Create(new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory));
            });
        }
        public virtual void initSwagger(IServiceCollection services)
        {
           // services.AddMvcCore().AddApiExplorer();
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("[FromBody]  Content-type=applicaiton/json");
            sb.AppendLine("[FromForm]  Content-type=application/x-www-form-urlencoded");
            sb.AppendLine("[FromRoute] 参数放在请求的URL路径上");
            sb.AppendLine("[FromQuery] 参数放在url的参数位置");
            sb.AppendLine("[FromHeader] 参数在http的头上");

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Info
                {
                    Version = "v1",
                    Title = this.appName+"接口文档",
                    Description = string.Format("{0}{1}{2}",
                    "RESTful API for "+appName, Environment.NewLine,
                    sb.ToString()),
                    TermsOfService = "None",

                    Contact = new Swashbuckle.AspNetCore.Swagger.Contact
                    { Name = deverName, Email = "", Url = "" }
                }
                );


                var basePath = System.AppDomain.CurrentDomain.BaseDirectory;
                foreach (var obj in this.swaggerXmlList)
                {
                    var xmlPath = Path.Combine(basePath, obj);
                    if (File.Exists(xmlPath))
                    {
                        c.IncludeXmlComments(xmlPath);
                    }
                }
                

                c.EnableAnnotations();
                //添加自定义参数，可通过一些特性标记去判断是否添加
                c.OperationFilter<AssignOperationVendorExtensions>();
                c.OperationFilter<Consumes>();
                c.OperationFilter<complexPorertyRemoveFilter>();
                //添加对控制器的标签(描述)
                c.DocumentFilter<ApplyTagDescriptions>();
                //忽略过时的action，ObsoleteAttribute
                c.IgnoreObsoleteActions();
                c.SchemaFilter<SwaggerExcludeFilter>();
                if (enableSwaggerSiteKey)
                    c.OperationFilter<AssignSiteKeyExtensions>();
                c.DocumentFilter<EnumDocumentFilter>();
                //c.OperationFilter<HttpHeaderOperation>(); // 添加httpHeader参数
            });
        }

        public virtual IList<TimeDoTask> initTasklist(string taskConfigFile)
        {
            if (string.IsNullOrEmpty(taskConfigFile))
                return null;
           var rootdir = System.AppDomain.CurrentDomain.BaseDirectory;
            string xmlfilename = System.IO.Path.Combine(rootdir, taskConfigFile);
            if (!File.Exists(xmlfilename))
                return null;

            List<TimeDoTask> alltask = new List<TimeDoTask>();
            FrmLib.Log.commLoger.runLoger.Debug(string.Format("now createTaskList"));

            Type[] nodeTaskHandles = FrmLib.Extend.tools_static.GetTypesFromAssemblysByType(typeof(ICronTask));
           
           
            XmlDocument tmpxmldoc = Static_xmltools.GetXmlDocument(xmlfilename);
            XmlNodeList alllist = null;
            string xpathstr = "/configuration/CronTask";
            alllist = Static_xmltools.SelectXmlNodes(tmpxmldoc, xpathstr);
            FrmLib.Log.commLoger.runLoger.Debug(string.Format("createTaskList count:{0}", alllist.Count));
            TimeDoTask tdt = null;
            TimeNowDoEventHandler myfunc = null;
            foreach (XmlNode onenode in alllist)
            {
                int tasktype = int.Parse(Static_xmltools.GetXmlAttr(onenode, "taskType"));
                string paramvalue = Static_xmltools.GetXmlAttr(onenode, "paramValue");
                string procesorName = Static_xmltools.GetXmlAttr(onenode, "procesorName");
                try
                {
                    var nodeTaskHandle = FrmLib.Extend.tools_static.getTypeHaveMethodByName(nodeTaskHandles, procesorName);
                    if (nodeTaskHandle == null)
                        continue;
                    myfunc = (TimeNowDoEventHandler)Delegate.CreateDelegate(typeof(TimeNowDoEventHandler), nodeTaskHandle, procesorName);
                    if (tasktype == (int)enum_taskType.interval)
                    {
                        tdt = new TimeDoTask(int.Parse(paramvalue), myfunc);
                    }
                    else
                        tdt = new TimeDoTask(paramvalue, myfunc, (enum_taskType)tasktype);
                    alltask.Add(tdt);
                }
                catch (Exception exp)
                {
                    FrmLib.Log.commLoger.runLoger.Error("create task error, info:" + onenode.InnerText);
                    return null;
                }
            }
            return alltask;
        }
        public virtual void UseWebSocket(IApplicationBuilder app)
        {
#if NoOptions
            #region UseWebSockets
                        app.UseWebSockets();
            #endregion
#endif
#if UseOptions
            #region UseWebSocketsOptions
                        var webSocketOptions = new WebSocketOptions()
                        {
                            KeepAliveInterval = TimeSpan.FromSeconds(120),
                            ReceiveBufferSize = 4 * 1024
                        };
                        app.UseWebSockets(webSocketOptions);
            #endregion
#endif
            #region AcceptWebSocket
            app.Use(async (context, next) =>
            {
                if (context.Request.Path == "/ws")
                {
                    if (context.WebSockets.IsWebSocketRequest)
                    {
                        WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
                        await Echo(context, webSocket);
                    }
                    else
                    {
                        context.Response.StatusCode = 400;
                    }
                }
                else
                {
                    await next();
                }

            });
            #endregion
        }
        #region Echo
        private static async Task Echo(HttpContext context, WebSocket webSocket)
        {
            var buffer = new byte[1024 * 4];
            WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            while (!result.CloseStatus.HasValue)
            {
                await webSocket.SendAsync(new ArraySegment<byte>(buffer, 0, result.Count), result.MessageType, result.EndOfMessage, CancellationToken.None);

                result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            }
            await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
        }
        #endregion
        #endregion
    }
}
