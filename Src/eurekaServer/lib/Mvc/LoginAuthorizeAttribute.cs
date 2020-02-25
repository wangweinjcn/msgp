   #if (NETCORE || NETSTANDARD2_0 )
using Ace;
using Ace.Application;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Routing;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ace.Web.Mvc.Common
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class SkipLoginAuthorizeAttribute : Attribute, IFilterMetadata
    {
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
    public class LoginAuthorizeAttribute : Attribute, IAuthorizationFilter
    {
        public LoginAuthorizeAttribute()
        {
        }
        public virtual void OnAuthorization(AuthorizationFilterContext filterContext)
        {
            if (filterContext.HttpContext.User.Identity.IsAuthenticated || this.SkipAuthorize(filterContext.ActionDescriptor))
            {
                return;
            }

            HttpRequest httpRequest = filterContext.HttpContext.Request;
            if (httpRequest.IsAjaxRequest())
            {
                Result result = Result.CreateResult(ResultStatus.NotLogin, "未登录或登录超时，请重新登录");
                string json = JsonHelper.Serialize(result);
                ContentResult contentResult = new ContentResult() { Content = json };
                filterContext.Result = contentResult;
                return;
            }
            else
            {
                string url = filterContext.HttpContext.Content("~/Account/Login");
                url = string.Concat(url, "?returnUrl=", httpRequest.Path);
                
                RedirectResult redirectResult = new RedirectResult(url);
                filterContext.Result = redirectResult;
                return;
            }
        }

        protected virtual bool SkipAuthorize(ActionDescriptor actionDescriptor)
        {
            bool skipAuthorize = actionDescriptor.FilterDescriptors.Where(a => a.Filter is SkipLoginAuthorizeAttribute).Any();
            if (skipAuthorize)
            {
                return true;
            }

            return false;
        }
    }

}
#endif