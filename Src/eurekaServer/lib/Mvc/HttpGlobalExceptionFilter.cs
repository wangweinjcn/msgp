   #if (NETCORE || NETSTANDARD2_0 )
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;


namespace Ace.Web.Mvc
{
    public class HttpGlobalExceptionFilter : IExceptionFilter
    {
        private readonly IHostingEnvironment _env;

        public HttpGlobalExceptionFilter(IHostingEnvironment env)
        {
            this._env = env;
        }

        public ContentResult FailedMsg(string msg = null)
        {
            Result retResult = new Result(ResultStatus.Failed, msg);
            string json = JsonHelper.Serialize(retResult);
            return new ContentResult() { Content = json };
        }
        public void OnException(ExceptionContext filterContext)
        {
            if (filterContext.ExceptionHandled)
                return;

            //执行过程出现未处理异常
            Exception ex = filterContext.Exception;
            var errorinfo1 =  FrmLib.Extend.tools_static.getExceptionMessage(ex);
            var errorinfo = string.Format("{0},{1}", errorinfo1, ex.StackTrace);
#if DEBUG
            if (filterContext.HttpContext.Request.IsAjaxRequest())
            {
                string msg = null;
                this.LogException(filterContext);
                msg = ex.Message;

                filterContext.Result = this.FailedMsg(errorinfo);
                filterContext.ExceptionHandled = true;
                return;
                
            }


#endif

            if (filterContext.HttpContext.Request.IsAjaxRequest())
            {
                string msg = null;

                {
                    this.LogException(filterContext);
                    msg = "服务器异常(" + errorinfo1 + ")";
                }

                filterContext.Result = this.FailedMsg(msg);
                filterContext.ExceptionHandled = true;
                return;
            }
            else
            {
                //对于非 ajax 请求


                string msg = null;

                {
                    this.LogException(filterContext);
                    msg = "服务器异常(" + errorinfo1 + ")";
                }

                filterContext.Result = this.FailedMsg(msg);
                filterContext.ExceptionHandled = true;
                return;
            }
        }

        /// <summary>
        ///  将错误记录进日志
        /// </summary>
        /// <param name="filterContext"></param>
        void LogException(ExceptionContext filterContext)
        {
            /*
            ILoggerFactory loggerFactory = filterContext.HttpContext.RequestServices.GetService(typeof(ILoggerFactory)) as ILoggerFactory;
            ILogger logger = loggerFactory.CreateLogger(filterContext.ActionDescriptor.DisplayName);
            */
            var  mess= FrmLib.Extend.tools_static.getExceptionMessage(filterContext.Exception);
            FrmLib.Log.commLoger.runLoger.ErrorFormat("Error: "
                + mess+System.Environment.NewLine+"  "+ReplaceParticular(filterContext.Exception.StackTrace));
        }

        static string ReplaceParticular(string s)
        {
            if (string.IsNullOrEmpty(s))
                return s;
            return s;
            //return s.Replace("\r", "#R#").Replace("\n", "#N#").Replace("|", "#VERTICAL#");
        }
    }
}
#endif