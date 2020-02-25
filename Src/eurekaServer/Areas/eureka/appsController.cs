using Ace.Web.Mvc;
using eurekaServer.Models;
using FrmLib.Extend;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Formatters.Xml.Extensions;
using Proxy.Comm;
using Proxy.Comm.model;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.IO;
using System.Net;
using System.Xml;

namespace eurekaServer.Areas.Controllers
{
    /// <summary>
    /// euraka RestApi服务控制器
    /// </summary>
    [Area(AreaNames.eureka)]
    [Route("[Area]/v2/[controller]")]
    [AllowAnonymous]
    [ApiController]
    public class appsController :apiController
    {
        string newskey;
        string headToken;
        private static object lockobj = new object();

        private bool haveCompKey(string key)
        {
            if (nStartup.sdkReoteComs == null)
            {
               
            }
            return nStartup.sdkReoteComs.ContainsKey(key);
        }
        private string getCompToken(string key)
        {
            return nStartup.sdkReoteComs[key];
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="filterContext"></param>
        [NonAction]
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
             Request.EnableRewind();
            //if (!filterContext.HttpContext.Request.Headers.ContainsKey("sdkToken"))
            //    filterContext.Result = this.FailedMsg("授权有异常,没有有效token");
            //headToken = filterContext.HttpContext.Request.Headers["sdkToken"];
            base.OnActionExecuting(filterContext);
        }
        private bool checkToken(string comKey, string taskNO, string timstamp)
        {
            if (!haveCompKey(comKey))
                return false;
            var signstr = string.Format("{0}{1}{2}{3}", comKey, taskNO, timstamp, getCompToken(comKey));
            var md5str = signstr.ToMD5();
            if (md5str.ToUpper() == headToken.ToUpper())
                return true;
            else
                return false;

        }
        private InstanceInfo prepareInstance()
        {
            InstanceInfo ins = null;
            string contenttype = Request.ContentType.ToUpper();
            if (contenttype.Contains("XML"))
            {
                ins=InstanceInfo.fromXml(Request.Body);
            }
            if (contenttype.Contains("JSON"))
            {
                InstanceInfo.fromJson(Request.Body);
            }
            return ins;
        }

        private IActionResult contentInstance(InstanceInfo ins,HttpStatusCode hscode)
        {
            string contenttype = Request.ContentType.ToUpper();
            if (contenttype.Contains("XML"))
            {
                return ContentWithStatus(ins.toxml(), hscode);
            }
            if (contenttype.Contains("JSON"))
            {
                return ContentWithStatus(ins.toJson(), hscode);
            }
            return ContentWithStatus("", HttpStatusCode.BadRequest);
        }
        
        #region 第三方调用
        /// <summary>
        /// Register new application instance
        /// </summary>
        /// <param name="appid"></param>
        /// <returns></returns>
        [HttpPost]
         [Route("{appid}")]
       // [SwaggerOperation(Tags = new[] { "eureka" })]
        public IActionResult addInstance([FromRoute]string appid)
        {

            var ins = prepareInstance();
            ins.appName = appid;
            mapPortGroup mpg = null;
            if (RunConfig.Instance.ownPNServer.maphttpGroup_dic.ContainsKey(ins.appName))
            {
                mpg = RunConfig.Instance.ownPNServer.maphttpGroup_dic[ins.appName];
            }
            else
            {
                mpg = new mapPortGroup("127.0.0.1", "0", ins.appName, -1, outPortSelectPolicy.fastResponse, RunConfig.Instance.clusterId, true);
                mpg.addOutPort(ins.ipAddr, ins.port.ToString(), null, -1, -1, true);
                RunConfig.Instance.ownPNServer.maphttpGroup_dic.Add(ins.appName, mpg);
            }
            return noContentStatus(HttpStatusCode.NoContent);
        }
       
        #endregion

    }
}
