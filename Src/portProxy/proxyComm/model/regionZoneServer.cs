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
    /// 域服务,代表一个region
    /// </summary>
    public class regionZoneServer
    {
        [JsonProperty]
        public string region { get; private set; }
        [JsonProperty]
        public string masterClusterId { get; private set; }
        [JsonProperty]
        public string masterId { get; private set; }

        [JsonProperty]
        public string slaveClusterId { get; private set; }
        [JsonProperty]
        public string slaveId { get; private set; }

        [JsonIgnore]
        public proxyNettyServer regionMaster { get {
                if (!string.IsNullOrEmpty(masterClusterId)&& zoneServer_dic.ContainsKey(masterClusterId))
                {
                    return zoneServer_dic[masterClusterId].getzoneServerById(masterId);
                }
                return null;
            }  }
          [JsonIgnore]
        public proxyNettyServer regionSlave { get {
                if (!string.IsNullOrEmpty( slaveClusterId) && zoneServer_dic.ContainsKey(slaveClusterId))
                {
                    return zoneServer_dic[slaveClusterId].getzoneServerById(slaveId);
                }
                return null;
            } }
        public int zoneServerCount { get { return zoneServer_dic.Count; } }
        [JsonProperty]
        private Dictionary<string, zoneServerCluster> zoneServer_dic { get; set; }

        public bool containZone(string zoneName)
        {
            return zoneServer_dic.ContainsKey(this.zoneCLusterId(zoneName));
        }
        public bool ContainsKey(string clusterId)
        {
            return zoneServer_dic.ContainsKey(clusterId);
        }
        public void addZoneServer(zoneServerCluster zsc)
        {
            
            if (!zoneServer_dic.ContainsKey(zsc.clusterId))
                zoneServer_dic.Add(zsc.clusterId, zsc);
        }
        public zoneServerCluster getzoneServerCluster(string clusterId)
        {
            if (zoneServer_dic.ContainsKey(clusterId))
                return zoneServer_dic[clusterId];
            return null;
        }
        public zoneServerCluster getzoneServerClusterByName(string zoneName)
        {
            return getzoneServerCluster(this.zoneCLusterId(zoneName));

        }
        
        public IList<zoneServerCluster> allZoneServerClusters()
        {
            
            return zoneServer_dic.Values.ToList();
        }
        public void removeZoneServerCluster(string clusterId)
        {
            if (zoneServer_dic.ContainsKey(clusterId))
                zoneServer_dic.Remove(clusterId);
        }
        public void removeZoneServerClusterByName(string zoneName)
        {
            removeZoneServerCluster(zoneCLusterId(zoneName));
        }
        public string zoneCLusterId(string zoneName)
        {
            return string.Format("{0}:{1}", region, zoneName).ToMD5();
        }
        public JObject toJson()
        {
            var jobj = JObject.FromObject(this);
            return jobj;
        }
        /// <summary>
        /// 获取该区域中服务器的总数
        /// </summary>
        /// <returns></returns>
        public long getServerCount()
        {
            int count = 0;
            if (this.regionMaster != null && this.regionMaster.status == serverStatusEnum.Ready)
                count++;
            foreach (var one in zoneServer_dic.Values)
            {

                count += one.getZoneServerCount();
            }
            return count;
        }
        public proxyNettyServer getServerById(string id)
        {
            proxyNettyServer result = null;
            foreach (var cluster in zoneServer_dic.Values)
            {
                result = cluster.getzoneServerById(id);
                if (result != null)
                    return result;
            }
            return null;
        }
        public zoneServerCluster getClusterByeServerId(string id)
        {
            proxyNettyServer result = null;
            foreach (var cluster in zoneServer_dic.Values)
            {
                result = cluster.getzoneServerById(id);
                if (result != null)
                    return cluster;
            }
            return null;
        }
        public void rsycUpdate(regionZoneServer offobj)
        {
            if (offobj.regionMaster.id != localRunServer.Instance.ownServer.id)
            {
               
                this.region = offobj.region;
            }
            foreach (var obj in offobj.zoneServer_dic)
            {
                if (zoneServer_dic.ContainsKey(obj.Key))
                {
                    zoneServer_dic[obj.Key].rsycUpdate(obj.Value);
                }
                else
                {
                    zoneServer_dic.Add(obj.Key, obj.Value);
                }
            }
        }
        public void setMaster(proxyNettyServer _master)
        {
            if (_master == null)
                return;
            if (!zoneServer_dic.ContainsKey(_master.clusterID))
            {
                throw new Exception("region not contain this zoneCluster");
            }
            var existServer = zoneServer_dic[_master.clusterID].getzoneServerById(_master.id);
            if (existServer == null)
            {
                 throw new Exception("cluster not contain this server");
            }
            this.masterClusterId = _master.clusterID;
            this.masterId = _master.id;
        }
        public void setSlave(proxyNettyServer _slave)
        {
            if (_slave == null)
                return;
            if (!zoneServer_dic.ContainsKey(_slave.clusterID))
            {
                throw new Exception("region not contain this zoneCluster");
            }
            var existServer = zoneServer_dic[_slave.clusterID].getzoneServerById(_slave.id);
            if (existServer == null)
            {
                throw new Exception("cluster not contain this server");
            }
            this.slaveClusterId = _slave.clusterID;
            this.slaveId = _slave.id;
        }

        public static regionZoneServer parlseJson(string str)
        {
            return JsonConvert.DeserializeObject<regionZoneServer>(str);
        }
        public regionZoneServer(string regionName)
        {
            this.region = regionName;
            zoneServer_dic = new Dictionary<string, zoneServerCluster>(StringComparer.CurrentCultureIgnoreCase);
        }
    }
}
