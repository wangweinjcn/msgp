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
    [Area(AreaNames.sysconf)]
    [Route("[Area]/[controller]/[action]")]
    [AllowAnonymous]
    [ApiController]
    public class gServerController : apiController
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

        private regionZoneServer getRegion(string regionName)
        {
            regionZoneServer regionZone=null;
            if (localRunServer.Instance.region_dic.ContainsKey(regionName))
            {
                regionZone = localRunServer.Instance.region_dic[regionName];

            }


            return regionZone;
        }
        private zoneServerCluster getZone(string zoneName,string regionName)
        {
            regionZoneServer regionZone=getRegion(regionName);

            if (regionZone.zoneServer_dic.ContainsKey(zoneName))
                return regionZone.zoneServer_dic[zoneName];

            return null;
        }


        #region  集群服务器调用
        /// <summary>
        /// 向主域服务器报告心跳
        /// </summary>
        /// <param name="sid">报告心跳的zoneMaster</param>
        /// <param name="regionName">报告的主域名称</param>
        /// <returns></returns>
        [HttpGet]
        [Route("{regionName}")]
        [SwaggerOperation(Tags = new[] { "gServer" })]
        public IActionResult sayAliveToRegionMaster([FromRoute]string regionName, [FromQuery]string sid)
        {
            if (localRunServer.Instance.regionRole != ServerRoleEnum.regionMaster)
                return FailedMsg(502,"not a regionMaster");

            regionZoneServer regionZone = getRegion(regionName);


            var server = regionZone.getServerById(sid);
            if (server != null)
                server.lastLive = DateTime.Now;
            return noContentStatus(HttpStatusCode.OK);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("{id}")]
        [SwaggerOperation(Tags = new[] { "gServer" })]
        public IActionResult sayAliveToZoneMaster([FromRoute]string id)
        {
            if (localRunServer.Instance.zoneRole != ServerRoleEnum.zoneMaster)
                return FailedMsg(502,"not a zoneMaster");
            regionZoneServer regionZone = getRegion(localRunServer.Instance.region);
           
            var server = regionZone.getServerById(id);
            if (server != null)
                server.lastLive = DateTime.Now;
            return noContentStatus(HttpStatusCode.OK);
        }

        /// <summary>
        /// 响应请求，返回当前服务器的Id
        /// </summary>
        /// <returns></returns>
        [HttpGet]
      
        [SwaggerOperation(Tags = new[] { "gServer" })]
        public IActionResult echoFor()
        {
            return SuccessData(localRunServer.Instance.ownServer.id);

        }
        /// <summary>
        /// 向集群主控服务器注册
        /// </summary>
        /// <param name="server"></param>
        /// <returns></returns>
        [HttpPost]
        [SwaggerOperation(Tags = new[] { "gServer" })]
        public IActionResult registerToZoneMaster([FromBody]proxyNettyServer server)
        {
            if (localRunServer.Instance.zoneRole != ServerRoleEnum.zoneMaster)
                 return FailedMsg(400,"not a zoneMaster");
            var zoneCluster = getZone(localRunServer.Instance.zone, localRunServer.Instance.region);
            if(zoneCluster.clusterId!=server.clusterID)
                return FailedMsg(400,"not a same cluster");
            Object o;
            lock (zoneCluster)
            {
                if (zoneCluster.slave == null)
                {
                    zoneCluster.setSlave(server);
                    o = new { serverType = (int)ServerRoleEnum.zoneSlave };
                }
                else
                {
                    zoneCluster.addRepetion(server);
                     o = new { serverType = (int)ServerRoleEnum.zoneRepetiton };
                }
            }
            return SuccessData(o);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="zsc"></param>
        /// <returns></returns>
        [HttpPost]       
        [SwaggerOperation(Tags = new[] { "gServer" })]
        public IActionResult registerToRegionMaster([FromBody]zoneServerCluster zsc)
        {

            if (localRunServer.Instance.zoneRole != ServerRoleEnum.regionMaster)
                return FailedMsg(400, "not a zoneMaster");
            if (zsc == null || zsc.master == null)
                return FailedMsg(400, "param error");
            var zoneName = zsc.zoneName;
            var server = zsc.master;
            var regionZone = getRegion(localRunServer.Instance.region);
            var zoneCluster = getZone(zoneName, localRunServer.Instance.region);
            proxyNettyServer oldmaster = null;
            
            if (zoneCluster!=null)
                oldmaster = zoneCluster.master;

            if(oldmaster!=null && oldmaster.status==serverStatusEnum.Ready && oldmaster.id!=server.id)
                return FailedMsg(400, "the  cluster master is already run");

            if (zoneCluster.clusterId == zsc.clusterId)
            {
                foreach (var one in zoneCluster.allAvailableServers())
                {
                    if (one.id != server.id)
                    {
                        if (zsc.slave == null)
                            zsc.setSlave(one);
                        else
                            zsc.addRepetion(one);
                    }
                }
            }
            

            Object o;
            lock (regionZone)
            {
                if(regionZone.zoneServer_dic.ContainsKey(zsc.zoneName))
                   regionZone.zoneServer_dic.Remove(zsc.zoneName);
                regionZone.zoneServer_dic.Add(zsc.zoneName, zsc);
               
                if (regionZone.regionSlave == null)
                {
                    regionZone.setSlave(server);
                    o = new { serverType = (int)ServerRoleEnum.regionSlave };
                }
                else
                {
                    
                    o = new { serverType = (int)ServerRoleEnum.unkown };
                }

            }
            return SuccessData(o);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id">被通知server的Id</param>
        /// <param name="am">通知消息</param>
        /// <returns></returns>
        [HttpPost]
        [Route("{id}")]
        [SwaggerOperation(Tags = new[] { "gServer" })]
        public IActionResult NoticeServer([FromRoute]string id,[FromBody]actionMessage am)
        {
            if (localRunServer.Instance.ownServer.id != id)
                return noContentStatus(HttpStatusCode.BadRequest);
            else
            {
                localRunServer.Instance.addActions(am);
                return SuccessMsg("ok");
            }
        }
        /// <summary>
        /// 获取当前zone的集群信息，请求的服务器应该和当前访问器是同一个region
        /// </summary>
        /// <param name="zoneName"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("{zoneName}")]
        [SwaggerOperation(Tags = new[] { "gServer" })]
        public IActionResult zone([FromRoute]string zoneName)
        {
            if (string.IsNullOrEmpty(zoneName))
                return FailedMsg(404,"bad zone name");
            var zone = getZone(zoneName, localRunServer.Instance.region);
            if (zone == null)
                return FailedMsg(404, "no found");
             return SuccessData(zone);
        }
        /// <summary>
        /// 获取整个区域的集群信息
        /// </summary>
        /// <param name="regionName">区域名</param>
        /// <returns></returns>
        [HttpGet]
        [Route("{regionName}")]
        [SwaggerOperation(Tags = new[] { "gServer" })]
        public IActionResult region([FromRoute]string regionName)
        {
            if (string.IsNullOrEmpty(regionName))
                return FailedMsg(400,"bad region name");

            if (!localRunServer.Instance.region_dic.ContainsKey(regionName)
                || !localRunServer.Instance.region_dic.ContainsKey("*"))
                return FailedMsg(404, "no found");

            regionZoneServer regionZone=getRegion(regionName);
            

            return SuccessData(regionZone);

        }

        [HttpGet]
        [Route("{id}")]
        [SwaggerOperation(Tags = new[] { "gServer" })]
        public IActionResult pNServer([FromRoute]string id,[FromQuery]string regionName,[FromQuery]string zoneName)
        {

            var zone = getZone(zoneName, regionName);
            if (zone != null)
            {
                var server = zone.getzoneServerById(id);
                if (server != null)
                    return SuccessData(server);
            }
            return FailedMsg("not found server");
        }
        #endregion

    }
}
