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
    [Area(AreaNames.gserver)]
    [Route("[Area]/[controller]/[action]")]
    [AllowAnonymous]
    [ApiController]
    public class gServerController :apiController
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
            
                return false;

        }
        

       

        #region  集群服务器调用
        /// <summary>
        /// 向主域服务器报告心跳
        /// </summary>
        /// <param name="sid">报告心跳的zoneMaster</param>
        /// <param name="regionName">报告的主域名称</param>
        /// <returns></returns>
        [HttpPost]
         [Route("{regionName}")]
        [SwaggerOperation(Tags = new[] { "gServer" })]
        public IActionResult sayaliveToRegionMaster([FromRoute]string regionName,[FromQuery]string sid)
        {
            regionZoneServer regionZone;
            if (RunConfig.Instance.region_dic.ContainsKey(regionName))
            {
                regionZone = RunConfig.Instance.region_dic[regionName];

            } else
                regionZone = RunConfig.Instance.region_dic["*"];

            var server = regionZone.getServerById(sid);
            if (server != null)
                server.lastLive = DateTime.Now;     
            return noContentStatus(HttpStatusCode.NoContent);
        }

        [HttpPost]
        [Route("{id}")]
         [SwaggerOperation(Tags = new[] { "gServer" })]
        public IActionResult sayaliveToZoneMaster([FromRoute]string id)
        {
            regionZoneServer regionZone;
            if (RunConfig.Instance.region_dic.ContainsKey(RunConfig.Instance.region))
            {
                regionZone = RunConfig.Instance.region_dic[RunConfig.Instance.region];

            }
            else
                regionZone = RunConfig.Instance.region_dic["*"];
            var server = regionZone.getServerById(id);
            if (server != null)
                server.lastLive = DateTime.Now;
            return noContentStatus(HttpStatusCode.NoContent);
        }

        [HttpPost]
        [Route("{id}")]
        [SwaggerOperation(Tags = new[] { "gServer" })]
        public IActionResult registerToZoneMaster([FromRoute]string id)
        {

            return noContentStatus(HttpStatusCode.NoContent);
        }
        [HttpPost]
        [Route("{id}")]
        [SwaggerOperation(Tags = new[] { "gServer" })]
        public IActionResult registerToRegionMaster([FromRoute]string id)
        {

            return noContentStatus(HttpStatusCode.NoContent);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>

        [HttpPost]
        [Route("{id}")]
        [SwaggerOperation(Tags = new[] { "gServer" })]
        public IActionResult zoneNotices([FromRoute]string id)
        {

            return noContentStatus(HttpStatusCode.NoContent);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("{id}")]
        [SwaggerOperation(Tags = new[] { "gServer" })]
        public IActionResult regionNotices([FromRoute]string id)
        {
            if (RunConfig.Instance.ownPNServer.id != id)
                return Content("");
            else
            {

            }
        }
        /// <summary>
        /// 获取当前zone的集群信息，请求的服务器应该和当前访问器是同一个zone
        /// </summary>
        /// <returns></returns>
        [HttpGet]
         [Route("{id}")]
        [SwaggerOperation(Tags = new[] { "gServer" })]
        public IActionResult zoneClusterServer()
        {
            regionZoneServer regionZone;
            if (RunConfig.Instance.region_dic.ContainsKey(RunConfig.Instance.region))
            {
                regionZone = RunConfig.Instance.region_dic[RunConfig.Instance.region];

            }
            else
                regionZone = RunConfig.Instance.region_dic["*"];
            if (regionZone.zoneServer_dic.ContainsKey(RunConfig.Instance.zone))
            {
                var str = regionZone.zoneServer_dic[RunConfig.Instance.zone].toJson().ToString();
                return Content(str);
            }
            else
                return noContentStatus(HttpStatusCode.NoContent);
        }
        /// <summary>
        /// 获取整个区域的集群信息
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [SwaggerOperation(Tags = new[] { "gServer" })]
        public IActionResult regionClusterServer()
        {
            regionZoneServer regionZone;
            if (RunConfig.Instance.region_dic.ContainsKey(RunConfig.Instance.region))
            {
                regionZone = RunConfig.Instance.region_dic[RunConfig.Instance.region];

            }
            else
                regionZone = RunConfig.Instance.region_dic["*"];

            return Content(regionZone.toJson().ToString());

        }
        #endregion

    }
}
