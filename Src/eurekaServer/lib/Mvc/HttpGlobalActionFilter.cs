
   #if (NETCORE || NETSTANDARD2_0 )
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Ace.Web.Mvc
{
    public class HttpOssNullActionFilter : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            var parameters = context.ActionDescriptor.Parameters;

            foreach (ParameterDescriptor parameter in parameters)
            {
                if (parameter.ParameterType == typeof(string))
                {
                    
                    if (context.ActionArguments.ContainsKey(parameter.Name))
                    {
                        var x = context.ActionArguments[parameter.Name];
                       if(x!=null && (x.Equals("null") || x.Equals("undefined")))
                         context.ActionArguments[parameter.Name] = null;
                        context.ActionArguments[parameter.Name] = (context.ActionArguments[parameter.Name] as string)?.Trim();
                    }
                }
            }
        }
            public void OnActionExecuted(ActionExecutedContext context)
        {
           
          
        }
    }
    public class HttpGlobalActionFilter : IActionFilter
    {
        public void OnActionExecuted(ActionExecutedContext context)
        {
            if (context.HttpContext.GetRequestUrl().Contains("home/getUnReadMessage"))
                return;
            var httpContext = context.HttpContext;
            var stopwach = httpContext.Items["stopwatchkey"] as Stopwatch;
            stopwach.Stop();
            var time = stopwach.Elapsed;
            string stragent = "";
            var strcsid = "";
            var token = "";
            if (context.HttpContext.Request.Headers.ContainsKey("CSID"))
            {
                strcsid = context.HttpContext.Request.Headers["CSID"];
            }
            if (context.HttpContext.Request.Headers.ContainsKey("User-Agent"))
            {
                stragent = context.HttpContext.Request.Headers["User-Agent"];
            }
            if (context.HttpContext.Request.Headers.ContainsKey("Authorization"))
            {
                token = context.HttpContext.Request.Headers["Authorization"];
            }
            var pmyvalue = "";
            if (!context.HttpContext.Request.Cookies.TryGetValue("pmyCookie", out pmyvalue))
            { }
            var responseStatus = context.HttpContext.Response.StatusCode;
            //foreach (var param in context.HttpContext.Request.Query)
            //{
            //    //if (!string.IsNullOrEmpty(param.Value) && param.Value.Equals("null"))

            //    strtmp = strtmp + "[" + param.Key + ":"+param.Value + "]";


            //}
            //FrmLib.Log.commLoger.devLoger.DebugFormat("params:"+strtmp);
            //strtmp = "";
            //foreach (var param in context.HttpContext.Request.Headers)
            //{
            //    strtmp = strtmp + "[" + param.Key + ":" + param.Value + "]";
            //}
            // FrmLib.Log.commLoger.biLoger.InfoFormat($"{context.ActionDescriptor.DisplayName},from IP:{GetClientIP(httpContext)}");
            FrmLib.Log.commLoger.biLoger.InfoFormat("{0}||{1}||{2}||{3}||{4}||{5}", GetClientIP(httpContext)
                , strcsid, context.HttpContext.GetRequestUrl(),stragent,responseStatus,pmyvalue );

           

            if (time.TotalSeconds > 1)
            {
               
                FrmLib.Log.commLoger.perfLoger.InfoFormat("{0}||{1}||{2}||{3}||{4}||{5}", GetClientIP(httpContext)
                , strcsid, context.HttpContext.GetRequestUrl(), stragent, responseStatus, "执行耗时:"+time.ToString());
            }
        }
        public  string GetClientIP( HttpContext httpContext)
        {
            var ip = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (string.IsNullOrEmpty(ip))
            {
                ip = httpContext.Connection.RemoteIpAddress.ToString();
            }

            if (ip == "::1")
                ip = "127.0.0.1";

            return ip;
        }
        public void OnActionExecuting(ActionExecutingContext context)
        {

            Stopwatch stopwach = null;
           
          if (context.HttpContext.Items.ContainsKey("stopwatchkey"))
                stopwach = context.HttpContext.Items["stopwatchkey"] as Stopwatch;
            else
            {
                stopwach = new Stopwatch();
                context.HttpContext.Items.Add("stopwatchkey", stopwach);
            }
            stopwach.Start();
           
        }
    }

}
#endif
