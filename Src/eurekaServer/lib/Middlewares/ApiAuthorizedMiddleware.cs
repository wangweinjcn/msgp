#if NETCORE
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Ace.Web.Mvc.Middlewares
{
    public class ApiAuthorizedMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ApiAuthorizedOptions _options;
        private readonly IRouter _router;
        private RouteMatcher _routeMatcher;
       
        public ApiAuthorizedMiddleware(RequestDelegate next, IRouter router)
            :this(next,null,router)
        {
            ApiAuthorizedOptions obj = new ApiAuthorizedOptions();
            obj.isWeb = false;
            obj.loginUrl = "";
            this._options = obj;
        }
        public ApiAuthorizedMiddleware(RequestDelegate next, IOptions<ApiAuthorizedOptions> options, IRouter router)
        {
            this._next = next;
            this._options = options.Value;

            _routeMatcher = new RouteMatcher();

            
           
            this._router = router;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            
            httpContext.Request.EnableRewind();
            var strtmp = httpContext.Request.Path.ToString().ToUpper();
            
            if (_options.enableSwagger && strtmp.Contains(_options.swaggerUrl.ToUpper()))
            {                
                    await _next.Invoke(httpContext);
                    return;
               
               
                
            }


            var context = new RouteContext(httpContext);


            context.RouteData.Routers.Clear();
            context.RouteData.Routers.Add(_router);
            RouteCollection routec = _router as RouteCollection;
         
            await _router.RouteAsync(context);
            RouteData routeData=context.RouteData;
            if (context.Handler != null)
            {
                httpContext.Features[typeof(IRoutingFeature)] = new RoutingFeature()
                {
                    RouteData = context.RouteData,
                };
            }
            Console.WriteLine(httpContext.Request.Method.ToUpper());
            Newtonsoft.Json.Linq.JObject paramjson = new Newtonsoft.Json.Linq.JObject();
            foreach (var onekey in routeData.Values.Keys)
            {
                object tmp;
                routeData.Values.TryGetValue(onekey, out tmp);
                if (tmp != null)
                    paramjson.Add(onekey, tmp.ToString());
                
            }
            string area, controler, action;
            if (routeData.Values.Keys.Contains("area"))
            {
                area = routeData.Values["area"].ToString().ToLower();
                paramjson.Remove("area");
            }
            else
                area = "-1";
            if (routeData.Values.Keys.Contains("controller"))
            {
                controler = routeData.Values["controller"].ToString().ToLower();
                paramjson.Remove("controller");
            }
            else
                controler = "";
            if (routeData.Values.Keys.Contains("action"))
            {
                action = routeData.Values["action"].ToString().ToLower();
                paramjson.Remove("action");
            }
            else
                action = "";
            if (area == "-1" && string.IsNullOrEmpty(controler))
            {
                await ReturnSystemError(httpContext);
                return;
            }
            string rolestr="";
            string userid = "";
            bool isdevice = false;
            Microsoft.Extensions.Primitives.StringValues dtoken = "";
            if (context.HttpContext.Request.Headers.TryGetValue("DeviceToken", out dtoken))
            {
                if (!string.IsNullOrEmpty(dtoken))
                    isdevice = true;
            }
            if (httpContext.User != null && httpContext.User.Identity.IsAuthenticated)
            {
                ClaimsIdentity ci = httpContext.User.Identity as ClaimsIdentity;
                var userPrincipal = new ClaimsPrincipal(ci);
                System.Threading.Thread.CurrentPrincipal = userPrincipal;
                var clm = (from x in ci.Claims where x.Type == "RoleId" select x).FirstOrDefault();
                rolestr = clm == null ? "" : clm.Value;
                clm = (from x in ci.Claims where x.Type == "UserId" select x).FirstOrDefault();
                userid = clm == null ? "" : clm.Value;
                if (!context.HttpContext.Request.Path.ToString().Contains("home/getUnReadMessage"))
                {

                }

            }
            else
            {
                if (httpContext.Request.Headers.ContainsKey("token"))
                {

                }
            }
            string paramstr = paramjson.ToString();
            if (!this._options.authorizefilter.AnonymousAllowed(controler, action,
                httpContext.Request.Method, paramstr, area, isdevice))
            {
                if (string.IsNullOrEmpty(rolestr))
                {
                    if(_options.isWeb)
                        await ReturnRedirect(httpContext, _options.loginUrl);
                    else
                        await ReturnNeedLogin(httpContext);
                    return;
                }
                if (!this._options.authorizefilter.Isvalidtoken(userid))
                {
                    await ReturnNeedLogin(httpContext);
                    return;
                }
                if (!this._options.authorizefilter.IsAllowed(rolestr, controler, action,
                  httpContext.Request.Method, paramstr, area, isdevice))
                {
                    await ReturnNoAuthorized(httpContext);
                    return;
                }
            }
            
            await _next.Invoke(httpContext);
        }

        #region private method
        /// <summary>
        /// not authorized request
        /// 401返回码，表示需要登录
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private async Task ReturnNeedLogin(HttpContext context)
        {
            BaseResponseResult response = new BaseResponseResult
            {
                Code = "401",
                Message = "You are not authorized!"
            };

            if (_options.noAuthoReturnok)
            {
                context.Response.StatusCode = 200;
            }
            else
            {

                context.Response.StatusCode = 401;
                
            }
            await context.Response.WriteAsync(JsonConvert.SerializeObject(response));
        }
        /// <summary>
        /// not authorized request
        /// 403返回码，表示无权限
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private async Task ReturnNoAuthorized(HttpContext context)
        {
            BaseResponseResult response = new BaseResponseResult
            {
                Code = "401",
                Message = "You are not authorized!"
            };

            if (_options.noAuthoReturnok)
            {
                context.Response.StatusCode = 200;
            }
            else
            {
             
                context.Response.StatusCode = 403;
            }
            await context.Response.WriteAsync(JsonConvert.SerializeObject(response));
        }
        /// <summary>
        /// system error request 
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private async Task ReturnSystemError(HttpContext context)
        {

            BaseResponseResult response = new BaseResponseResult
            {
                Code = "500",
                Message = "Some exception!"
            };
            context.Response.StatusCode = 500;
            await context.Response.WriteAsync(JsonConvert.SerializeObject(response));
        }
        /// <summary>
        /// timeout request 
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private async Task ReturnTimeOut(HttpContext context)
        {
            
            BaseResponseResult response = new BaseResponseResult
            {
                Code = "408",
                Message = "Time Out!"
            };
            context.Response.StatusCode = 408;
            await context.Response.WriteAsync(JsonConvert.SerializeObject(response));
        }
        private async Task ReturnRedirect(HttpContext context, string redictUrl)
        {
            BaseResponseResult response = new BaseResponseResult
            {
                Code = "302",
                Message = "Need login!"
            };
            context.Response.StatusCode = 302;
            context.Response.Headers.Add("Location", redictUrl);
            await context.Response.WriteAsync(JsonConvert.SerializeObject(response));
        }
        /// <summary>
        /// check the application
        /// </summary>
        /// <param name="context"></param>
        /// <param name="applicationId"></param>
        /// <param name="applicationPassword"></param>
        /// <returns></returns>
        private async Task CheckApplication(HttpContext context, string applicationId, string applicationPassword)
        {
            var application = GetAllApplications().Where(x => x.ApplicationId == applicationId).FirstOrDefault();
            if (application != null)
            {
                if (application.ApplicationPassword != applicationPassword)
                {
                    await ReturnNoAuthorized(context);
                }
            }
            else
            {
                await ReturnNoAuthorized(context);
            }
        }

        /// <summary>
        /// check the expired time
        /// </summary>
        /// <param name="timestamp"></param>
        /// <param name="expiredSecond"></param>
        /// <returns></returns>
        private bool CheckExpiredTime(double timestamp, double expiredSecond)
        {
            double now_timestamp = (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
            return (now_timestamp - timestamp) > expiredSecond;
        }

        /// <summary>
        /// http get invoke
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private async Task GetInvoke(HttpContext context)
        {
            var queryStrings = context.Request.Query;
            RequestInfo requestInfo = new RequestInfo
            {
                ApplicationId = queryStrings["applicationId"].ToString(),
                ApplicationPassword = queryStrings["applicationPassword"].ToString(),
                Timestamp = queryStrings["timestamp"].ToString(),
                Nonce = queryStrings["nonce"].ToString(),
                Sinature = queryStrings["signature"].ToString()
            };
            await Check(context, requestInfo);
        }

        /// <summary>
        /// http post invoke
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private async Task PostInvoke(HttpContext context)
        {
            var formCollection = context.Request.Form;
             
            RequestInfo requestInfo = new RequestInfo
            {
                ApplicationId = formCollection["applicationId"].ToString(),
                ApplicationPassword = formCollection["applicationPassword"].ToString(),
                Timestamp = formCollection["timestamp"].ToString(),
                Nonce = formCollection["nonce"].ToString(),
                Sinature = formCollection["signature"].ToString()
            };
            await Check(context, requestInfo);
        }

        /// <summary>
        /// the main check method
        /// </summary>
        /// <param name="context"></param>
        /// <param name="requestInfo"></param>
        /// <returns></returns>
        private async Task Check(HttpContext context, RequestInfo requestInfo)
        {
             string computeSinature = "";
            double tmpTimestamp;
            if (computeSinature.Equals(requestInfo.Sinature) &&
                double.TryParse(requestInfo.Timestamp, out tmpTimestamp))
            {
                if (CheckExpiredTime(tmpTimestamp, _options.ExpiredSecond))
                {
                    await ReturnTimeOut(context);
                }
                else
                {
                    await CheckApplication(context, requestInfo.ApplicationId, requestInfo.ApplicationPassword);
                }
            }
            else
            {
                await ReturnNoAuthorized(context);
            }
        }

        /// <summary>
        /// return the application infomations
        /// </summary>
        /// <returns></returns>
        private IList<ApplicationInfo> GetAllApplications()
        {
            return new List<ApplicationInfo>()
            {
                new ApplicationInfo { ApplicationId="1", ApplicationName="Member", ApplicationPassword ="123"},
                new ApplicationInfo { ApplicationId="2", ApplicationName="Order", ApplicationPassword ="123"}
            };
        }
        #endregion
    }
}
#endif