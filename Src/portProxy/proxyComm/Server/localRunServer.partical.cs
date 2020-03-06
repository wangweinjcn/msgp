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
    using DotNetty.Transport.Bootstrapping;
    using DotNetty.Transport.Channels;
    using DotNetty.Transport.Channels.Sockets;
    using FrmLib.Extend;
    using FrmLib.Http;
    using Microsoft.Extensions.Configuration;
    using Newtonsoft.Json.Linq;
    using Proxy.Comm.model;
    using System.Linq;
    using System.Xml.Serialization;
    public partial class localRunServer
    {
         #region 方法
        
       

        private void regionCheckAlive()
        {
            if (ownRegion == null || ownRegion.regionMaster==null ||ownRegion.regionMaster.id!=ownServer.id)
                return;
            checkOneProxyServer(ownRegion.regionMaster);
            checkOneProxyServer(ownRegion.regionSlave);
            foreach (var obj in ownRegion.allZoneServerClusters())
            {
                if (obj.master == null || obj.master.id == ownServer.id)
                    continue;
                checkOneProxyServer(obj.master);


            }
            
        }
        
        /// <summary>
        /// 代理服务器监测转发服务组
        /// </summary>
        private void checkMapGroup(proxyNettyServer server)
        {
            if (server.checkMapGroupChange())
            {
                actionMessage am = new actionMessage(enum_Actiondo.ServerChanged, ownServer.id, region, "", "", JsonConvert.SerializeObject(new { url = ownServer.serviceUrl, region = region, zone = zoneclusterId, id = server.id }));
                addActions(am);
            }
        }
        /// <summary>
        /// 检查单台服务器，失联时间超过remove时间，移除并不在检查；小于移除时间大于失效时间，设置失效状态，但进行检查
        /// </summary>
        /// <param name="server"></param>
        private void checkOneProxyServer(proxyNettyServer server)
        {
           
            if (server == null)
                return;
            if (server.id == ownServer.id)
                return;
            if (server.status == serverStatusEnum.Disable)
                return;
            var ts = (DateTime.Now - server.lastLive).TotalSeconds;
             var oldstatus = server.status;
            if (ts > this.serverRemoveTimes)
            {
                //should remove server
                Console.WriteLine("checkOneProxyServer server set Disable ");
                server.setStatus(serverStatusEnum.Disable);
            }
            else
            {
                if (ts > this.serverFailTimes)
                {
                    var sid = _rsycClient.getEchoServerId(server.serviceUrl);
                    if (sid == server.id)
                    {
                        Console.WriteLine("checkOneProxyServer server set ready ");
                        server.lastLive = DateTime.Now;
                        server.setStatus(serverStatusEnum.Ready);
                    }
                    else
                    {
                        Console.WriteLine("checkOneProxyServer server set fail ");
                        server.setStatus(serverStatusEnum.Fail);

                    }

                }
            }
            
            if (oldstatus != server.status)
            {
                Console.WriteLine("checkOneProxyServer server add actions ");
                actionMessage am = new actionMessage(enum_Actiondo.ServerChanged, ownServer.id, region, "", "", JsonConvert.SerializeObject(new { url = ownServer.serviceUrl, region = region, zone = zoneclusterId, id = server.id }));
                addActions(am);
            }
        }
        /// <summary>
        /// 域主控广播域服务消息
        /// 广播机制：域主服务器向自己知道的所有服务器推送广播消息，确认自己是主服务器；消息中包含所有接受消息的服务器Id，收到消息的服务器，向自己知道的域服务器且不在发送列表中的服务器转发消息；消息结构参考regionBroadCastMessage；任何一个收到该消息的域主服务器应和自己进行选主服务器操作；如果发生主服务器变更，则向自己知道的服务器发送向域主服务器同步命令
        /// </summary>
        /// <param name="state"></param>
        /// <param name="e"></param>
        public void broadCastForRegion(Object state, System.Timers.ElapsedEventArgs e)
        {
            if (bcNowDo)
                return;

            lock (this.bcLockobj)
            {
                if (!bcNowDo)
                    bcNowDo = true;

            }
            try
            {

            }
            catch (Exception ex)
            { }
            finally
            {
                bcNowDo = false;
            }
        }
        private void clusterCheckAlive()
        {
           
            if (ownCluster == null || ownCluster.master.id != ownServer.id)
                return;
            if (ownCluster.slave != null)
            {
               
                checkOneProxyServer(ownCluster.slave);
            }
            foreach (var obj in ownCluster.repetionList)
            {

                checkOneProxyServer(obj);

            }
        }


        /// <summary>
        /// 选择域主控服务器
        /// </summary>
        /// <param name="serverlist">从域中获取到主服务器列表</param>
        /// <returns></returns>
        private proxyNettyServer selectRegionMasterServer(List<proxyNettyServer> serverlist)
        {
            if (serverlist == null || serverlist.Count == 0)
                return null;
            if (serverlist.Count == 1)
                return serverlist[0];

            proxyNettyServer masterServer=null;
            IList<proxyNettyServer> waitforNextList = new List<proxyNettyServer>();
           
            var olddt = (from x in serverlist select x).Min(a => a.createDt);
            waitforNextList = (from x in serverlist where x.createDt == olddt select x).ToList();
            if (waitforNextList.Count > 1)
            {
                int zoneCount = int.MinValue;
                proxyNettyServer maxZoneCountServer = null;
                regionZoneServer maxrzs = null;
                IList<Tuple<proxyNettyServer, regionZoneServer>> waitforNextList2 = new List<Tuple<proxyNettyServer, regionZoneServer>>();

                //比较包含集群的数量
                foreach (var one in waitforNextList)
                {
                    var rzs = _rsycClient.getRegionFromRegionMaster(one.serviceUrl);
                    if (rzs.zoneServerCount > zoneCount)
                    {
                        maxZoneCountServer = one;
                        maxrzs = rzs;
                        zoneCount = rzs.zoneServerCount;
                        waitforNextList2.Clear();
                     
                    }
                    else
                    {
                        if (rzs.zoneServerCount == zoneCount)
                        {
                            if (maxZoneCountServer.id == one.id)
                                continue;//相同的server
                            waitforNextList2.Add(new Tuple<proxyNettyServer, regionZoneServer>(one,rzs));
                           
                        }
                    }
                        
                }

                waitforNextList2.Add(new Tuple<proxyNettyServer, regionZoneServer>( maxZoneCountServer,maxrzs));
                if (waitforNextList2.Count > 1)
                {
                    long serverCount = int.MinValue;
                    proxyNettyServer maxCountServer = null;
                    IList<proxyNettyServer> waitforNextList3 = new List<proxyNettyServer>();
                    //比较总服务器数量
                    for (int i = 0; i < waitforNextList2.Count; i++)
                    {
                        var obj = waitforNextList2[i].Item2;
                        var tmp = obj.getServerCount();
                        if (tmp > serverCount)
                        {
                            serverCount = tmp;
                            maxCountServer = waitforNextList2[i].Item1;
                            waitforNextList3.Clear();
                        }
                        else
                        {
                            if (tmp == serverCount)
                            {
                                if (maxCountServer.id == waitforNextList2[i].Item1.id)
                                    continue;
                                waitforNextList3.Add(waitforNextList2[i].Item1);
                            }
                        }
                    }
                    waitforNextList3.Add(maxCountServer);
                    if (waitforNextList3.Count > 1)
                    {
                        //仍然相等，不做处理,返回第一个
                        return waitforNextList3[0];
                    }
                    else
                    {
                        masterServer = waitforNextList3[0];
                       
                       
                    }

                }
                else
                {
                    masterServer = waitforNextList2[0].Item1;
                 

                }
            }
            else
            {
                masterServer = waitforNextList[0];
                
            }
              _rsycClient.noticeRegionSetMaster(serverlist, masterServer);//通知相关的服务器设置master
            return masterServer;
        }
        /// <summary>
        /// 选集群主控
        /// </summary>
        /// <param name="serverlist"></param>
        /// <returns></returns>
        private proxyNettyServer selectZoneMasterServer(List<proxyNettyServer> serverlist)
        {
            if (serverlist == null || serverlist.Count == 0)
                return null;
            if (serverlist.Count == 1)
                return serverlist[0];

            proxyNettyServer masterServer = null;
            IList<proxyNettyServer> waitforNextList = new List<proxyNettyServer>();

            var olddt = (from x in serverlist select x).Min(a => a.createDt);
            //取最早的服务器为主服务器
            waitforNextList = (from x in serverlist where x.createDt == olddt select x).ToList();
            if (waitforNextList.Count > 1)
            {
                int zoneCount = int.MinValue;
                proxyNettyServer maxZoneCountServer = null;
                zoneServerCluster maxrzs = null;
                IList<Tuple<proxyNettyServer, zoneServerCluster>> waitforNextList2 = new List<Tuple<proxyNettyServer, zoneServerCluster>>();

                //比较包含服务器的数量
                foreach (var one in waitforNextList)
                {
                    var zcluster = _rsycClient.getClusterFromZoneMaster(one.serviceUrl);
                    if (zcluster.getZoneServerCount() > zoneCount)
                    {
                        maxZoneCountServer = one;
                        maxrzs = zcluster;
                        zoneCount = zcluster.getZoneServerCount();
                        waitforNextList2.Clear();

                    }
                    else
                    {
                        if (zcluster.getZoneServerCount() == zoneCount)
                        {
                            if (maxZoneCountServer.id == one.id)
                                continue;//相同的server
                            waitforNextList2.Add(new Tuple<proxyNettyServer, zoneServerCluster>(one, zcluster));

                        }
                    }

                }

                waitforNextList2.Add(new Tuple<proxyNettyServer, zoneServerCluster>(maxZoneCountServer, maxrzs));
                if (waitforNextList2.Count > 1)
                {
                    long serverCount = int.MinValue;
                    proxyNettyServer maxCountServer = null;
                    IList<proxyNettyServer> waitforNextList3 = new List<proxyNettyServer>();
                    //比较包含转发组数量
                    for (int i = 0; i < waitforNextList2.Count; i++)
                    {
                        var obj = waitforNextList2[i].Item1;
                        var tmp = obj.mapGroupCount;
                        if (tmp > serverCount)
                        {
                            serverCount = tmp;
                            maxCountServer = waitforNextList2[i].Item1;
                            waitforNextList3.Clear();
                        }
                        else
                        {
                            if (tmp == serverCount)
                            {
                                if (maxCountServer.id == waitforNextList2[i].Item1.id)
                                    continue;
                                waitforNextList3.Add(waitforNextList2[i].Item1);
                            }
                        }
                    }
                    waitforNextList3.Add(maxCountServer);
                    if (waitforNextList3.Count > 1)
                    {
                        //仍然相等，不做处理,返回第一个
                        return waitforNextList3[0];
                    }
                    else
                    {
                        masterServer = waitforNextList3[0];
                      
                    }

                }
                else
                {
                    masterServer = waitforNextList2[0].Item1;
                    

                }
            }
            else
            {
                masterServer = waitforNextList[0];
               
            }
              _rsycClient.noticeZoneSetMaster(serverlist, masterServer); //通知集群设置主服务器
            return masterServer;
        }
        
        /// <summary>
        /// 获取域主服务器
        /// </summary>
        /// <returns></returns>
        public proxyNettyServer getRegionMaster()
        {
            List<proxyNettyServer> waitforList = new List<proxyNettyServer>();

            foreach (var obj in regionUrls)
            {
                var rzs = _rsycClient.getRegionFromRegionMaster(obj);
                if (rzs == null ||(rzs.region!=this.region && rzs.region!="*")|| rzs.regionMaster==null)
                    continue;
                waitforList.Add(rzs.regionMaster);
            }
            var regionMaster = selectRegionMasterServer(waitforList);
            return regionMaster;
        }
        /// <summary>
        /// 获取集群主控
        /// </summary>
        /// <param name="zoneUrls"></param>
        /// <returns></returns>
        public proxyNettyServer getZoneMaster(List<string> zoneUrls)
        {
            List<proxyNettyServer> waitforList = new List<proxyNettyServer>();

            foreach (var obj in regionUrls)
            {
                var rzs = _rsycClient.getClusterFromZoneMaster(obj);
                if (rzs == null || (rzs.clusterId != this.zoneclusterId && rzs.zoneName != "*") || rzs.master == null)
                    continue;
                waitforList.Add(rzs.master);
            }
            var regionMaster = selectZoneMasterServer(waitforList);
            return regionMaster;
        }
      
        public void addActions(actionMessage am         )
        {
            if (am.action == enum_Actiondo.ServerChanged)
            {
                var count = (from x in messagesNeedAction where x.action == enum_Actiondo.ServerChanged select x).Count();
                if (count > 0)
                    return;
            }
            messagesNeedAction.Add(am);
        }
        /// <summary>
        /// 向主集群服务器报告心跳（集群从服务器或副本）
        /// </summary>
        private void triggerSayAliveToZoneMaster(Object state, System.Timers.ElapsedEventArgs e)
        {
            if (sayaliveNowDo)
                return;

            lock (aliveLockobj)
            {
                if (!sayaliveNowDo)
                    sayaliveNowDo = true;
              
            }
            try
            {
                  _rsycClient.sayAliveToZoneMaster();
            }
            catch (Exception ex)
            { }
            finally
            {
                sayaliveNowDo = false;
            }

        }
        /// <summary>
        /// 向主域服务器报告心跳（集群主服务器，非主域服务器）
        /// </summary>
        private void triggerSayAliveToRegionMaster(Object state, System.Timers.ElapsedEventArgs e)
        {
            if (sayaliveNowDo)
                return;

            lock (aliveLockobj)
            {
                if (!sayaliveNowDo)
                    sayaliveNowDo = true;

            }
            try
            {
                _rsycClient.sayAliveToRegionMaster();
            }
            catch (Exception ex)
            { }
            finally
            {
                sayaliveNowDo = false;
            }
        }
        private void triggerCheckAlive(Object state, System.Timers.ElapsedEventArgs e)
        {
            if (checkAliveNowDo)
                return;
            lock (checkAliveLockObj)
            {
                if (checkAliveNowDo)
                    return;
                checkAliveNowDo = true;
            }
            try
            {
                if (this.regionRole == ServerRoleEnum.regionMaster)
                {
                    regionCheckAlive();
                }
                if (this.zoneRole == ServerRoleEnum.zoneMaster)
                {
                    clusterCheckAlive();
                }
                checkMapGroup(ownServer); //检查自己的转发服务器的存活
            }
            finally
            {
                checkAliveNowDo = false;
            }
        }
        /// <summary>
        /// actionMessage 处理器
        /// </summary>
        /// <param name="state"></param>
        /// <param name="e"></param>
        private void triggerActionMessageProcesser(Object state, System.Timers.ElapsedEventArgs e)
        {
            if (actionPNowDo)
                return;
            lock (actionLockobj)
            {
                if (!actionPNowDo)
                    actionPNowDo = true;
            }
            IList<actionMessage> processlist;
            lock (messagesNeedAction)
            {
                processlist = messagesNeedAction;
                messagesNeedAction = new List<actionMessage>();
            }

            try {
                
                while(processlist.Count>0)
                {
                    var one = processlist[0];
                    switch (one.action)
                    {
                        case (enum_Actiondo.noticeToRsycZoneMaster):
                            break;
                        case (enum_Actiondo.noticeToRsycRegionMaster):
                            break;
                        case (enum_Actiondo.broadcastFromRegionMaster):
                            break;
                        case (enum_Actiondo.resetZoneMasterServer):
                            break;
                        case (enum_Actiondo.resetZoneServers):

                            break;
                        case (enum_Actiondo.resetRegionMasterServer):
                            break;
                        case (enum_Actiondo.resetRegionServers):
                            break;
                        case (enum_Actiondo.resetRegionZones):
                            break;
                        case (enum_Actiondo.serverAdd):
                          
                        case (enum_Actiondo.ServerChanged):
                            if (zoneRole != ServerRoleEnum.zoneMaster)
                            {
                                _rsycClient.reportMyChangeToMaster();
                            }
                            break;

                        case (enum_Actiondo.serverRemove):
                            break;
                        case (enum_Actiondo.needNoticeServer):
                            if (zoneRole == ServerRoleEnum.zoneMaster)
                            {
                                actionMessage am = new actionMessage(enum_Actiondo.needRsycOneServer, ownServer.id, region, null, null, one.messageParam);
                                if (ownCluster.slave != null)
                                    _rsycClient.noticeServerWithMessage(ownCluster.slave.serviceUrl, ownCluster.slave.id, am);
                                foreach(var obj in ownCluster.repetionList)
                                     _rsycClient.noticeServerWithMessage(obj.serviceUrl, obj.id, am);

                                     
                            }
                            break;
                        case (enum_Actiondo.needRsycOneServer):
                            if (zoneRole != ServerRoleEnum.zoneMaster)
                            {
                                var jobj = JObject.Parse(one.messageParam);
                                var url = jobj["url"].ToString();
                                var region = jobj["region"].ToString();
                                var zone = jobj["zone"].ToString();
                                var id = jobj["id"].ToString();
                                var newserver = _rsycClient.getServer(url, id, region, zone);
                                if (newserver == null || newserver.id==ownServer.id)
                                    return;
                                var oldserver = ownCluster.getzoneServerById(newserver.id);
                                if (oldserver != null)
                                {
                                    oldserver = newserver;
                                }
                                else
                                {
                                    if (ownCluster.slave == null)
                                        ownCluster.setSlave(newserver);
                                    else
                                        ownCluster.addRepetion(newserver);
                                }
                            }
                            break;

                    }
                    processlist.Remove(one);

}
            }
            catch (Exception ex)
            {
                
            }
            finally
            {
                actionPNowDo = false;
            }
        }
             
       
        /// <summary>
        /// 启动服务
        /// 
        /// </summary>
        public void startServer()
        {
            regionZoneServer rzs = null;
            proxyNettyServer zoneMaster = null;
            zoneServerCluster zsc = null;
            var regionMaster = getRegionMaster();

            if (regionMaster == null)
                this.regionRole = ServerRoleEnum.regionMaster;
            else
            {
                rzs =_rsycClient.getRegionFromRegionMaster(regionMaster.serviceUrl);
                if (rzs.ContainsKey(this.zoneclusterId))
                {
                    var cluster = rzs.getzoneServerCluster(this.zoneclusterId);
                    if (cluster != null && cluster.master != null)
                        zoneMaster = cluster.master;
                    else
                    {
                        //集群存在，但集群的主服务器是空,存在异常，但有可能是集群的主服务器掉线，新的主服务器还没有选出；退出重试
                        throw new Exception("从域服务器获得的集群信息中，主服务器异常");
                    }
                }
                if (zoneMaster != null && !zoneUrls.Contains(zoneMaster.serviceUrl))
                    zoneUrls.Add(zoneMaster.serviceUrl);
                
            }

             zoneMaster = getZoneMaster(zoneUrls);
            if (zoneMaster == null)
            {
                if (rzs != null && rzs.ContainsKey(this.zoneclusterId))
                    throw new Exception("获取不到集群主服务器，但主域的信息中包含了集群的信息");
                zoneRole = ServerRoleEnum.zoneMaster;

            }
            else
            {
                zsc = _rsycClient.getClusterFromZoneMaster(zoneMaster.serviceUrl);
            }
           

            bool isRegionMaster = regionRole == ServerRoleEnum.regionMaster;
            bool isZoneMaster = zoneRole == ServerRoleEnum.zoneMaster;
             string serviceUrl = string.Format("{0}://{1}:{2}", protocol, ownHost, lPort);
            if (isRegionMaster && isZoneMaster)
            {
               
                //既是主域服务器也是主集群服务器
                ownServer = new proxyNettyServer(this.clusterId, this.httpProxyPort, serviceUrl);
                ownServer.loadPortProxyCfg();
                regionZoneServer rz = new regionZoneServer(region);
                rz.addZoneServer(new zoneServerCluster(region, zone, ownServer));
                rz.setMaster(ownServer);
                this.region_dic.Add(region, rz);
                RegionBroadCastTimer.Elapsed += new System.Timers.ElapsedEventHandler(broadCastForRegion);
                RegionBroadCastTimer.AutoReset = true;
                RegionBroadCastTimer.Start();
            }
            else
            {
                if (!isRegionMaster)
                {
                    if (isZoneMaster)
                    {
                        
                       
                                     
                        ownServer = new proxyNettyServer(this.clusterId, this.httpProxyPort, serviceUrl);
                         ownServer.loadPortProxyCfg();
                         zsc = new zoneServerCluster(this.region, this.zone, ownServer);                      
                        var jobj = _rsycClient.registerForRegionMaster(rzs);
                        if (jobj["status"].ToObject<int>()!=200)
                            throw new Exception("服务注册失败");
                        rzs.addZoneServer( zsc);
                        zsc.setMaster( ownServer);
                        this.region_dic.Add(this.region, rzs);
                        sayAliveTimer.Elapsed += new System.Timers.ElapsedEventHandler(triggerSayAliveToRegionMaster);
                        sayAliveTimer.AutoReset = true;
                        sayAliveTimer.Start();
                    }
                    else
                    {
                      

                        this.clusterId = zsc.clusterId;
                        ownServer = new proxyNettyServer(this.clusterId, this.httpProxyPort, serviceUrl);
                        ownServer.loadPortProxyCfg();
                        var jobj = _rsycClient.registerForZoneMaster(zsc);
                        if (jobj["Status"].ToObject<int>() != 100)
                            throw new Exception("服务注册失败");
                        else
                        {
                            if (jobj["Data"]["serverType"].ToObject<int>() == (int)ServerRoleEnum.zoneSlave)
                            {
                                zsc.setSlave(ownServer);
                            }
                            else
                                zsc.addRepetion( ownServer);
                        }

                        rzs.addZoneServer(zsc);
                        this.region_dic.Add(region, rzs);

                        sayAliveTimer.Elapsed += new System.Timers.ElapsedEventHandler(triggerSayAliveToZoneMaster);
                        sayAliveTimer.AutoReset = true;
                        sayAliveTimer.Start();
                    }
                }
                else
                {
                    //是域主控，不是集群主控;这种情况暂不考虑，不应该发生，没有域信息

                    throw new Exception("是域主控，不是集群主控是异常信息");
                }


            }

             actionDoTimer.Start();
             checkAliveTimer.Start();
            ownServer.Start();//启动自身的转发服务
        }
        private void initPcounter(IList<pCounter> perfCounters)
        {
            /*
           perfCounters.Add(PerfCounterHelper.getPerfCategoryByName("Processor").getPCounterByName("% User Time"));
           perfCounters.Add(PerfCounterHelper.getPerfCategoryByName("Processor").getPCounterByName("% Processor Time"));
           perfCounters.Add(PerfCounterHelper.getPerfCategoryByName("Process").getPCounterByName("% Processor Time"));
           perfCounters.Add(PerfCounterHelper.getPerfCategoryByName("Process").getPCounterByName("Thread Count"));
           perfCounters.Add(PerfCounterHelper.getPerfCategoryByName("Process").getPCounterByName("Virtual Bytes"));
           perfCounters.Add(PerfCounterHelper.getPerfCategoryByName("Process").getPCounterByName("Working Set"));
           perfCounters.Add(PerfCounterHelper.getPerfCategoryByName("Process").getPCounterByName("Private Bytes"));
           perfCounters.Add(PerfCounterHelper.getPerfCategoryByName("Mono Memory").getPCounterByName("Total Physical Memory"));
           perfCounters.Add(PerfCounterHelper.getPerfCategoryByName("Mono Memory").getPCounterByName("Allocated Objects"));
           perfCounters.Add(PerfCounterHelper.getPerfCategoryByName(".NET CLR Memory").getPCounterByName("# Gen 0 Collections"));
           perfCounters.Add(PerfCounterHelper.getPerfCategoryByName(".NET CLR Memory").getPCounterByName("# Gen 1 Collections"));
           perfCounters.Add(PerfCounterHelper.getPerfCategoryByName(".NET CLR Memory").getPCounterByName("# Gen 2 Collections"));
           perfCounters.Add(PerfCounterHelper.getPerfCategoryByName(".NET CLR Memory").getPCounterByName("Promoted Memory from Gen 0"));
           perfCounters.Add(PerfCounterHelper.getPerfCategoryByName(".NET CLR Memory").getPCounterByName("Promoted Memory from Gen 1"));
           perfCounters.Add(PerfCounterHelper.getPerfCategoryByName(".NET CLR Memory").getPCounterByName("Gen 1 heap size"));
           perfCounters.Add(PerfCounterHelper.getPerfCategoryByName(".NET CLR Memory").getPCounterByName("Gen 2 heap size"));
           perfCounters.Add(PerfCounterHelper.getPerfCategoryByName(".NET CLR Memory").getPCounterByName("Large Object Heap size"));
           perfCounters.Add(PerfCounterHelper.getPerfCategoryByName("Network Interface").getPCounterByName("Bytes Received/sec"));
           perfCounters.Add(PerfCounterHelper.getPerfCategoryByName("Network Interface").getPCounterByName("Bytes Sent/sec"));
           perfCounters.Add(PerfCounterHelper.getPerfCategoryByName("Mono Threadpool").getPCounterByName("Work Items Added/Sec"));
           perfCounters.Add(PerfCounterHelper.getPerfCategoryByName("Mono Threadpool").getPCounterByName("IO Work Items Added/Sec"));
           perfCounters.Add(PerfCounterHelper.getPerfCategoryByName("Mono Threadpool").getPCounterByName("# of Threads"));
           perfCounters.Add(PerfCounterHelper.getPerfCategoryByName("Mono Threadpool").getPCounterByName("# of IO Threads"));
           */
        }
        #endregion
    }
}
