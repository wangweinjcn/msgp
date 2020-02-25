
   #if (NETCORE || NETSTANDARD2_0 )
using Ace.Web.Mvc.Common;
using FrmLib.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Ace.Web.Mvc
{
    public class BaseResponseResult
    {
        public string Code { get; set; }

        public string Message { get; set; }
    }
    public class AuthorizedFilterOptions
    {
        public string Name { get; set; }

        public string EncryptKey { get; set; }

        public int ExpiredSecond { get; set; }
        public bool isWeb { get; set; }
        public bool noAuthoReturnok { get; set; }
        public string loginUrl { get; set; }
       
        public string errorUrl { get; set; }

        public bool isUseCache { get; set; }
        public bool enableSwagger { get; set; }
        public string swaggerUrl { get; set; }
        public IAuthorizefilter authorizefilter { get; set; }

        public AuthorizedFilterOptions()
        {
            noAuthoReturnok = false;
            enableSwagger = false;
            swaggerUrl = "/Swagger";
        }
    }
    
    public class HttpGlobalAuthFilter : Attribute, IAuthorizationFilter
    {
        private readonly AuthorizedFilterOptions _options;

        public HttpGlobalAuthFilter(AuthorizedFilterOptions options)
        {
            _options = options;
        }
        public void OnAuthorization(AuthorizationFilterContext context)
        {
           
           
          var list=  context.HttpContext.Authentication.GetAuthenticationSchemes();
            foreach (var one in list)
            {
              Console.WriteLine(  one.AuthenticationScheme);
            }
            Newtonsoft.Json.Linq.JObject paramjson = new Newtonsoft.Json.Linq.JObject();
            foreach (var onekey in context.RouteData.Values.Keys)
            {
                object tmp;
                context.RouteData.Values.TryGetValue(onekey, out tmp);
                if (tmp != null)
                    paramjson.Add(onekey, tmp.ToString());

            }
            string area, controler, action;
            if (context.RouteData.Values.Keys.Contains("area"))
            {
                area = context.RouteData.Values["area"].ToString().ToLower();
                paramjson.Remove("area");
            }
            else
                area = "-1";
            if (context.RouteData.Values.Keys.Contains("controller"))
            {
                controler = context.RouteData.Values["controller"].ToString().ToLower();
                paramjson.Remove("controller");
            }
            else
                controler = "";
            if (context.RouteData.Values.Keys.Contains("action"))
            {
                action = context.RouteData.Values["action"].ToString().ToLower();
                paramjson.Remove("action");
            }
            else
                action = "";
            if (area == "-1" && string.IsNullOrEmpty(controler))
            {
                context.Result = ReturnSystemError(context);
                return;
            }
          
            string rolestr = "";
            string userid = "";
            bool isdevice = false;
            Microsoft.Extensions.Primitives.StringValues dtoken = "";
            if (context.HttpContext.Request.Headers.TryGetValue("DeviceToken", out dtoken))
            {
                if (!string.IsNullOrEmpty(dtoken))
                    isdevice = true;
            }
          Console.WriteLine(   System.Threading.Thread.CurrentThread.ManagedThreadId);
            if (context.HttpContext.User != null && context.HttpContext.User.Identity.IsAuthenticated)
            {
                ClaimsIdentity ci = context.HttpContext.User.Identity as ClaimsIdentity;
                var userPrincipal = new ClaimsPrincipal(ci);
                System.Threading.Thread.CurrentPrincipal = userPrincipal;
                var clm = (from x in ci.Claims where x.Type == "RoleId" select x).FirstOrDefault();
                rolestr = clm == null ? "" : clm.Value;
                clm = (from x in ci.Claims where x.Type == "UserId" select x).FirstOrDefault();
                userid = clm == null ? "" : clm.Value;
                

            }
            else
            {
                if (context.HttpContext.Request.Headers.ContainsKey("token"))
                {

                }
            }
            string paramstr = paramjson.ToString();
            if (!this._options.authorizefilter.AnonymousAllowed(controler, action,
                context.HttpContext.Request.Method, paramstr, area, isdevice))
            {
                var Caction = context.ActionDescriptor as ControllerActionDescriptor;
                bool isapi = false;
                //控制器是继承CsApiHttpController
                if (typeof(apiController).IsAssignableFrom(Caction.ControllerTypeInfo))
                    isapi = true;

                bool shouldRedirct = false;
                if (string.IsNullOrEmpty(rolestr)
                    || !this._options.authorizefilter.Isvalidtoken(userid)
                    || !this._options.authorizefilter.IsAllowed(rolestr, controler, action,
                  context.HttpContext.Request.Method, paramstr, area, isdevice))
                {

                    shouldRedirct = true;


                }
                if (shouldRedirct)
                {
                    if (!isapi && _options.isWeb)
                        context.Result = ReturnRedirect(context, _options);
                    else
                        context.Result = ReturnNeedLogin(context);
                    return;
                }
            }
            //if (context.HttpContext.User.Identity.Name != "1") //只是个示范作用
            //{
            //    //未通过验证则跳转到无权限提示页
            //    RedirectToActionResult content = new RedirectToActionResult("NoAuth", "Exception", null);
            //    StatusCodeResult scr = new StatusCodeResult(400);
            //    JObject jobj = new JObject();
            //    jobj.Add("code", 100);
            //    jobj.Add("memo", "test");
            //    BadRequestObjectResult bror = new BadRequestObjectResult(jobj);
            //     context.Result = bror;
            //}
        }
#region private method
        /// <summary>
        /// not authorized request
        /// 401返回码，表示需要登录
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private  IActionResult ReturnNeedLogin(AuthorizationFilterContext context)
        {
            IActionResult resutl;
            BaseResponseResult response = new BaseResponseResult
            {
                Code = "401",
                Message = "You are not authorized!"
            };
            var str = JsonConvert.SerializeObject(response);
            if (_options.noAuthoReturnok)
            {
                resutl = new ObjectResult(response) { StatusCode = 200 };
              
                
            }
            else
            {
                resutl = new ObjectResult(response) { StatusCode = 401 };
               
            }
            return resutl;
        }
        /// <summary>
        /// not authorized request
        /// 403返回码，表示无权限
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private IActionResult ReturnNoAuthorized(AuthorizationFilterContext context)
        {
            IActionResult resutl;
            BaseResponseResult response = new BaseResponseResult
            {
                Code = "401",
                Message = "You are not authorized!"
            };
            var str = JsonConvert.SerializeObject(response);
            if (_options.noAuthoReturnok)
            {
                resutl = new ObjectResult(response) { StatusCode = 200 };


            }
            else
            {
                resutl = new ObjectResult(response) { StatusCode = 403 };

            }
            return resutl;
            
        }
        /// <summary>
        /// system error request 
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private IActionResult ReturnSystemError(AuthorizationFilterContext context)
        {
            IActionResult resutl;
            BaseResponseResult response = new BaseResponseResult
            {
                Code = "500",
                Message = "权限解析错误!"
            };
           
            if (_options.noAuthoReturnok)
            {
                resutl = new ObjectResult(response) { StatusCode = 500 };


            }
            else
            {
                resutl = new ObjectResult(response) { StatusCode = 500 };

            }

            return resutl;
           
           
        }

        private IActionResult ReturnRedirect(AuthorizationFilterContext context, AuthorizedFilterOptions options)
        {
            IActionResult xx = new RedirectResult(options.loginUrl);
           
            return xx;
        }



#endregion
        }

}
#endif