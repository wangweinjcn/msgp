using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json;
using System.Security.Cryptography.X509Certificates;
using System.Net;
using System.Xml;
using System.IO;
using Proxy.Comm.model;
using FrmLib.Extend;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Handlers.Logging;
using DotNetty.Transport.Channels.Sockets;
using DotNetty.Handlers.Tls;
using DotNetty.Codecs;
using System.Runtime.InteropServices;
using System.Runtime;
using DotNetty.Codecs.Http;

using Proxy.Comm.http;
using Proxy.Comm.socket;
using System.Xml.Serialization;
using FrmLib.Http;

namespace Proxy.Comm.model
{
    /// <summary>
    /// 服务集群实例,代表一个zone
    /// </summary>
    public class zoneServerCluster
    {

        public void  clusterServerChanged(object sender, serverChangeEventArgs e)
        {
            //处理消息
            if (e.newValue.ToString()!=localRunServer.Instance.ownCluster.clusterId)
                return;
            if (e.changeServerId != localRunServer.Instance.ownServer.id)
                return;
            enum_Actiondo ead = enum_Actiondo.unknown;
            if (e.changeType != serverChangeTypeEnum.zoneMasterChanged )
            {
                ead = enum_Actiondo.resetZoneServers;
            }
            else
                ead = enum_Actiondo.resetZoneMasterServer;
             actionMessage am = new actionMessage(ead, e.newValue.ToString(), this.regionName, "", "", "");
            localRunServer.Instance.addActions(am);
        }
        [JsonProperty]
        public string clusterId { get; private set; }

        //在委托的机制下,建立变更事件
        public event serverChangeEvnent clusterChangeEvent;
        //声明一个可重写的OnChange的保护函数
        protected virtual void OnChange(serverChangeEventArgs e)
        {
            if (clusterChangeEvent != null)
            {
                //Sender = this，也就是serverZooCluster
                this.clusterChangeEvent(this, e);
            }
        }

        [JsonProperty]
        public proxyNettyServer master { get; private set; }
        [JsonProperty]
        public proxyNettyServer slave { get; private set; }
        /// <summary>
        /// 副本服务器列表
        /// </summary>
        [JsonProperty]
        public IList<proxyNettyServer> repetionList { get; private set; }

        public proxyNettyServer getAvailableServer(string appkey)
        {
            return null;
        }
        public IList<proxyNettyServer> allAvailableServers()
        {
            List<proxyNettyServer> servers = new List<proxyNettyServer>();
            if (master != null && master.status == serverStatusEnum.Ready)
                servers.Add(master);
            if (slave != null && slave.status == serverStatusEnum.Ready)
                servers.Add(slave);
            foreach(var one in repetionList)
            {

                if (one != null && one.status == serverStatusEnum.Ready)
                    servers.Add(one);
            }
            return servers;
        }
        public zoneServerCluster(string region, string zone, proxyNettyServer masterServer)
        {
            this.clusterId = string.Format("{0}:{1}", region, zone).ToMD5();
            this.zoneName = zone;
            this.master = masterServer;
            this.regionName = region;
            repetionList = new List<proxyNettyServer>();

        }
        [JsonConstructor]
        public zoneServerCluster():this("","",null)
        { }


        public proxyNettyServer getzoneServer(string host, string port)
        {
            if (master.host == host && master.port == port)
                return master;
            else
            {
                if (slave != null && slave.host == host && slave.port == port)
                    return slave;
                else
                {
                    var a = (from x in repetionList where x.host == host && x.port == port select x).ToList();
                    if (a != null && a.Count > 0)
                        return a.First();
                    else
                        return null;
                }
            }

        }
        public proxyNettyServer getzoneServerById(string id)
        {
            if (master.id == id)
                return master;
            else
            {
                if (slave != null && slave.id == id)
                    return slave;
                else
                {
                    var a = (from x in repetionList where x.id == id select x).ToList();
                    if (a != null && a.Count > 0)
                        return a.First();
                    else
                        return null;
                }
            }

        }
        public void setSlave(proxyNettyServer _slave)
        {
            if (repetionList.Contains(_slave))
                removeRepetion(_slave);
            this.slave = _slave;
            if (_slave == localRunServer.Instance.ownServer)
                localRunServer.Instance.zoneRole = ServerRoleEnum.zoneSlave;

            this.OnChange(new serverChangeEventArgs(_slave.id, serverChangeTypeEnum.zoneSlaveChanged,"slave","",this.clusterId));
        }

        public void setMaster(proxyNettyServer _master)
        {
            if (repetionList.Contains(_master))
                removeRepetion(_master);
            if (slave == _master)
                this.slave = null;
            this.master = _master;
            if (_master == localRunServer.Instance.ownServer)
                localRunServer.Instance.zoneRole = ServerRoleEnum.zoneMaster;
            this.OnChange(new serverChangeEventArgs(_master.id, serverChangeTypeEnum.zoneMasterChanged,"master set","",this.clusterId));
        }
        public void addRepetion( proxyNettyServer _server)
        {
            if (!repetionList.Contains(_server))
            {
                if (_server == localRunServer.Instance.ownServer)
                    localRunServer.Instance.zoneRole = ServerRoleEnum.zoneRepetiton;
                this.repetionList.Add(_server);
               
                this.OnChange(new serverChangeEventArgs(_server.id, serverChangeTypeEnum.zoneRepChanged,"repetionList add","",this.clusterId));
            }
        }
        public void removeRepetion(proxyNettyServer _server)
        {
            if (repetionList.Contains(_server))
            {
                this.repetionList.Remove(_server);
                if (_server == localRunServer.Instance.ownServer)
                    localRunServer.Instance.zoneRole = ServerRoleEnum.unkown;
                this.OnChange(new serverChangeEventArgs(_server.id, serverChangeTypeEnum.zoneRepRemoved,"repetionList delete","",this.clusterId));
            }
        }

        private static zoneServerCluster _instance = null;
        private static readonly object padlock = new object();
        [JsonProperty]
        public string zoneName { get; private set; }
        [JsonProperty]
        public string regionName { get; private set; }
        public static zoneServerCluster parlseJson(string jsonstr)
        {

            _instance = JsonConvert.DeserializeObject<zoneServerCluster>(jsonstr);
            return _instance;

        }
        /// <summary>
        /// 同步更新zone服务器集群
        /// </summary>
        /// <param name="offobj"></param>
        public void rsycUpdate(zoneServerCluster offobj)
        {
            if (this.clusterId != offobj.clusterId)
            {
                //todoXout.LogInf("");
                return;// 有异常问题，待处理
            }

            if (offobj.master != null && offobj.master.id != localRunServer.Instance.ownServer.id)
            {
                this.master = offobj.master;
            }
            if (offobj.master == null)
                this.master = null;
            if (offobj.slave != null && offobj.slave.id != localRunServer.Instance.ownServer.id)
            {
                this.slave = offobj.slave;
            }
            if (offobj.slave == null)
               this.slave=null;

            this.regionName = offobj.regionName;
            this.zoneName = offobj.zoneName;
            foreach (var one in offobj.repetionList)
            {
                if (one.id == localRunServer.Instance.ownServer.id)
                    continue;
                var old = (from x in repetionList where x.id == one.id select x).FirstOrDefault();
                if (old == null)
                    repetionList.Add(old);
                else
                    old = one;
            }

            }
        
        public JObject toJson()
        {
            JObject jobj = new JObject();
            JArray jarr = new JArray();
            if (this.master != null)
                jobj.Add("master", master.toJObject());
            if (this.slave != null)
                jobj.Add("slave", slave.toJObject());
            foreach (var obj in repetionList)
                jarr.Add(obj.toJObject());
            jobj.Add("repetionList", jarr);
            return jobj;

           
        }

       
        public void setClusterID(string cid)
        {
            this.clusterId = cid;
        }

        
        /// <summary>
        /// 获取集群中有效服务器数量
        /// </summary>
        /// <returns></returns>
        public int getZoneServerCount()
        {
            int count = 0;
            var one = this;
            if (one.master != null && one.master.status == serverStatusEnum.Ready)
                count++;
            if (one.slave != null && one.slave.status == serverStatusEnum.Ready)
                count++;
            foreach (var rep in one.repetionList)
            {
                if (rep.status == serverStatusEnum.Ready)
                    count++;
            }
            return count;
        }
    }
}
