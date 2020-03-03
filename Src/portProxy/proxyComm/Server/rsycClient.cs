using System;
using System.Collections.Generic;
using System.Xml;
using System.Configuration;
using log4net;
using System.IO;
using System.Net;

using Newtonsoft.Json;

using System.Reflection;

using System.Collections.Concurrent;
using Proxy.Comm;
using FrmLib.Log;
using System.Timers;

namespace Proxy.Comm
{
    using Ace;
    using FrmLib.Http;
    using Microsoft.Extensions.Configuration;
    using Newtonsoft.Json.Linq;
    using System.Linq;
    using System.Xml.Serialization;

    /// <summary>
    /// {0}同一是服务器的url地址
    /// </summary>
    public class rsycClient
    {
        public zoneServerCluster ownCluser { get { return localRunServer.Instance.ownCluster; } }
        public regionZoneServer ownRegion{get { return localRunServer.Instance.ownRegion; } }
        public proxyNettyServer ownServer { get { return localRunServer.Instance.ownServer; } }
        public string region { get { return localRunServer.Instance.region; } }
        private  HttpHelper hclient = new HttpHelper(new TimeSpan(0, 0, 5));
        #region urls
        /// <summary>
        ///  slave和副本服务器向集群主服务器报告心跳url，get请求，参数是当前服务器的id
        /// </summary>
        private string sayAliveToZoneMasterUrl =  "{0}/sysconf/gserver/sayAliveToZoneMaster/{1}";
        /// <summary>
        ///  slave和副本服务器向域主服务器报告心跳url,get请求，参数是当前服务器id和区域名称
        /// </summary>
        private string sayAliveToRegionMasterUrl = "{0}/sysconf/gserver/sayAliveToRegionMaster/{1}?regionName={1}";
        /// <summary>
        /// 向zone主服务器获取区域的集群信息（zone中的从服务器和副本向zone的主服务器，或者各zone主服务器向region主服务器获取）,get请求,参数是域名称
        /// </summary>
        private string getRegionUrl = "{0}/sysconf/gserver/region/{1}";
        /// <summary>
        /// 向zone主服务器获取本zone集群信息
        /// </summary>
        private string getZoneClustUrl = "{0}/sysconf/gserver/zone/{1}";
        /// <summary>
        /// 通知指定服务器处理消息,post请求，参数为服务器id；请求body中为actionmessage
        /// </summary>
        private string zoneMasterNoticeUrl = "{0}/sysconf/gserver/NoticeServer/{1}";
        /// <summary>
        /// 区域主服务器通知zone服务器有zone集群的变更通知，各zone服务器应该向区域主服务器获取新的Region信息（包含该region的主服务器和从服务器信息）；
        /// zone主从服务的变更应该有region服务器下发到服务器更新集群信息
        /// </summary>
        [Obsolete]
        private string regionMasterNoticeUrl = "/sysconf/gserver/";

        /// <summary>
        /// 域服务器设置集群主服务器
        /// </summary>
        [Obsolete]
        private string regionSetZoneMasterUrl = "/sysconf/gserver/";
        /// <summary>
        /// 请求服务器响应，返回服务端ID,get请求，无参数，返回服务器Id给对方
        /// </summary>
        private string serverEchoUrl = "{0}/sysconf/gserver/echoFor";
        /// <summary>
        /// 向域主服务器注册，post请求，报文参数为ZoneServerCluster
        /// </summary>
        private string registerForRegionUrl = "{0}/sysconf/gserver/registerToRegionMaster";
        /// <summary>
        /// 向集群主控注册，post请求，报文为ProxyNettyServer
        /// </summary>
        private string registerForZoneUrl = "{0}/sysconf/gserver/registerToRegionMaster";

        /// <summary>
        /// 获取一个proxyNettyServer的详细信息，get请求，参数包括：服务器ID，域名，集群名
        /// </summary>
        private string getServerUrl = "{0}/sysconf/gserver/pNServer/{1}?regionName={2}&zoneName={3}";
        #endregion
        private  bool checkOk(JObject jobj)
        {
            return FrmLib.Extend.tools_static.jobjectHaveKey(jobj, "Status") && jobj["Status"].ToObject<int>() == 200;
        }
        /// <summary>
        //  从主集群服务器获取本集群信息
        /// </summary>
        /// <returns></returns>
        public zoneServerCluster getClusterFromZoneMaster(string url)
        {
            try
            {
                if (ownCluser != null && ownCluser.master != null && ownCluser.master.id != ownServer.id)
                {
                    var respone = hclient.doAsycHttpRequest(string.Format(getZoneClustUrl, ownCluser.master.serviceUrl));
                    if (respone.IsSuccessStatusCode)
                    {
                        var jobj = JObject.Parse(hclient.ResponseToString(respone));
                        if (checkOk(jobj))
                        {
                            zoneServerCluster zsc = zoneServerCluster.parlseJson(jobj["data"].ToString());
                            return zsc;
                        }
                        else
                        {
                            FrmLib.Log.commLoger.runLoger.Error(string.Format("server {0} getClusterFromZoneMaster to{1} error ,resopne:{2}", ownServer.id, ownCluser.master.serviceUrl, jobj.ToString()));
                        }

                    }
                    FrmLib.Log.commLoger.runLoger.Error(string.Format("server {0} getClusterFromZoneMaster to{1} error ,resopne:{2}", ownServer.id, ownCluser.master.serviceUrl, respone.ToString()));
                }
                else
                {
                    FrmLib.Log.commLoger.runLoger.Error(string.Format("server {0} getClusterFromZoneMaster to{1} is this own  ", ownServer.id, ownCluser.master.serviceUrl));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("some error:ex:" + FrmLib.Extend.tools_static.getExceptionMessage(ex));
                return null;
            }
            return null;
        }
        /// <summary>
        /// 获取全域信息,非主域控服务器
        /// </summary>
        /// <returns></returns>
        public regionZoneServer getRegionFromRegionMaster(string url)
        {


            try
            {
                if (ownRegion != null && ownRegion.regionMaster != null && ownRegion.regionMaster.id != ownServer.id)
                {
                    var respone = hclient.doAsycHttpRequest(string.Format(getRegionUrl, ownRegion.regionMaster.serviceUrl));
                    if (respone.IsSuccessStatusCode)
                    {
                        var jobj = JObject.Parse(hclient.ResponseToString(respone));
                        if (checkOk(jobj))
                        {
                            regionZoneServer zsc = regionZoneServer.parlseJson(jobj["data"].ToString());
                            return zsc;
                        }
                        else
                        {
                            FrmLib.Log.commLoger.runLoger.Error(string.Format("server {0} getRegionFromRegionMaster to{1} error ,resopne:{2}", ownServer.id, ownCluser.master.serviceUrl, jobj.ToString()));
                        }

                    }
                    FrmLib.Log.commLoger.runLoger.Error(string.Format("server {0} getRegionFromRegionMaster to{1} error ,resopne:{2}", ownServer.id, ownCluser.master.serviceUrl, respone.ToString()));
                }
                else
                {
                    FrmLib.Log.commLoger.runLoger.Error(string.Format("server {0} getRegionFromRegionMaster to{1} is this own  ", ownServer.id, ownCluser.master.serviceUrl));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("getRegionFromRegionMaster some error:ex:" + FrmLib.Extend.tools_static.getExceptionMessage(ex));
                return null;
            }
            return null;
        }
        public  string getEchoServerId(string urls)
        {

            var respone = hclient.doAsycHttpRequest(string.Format(serverEchoUrl, urls), new JObject(), true, urls.ToLower().StartsWith("https"));
            JObject jobj = null;
            if (respone.IsSuccessStatusCode)
            {
                jobj = JObject.Parse(hclient.ResponseToString(respone));
                if (checkOk(jobj))
                    return jobj["Data"].ToString();


            }
            else
            {
                FrmLib.Log.commLoger.runLoger.Error(string.Format("server {0} say live to{1} error ,resopne:{2}",  ownServer.id, ownCluser.master.serviceUrl, respone.ToString()));
            }

            return null;
        }

        /// <summary>
        /// 向主集群服务器报告心跳（集群从服务器或副本）
        /// </summary>
        public void sayAliveToZoneMaster()
        {
          


            try
            {
                if (ownCluser != null && ownCluser.master != null && ownCluser.master.id != ownServer.id)
                {
                    var respone = hclient.doAsycHttpRequest(string.Format(sayAliveToZoneMasterUrl, ownCluser.master.serviceUrl, this.ownServer.id), new JObject(), true, (ownCluser.master.serviceUrl).ToLower().StartsWith("https"));
                    if (respone.IsSuccessStatusCode && checkOk(JObject.Parse(hclient.ResponseToString(respone))))
                    {

                    }
                    else
                    {
                        FrmLib.Log.commLoger.runLoger.Error(string.Format("server {0} say live to{1} error ,resopne:{2}", ownServer.id, ownCluser.master.serviceUrl, respone.ToString()));
                    }
                }
            }
            catch (Exception ex)
            {

            }
            finally
            {
               
            }

        }
        /// <summary>
        /// 向主域服务器报告心跳（集群主服务器，非主域服务器）
        /// </summary>
        public void sayAliveToRegionMaster()
        {
           
            try
            {
                if (ownRegion != null && ownRegion.regionMaster != null && ownRegion.regionMaster.id != ownServer.id)
                {
                    var respone = hclient.doAsycHttpRequest(string.Format(sayAliveToRegionMasterUrl, ownRegion.regionMaster.serviceUrl, this.ownServer.id));

                    if (respone.IsSuccessStatusCode && checkOk(JObject.Parse(hclient.ResponseToString(respone))))
                    {

                    }
                    else
                    {
                        FrmLib.Log.commLoger.runLoger.Error(string.Format("server {0} say live to{1} error ,resopne:{2}", ownServer.id, ownCluser.master.serviceUrl, respone.ToString()));
                    }
                }
            }
            catch (Exception ex)
            { }
            finally
            {
                
            }
        }


        /// <summary>
        /// 向主集群服务器注册（集群从服务器或副本）,返回数据中
        /// data.type代表是从服务器还是副本
        /// </summary>
        public  JObject registerForZoneMaster(zoneServerCluster zsc)
        {
            if (this.ownServer != null && localRunServer.Instance.zoneRole != ServerRoleEnum.zoneMaster)
            {
                var respone = hclient.doAsycHttpRequest(string.Format(registerForZoneUrl, zsc.master.serviceUrl),JObject.FromObject(this.ownServer),false,zsc.master.serviceUrl.ToLower().StartsWith("https"));

                if (respone.IsSuccessStatusCode )
                {
                    return JObject.Parse(hclient.ResponseToString(respone));
                }
                else
                {
                    FrmLib.Log.commLoger.runLoger.Error(string.Format("server {0} registerForZoneMaster {1} error ,resopne:{2}", ownServer.id, zsc.master.serviceUrl, respone.ToString()));
                }
            }
            return null;
        }
        /// <summary>
        /// 向主域服务器注册（集群主服务器）
        /// 
        /// </summary>
        public  JObject   registerForRegionMaster(regionZoneServer rzs)
        {
            if (this.ownServer != null && localRunServer.Instance.zoneRole != ServerRoleEnum.regionMaster)
            {
                var respone = hclient.doAsycHttpRequest(string.Format(registerForZoneUrl, rzs.regionMaster.serviceUrl), JObject.FromObject(this.ownServer), false, rzs.regionMaster.serviceUrl.ToLower().StartsWith("https"));

                if (respone.IsSuccessStatusCode)
                {
                    return JObject.Parse(hclient.ResponseToString(respone));
                }
                else
                {
                    FrmLib.Log.commLoger.runLoger.Error(string.Format("server {0} registerForRegionMaster {1} error ,resopne:{2}", ownServer.id, rzs.regionMaster.serviceUrl, respone.ToString()));
                }
            }
            return null;


        }
        /// <summary>
        /// 通知服务器列表重新设置域主服务器
        /// </summary>
        /// <param name="noticeList">被通知列表</param>
        /// <param name="masterServer">域主服务器</param>

        public  void noticeRegionSetMaster(IList<proxyNettyServer> noticeList,proxyNettyServer masterServer)
        {
            foreach (var one in noticeList)
            {
                if (one.id == ownServer.id)
                    continue;
                var jobj = (new JObject());
                jobj.Add("url", masterServer.serviceUrl);
                actionMessage am = new actionMessage(enum_Actiondo.noticeToRsycRegionMaster, ownServer.id, this.region, one.id, this.region, jobj.ToString());
                var respone = hclient.doAsycHttpRequest(string.Format(zoneMasterNoticeUrl, one.serviceUrl,one.id), JObject.FromObject(am), false, one.serviceUrl.ToLower().StartsWith("https"));

                if (respone.IsSuccessStatusCode)
                {
                    continue;
                }
                else
                {
                    FrmLib.Log.commLoger.runLoger.Error(string.Format("server {0} noticeRegionSetMaster {1} error ,resopne:{2}", ownServer.id,one.serviceUrl, respone.ToString()));
                }
            }
        }
        /// <summary>
        /// 通知服务器列表重新设置集群主服务器
        /// </summary>
        /// <param name="noticeList">被通知列表</param>
        /// <param name="masterServer">集群主服务器</param>

        public  void noticeZoneSetMaster(IList<proxyNettyServer> noticeList, proxyNettyServer masterServer)
        {
            foreach (var one in noticeList)
            {
                if (one.id == ownServer.id)
                    continue;
                var jobj = (new JObject());
                jobj.Add("url", masterServer.serviceUrl);
                actionMessage am = new actionMessage(enum_Actiondo.noticeToRsycZoneMaster, ownServer.id, this.region, one.id, this.region, jobj.ToString());
                var respone = hclient.doAsycHttpRequest(string.Format(zoneMasterNoticeUrl, one.serviceUrl, one.id), JObject.FromObject(am), false, one.serviceUrl.ToLower().StartsWith("https"));

                if (respone.IsSuccessStatusCode)
                {
                    continue;
                }
                else
                {
                    FrmLib.Log.commLoger.runLoger.Error(string.Format("server {0} noticeZoneSetMaster {1} error ,resopne:{2}", ownServer.id, one.serviceUrl, respone.ToString()));
                }
            }
        }
    }
}
