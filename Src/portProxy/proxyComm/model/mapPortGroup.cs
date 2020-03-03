using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text;
using Newtonsoft.Json.Converters;
using System.Net;
using Newtonsoft.Json;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

using DotNetty.Transport.Bootstrapping;
using System.Xml.Serialization;
using System.Linq;
using DotNetty.Transport.Channels;

namespace Proxy.Comm.model
{

    /// <summary>
    /// 端口转发组，同一业务服务归属一个组
    /// </summary>
    public class mapPortGroup:baseServer
    {

        
        [JsonIgnore]
        [XmlIgnore]
        public IChannel inputChannel;
        [JsonIgnore]
        [XmlIgnore]
        /// <summary>
        /// 归属的Server
        /// </summary>
        public proxyNettyServer ownServer { get; set; }
        /// <summary>
        /// 归属的服务器Id
        /// </summary>
        public string ownServerId { get { return ownServer.id; } }

        /// <summary>
        /// 归属集群Id 
        /// </summary>

        public string cid { get { return ownServer.clusterID; } }

        /// <summary>
        /// 转发映射类型，0：端口转发，1：http的appkey转发
        /// </summary>
        [JsonProperty]
        public int mapType { get { return _mapType; }
            private set {
                  this.Change(new serverChangeEventArgs(this.id, serverChangeTypeEnum.serverParamsChanged, "mapType", _mapType, value));
                _mapType = value;
            } }

        private int _mapType;
        /// <summary>
        /// 最大允许连入数量
        /// </summary>
        public int maxInCount { get { return _maxInCount; } set {
                this.Change(new serverChangeEventArgs(this.id, serverChangeTypeEnum.serverParamsChanged, "maxInCount", maxInCount, value));
                _maxInCount = value;
            } }
        private int _maxInCount;

        #region Data Members (4)
        /// <summary>
        /// 
        /// </summary>
        /// 
        [JsonProperty]
        public long inCount { get { return _inCount; } private set {
                this.Change(new serverChangeEventArgs(this.id, serverChangeTypeEnum.serverSettDataChanged, "inCount", inCount, value));
                _inCount = value;
            } }

        private long _inCount;
        /// <summary>
        /// 累计发送字节
        /// </summary>
        public long bytesSend { get; set; }
        public void addSendBytes(long count)
        {
            //   Interlocked.Add(ref inCount, 1);
            var oldvalue = bytesSend;
            lock (this)
            {
                bytesSend+=count;

            }
            this.Change(new serverChangeEventArgs(this.id, serverChangeTypeEnum.serverParamsChanged, "bytesSend", oldvalue, bytesSend));

        }
        /// <summary>
        /// 累计接收字节
        /// </summary>
        public long bytesRecv{ get; set; }
        public void addRecvBytes(long count)
        {
            //   Interlocked.Add(ref inCount, 1);
            var oldvalue = bytesRecv;
            lock (this)
            {
                bytesRecv += count;

            }
            this.Change(new serverChangeEventArgs(this.id, serverChangeTypeEnum.serverParamsChanged, "bytesRecv", oldvalue, bytesRecv));

        }

        /// <summary>
        /// 应用Key值
        /// </summary>
        public string appkey { get { return _appkey; } set {
                this.Change(new serverChangeEventArgs(this.id, serverChangeTypeEnum.serverParamsChanged, "appkey", appkey, value));
                _appkey = value;
            } }
        private string _appkey;
        /// <summary>
        /// 转发选择策略
        /// </summary>
        public outPortSelectPolicy groupPolicy { get { return _groupPolicy; } set {
                this.Change(new serverChangeEventArgs(this.id, serverChangeTypeEnum.serverParamsChanged, "groupPolicy", _groupPolicy, value));
                _groupPolicy = value;
            } }

        private outPortSelectPolicy _groupPolicy;

        /// <summary>
        /// 是否使用https
        /// </summary>
        public listenHttpsEnum useHttps { get { return _useHttps; } set {
                  this.Change(new serverChangeEventArgs(this.id, serverChangeTypeEnum.serverParamsChanged, "useHttps", _useHttps, value));
                _useHttps = value;
            } }
        private listenHttpsEnum _useHttps;
       
        public void setstatus(serverStatusEnum _status)
        {
            this.status = _status;
        }
        /// <summary>
        /// 更新基础属性是否需要重启
        /// </summary>
        /// <param name="jobj"></param>
        /// <returns></returns>
        public bool needRestartChanged(mapPortGroup obj)
        {
            if (this.port != obj.port
                || this.httpsPort != obj.httpsPort
                || this.host != obj.host
                 || this.useHttps != obj.useHttps
                 || this.appkey != obj.appkey)
                return true;
            return false;
        }
        


        [JsonIgnoreAttribute]
        [Obsolete]
        public EndPoint _point_in { get; set; }
        /// <summary>
        /// 可转发服务列表
        /// </summary>
        private IList<outMapPort> outPortList;
        /// <summary>
        /// 
        /// </summary>
        public bool isfromEureka = false;

        public void addCount()
        {
            //   Interlocked.Add(ref inCount, 1);
            var oldvalue = inCount;
            lock (this)
            {
                inCount++;

            }
          this.Change(new serverChangeEventArgs(this.id, serverChangeTypeEnum.serverParamsChanged, "inCount", oldvalue, inCount));

        }
        public IList<outMapPort> getAllServer()
        {
            return outPortList;
        }
        public void delCount()
        {
            var oldvalue = inCount;
            
            lock (this)
            {
                if (inCount > 0)
                    inCount--;

            }
          this.Change(new serverChangeEventArgs(this.id, serverChangeTypeEnum.serverParamsChanged, "inCount", oldvalue, inCount));
        }

        public outMapPort selectOutPortMaped()
        {
            switch (groupPolicy)
            {
                case (outPortSelectPolicy.loadAverage):
                    return getLoadAverageOutHostPort();
                    break;
                case (outPortSelectPolicy.fastResponse):
                    return getFastResponseOutHostPort();
                    break;
                default:
                    return getLoadAverageOutHostPort();
                    break;
            }

        }
        internal outMapPort getFastResponseOutHostPort()
        {
            double responseTime = int.MaxValue;
            outMapPort minopm = null;
            foreach (var obj in outPortList)
            {
                Console.WriteLine("status:{0},count:{1},ip:{2}", obj.status, obj.connectedCount, obj.host);
                if (obj.status == serverStatusEnum.Ready && (obj.maxConnected == -1 || obj.connectedCount < obj.maxConnected)
                    && obj.callResponeTime < responseTime)
                {
                    responseTime = obj.callResponeTime;
                    minopm = obj;
                }
            }

            return minopm;

        }
        internal outMapPort getLoadAverageOutHostPort()
        {
            int minCount = int.MaxValue;
            outMapPort minopm = null;
            foreach (var obj in outPortList)
            {
                localRunServer.Instance.devlog.InfoFormat("status:{0},count:{1},ip:{2}", obj.status, obj.connectedCount, obj.host);
                if (obj.status == serverStatusEnum.Ready && (obj.maxConnected == -1 || obj.connectedCount < obj.maxConnected) && obj.connectedCount < minCount)
                {
                    minCount = obj.connectedCount;
                    minopm = obj;
                }
            }

            return minopm;

        }
        #endregion Data Members
        public mapPortGroup(string host, string port, string appkey, int _listenCount, outPortSelectPolicy grouppolicy, proxyNettyServer oServer, bool fromEureka = false) : this(host, port, appkey, _listenCount, grouppolicy, null, oServer, listenHttpsEnum.onlyHttpsPort, 0, fromEureka)
        { }
        public mapPortGroup(string host, string port, string appkey, int _listenCount, outPortSelectPolicy grouppolicy, string httpsPort, proxyNettyServer oServer, listenHttpsEnum usehttps = listenHttpsEnum.onlyListenport, int _mapType = 0, bool fromEureka = false) : this()
        {
            this.ownServer = oServer;
            IPAddress ip = IPAddress.Any;
            if (!IPAddress.TryParse(host, out ip))
            { }
            ushort uport;
            if (ushort.TryParse(port, out uport))
            {
                _point_in = new IPEndPoint(ip, uport);
            }
            else
            {
                _point_in = null;
            }
            this.host = host;
            this.port = port;
            this.appkey = appkey;
            this.mapType = _mapType;
            this.groupPolicy = grouppolicy;
            this._maxInCount = _listenCount;
            this.httpsPort = httpsPort;
            this.useHttps = usehttps;
            this.isfromEureka = fromEureka;

        }
        [JsonConstructor]
        public mapPortGroup():base()
        {
            outPortList = new List<outMapPort>();
            this.bytesRecv = 0;
            this.bytesSend = 0;
          
            
        }


        public string toStateString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(string.Format("    key:{0},host:{1},port:{2},status:{3}", this.appkey, this.host, this.port, this.status.ToString()));
            foreach (var obj in outPortList)
            {
                sb.AppendLine(string.Format("             map host:{0},port:{1},count:{2},status:{3},needCheck:{5},lastlive:{4}"
                    , obj.host, obj.port, obj.connectedCount, obj.status.ToString(), obj.lastLive, obj.needCheckLive));
            }
            return sb.ToString();
        }
       
        
        public static mapPortGroup ParseJson(string json)
        {
            //todo
            mapPortGroup mpg = JsonConvert.DeserializeObject<mapPortGroup>(json);
            return mpg;
        }

        public outMapPort haveOPM(string host, string listenPort, string httpsPort)
        {

            foreach (var obj in outPortList)
            {
                if (obj.host.Equals(host, StringComparison.CurrentCultureIgnoreCase)
                    && listenPort.Equals(obj.port, StringComparison.CurrentCultureIgnoreCase)
                    && httpsPort.Equals(obj.httpsPort, StringComparison.CurrentCultureIgnoreCase))
                {

                    return obj;
                }
            }
            return null;

        }
        public outMapPort getOPMbyID(string id)
        {
            foreach (var obj in outPortList)
            {
                if (obj.id == id)
                    return obj;
            }
            return null;
        }
        public void removeOPMById(string id)
        {
            foreach (var obj in outPortList)
            {
                if (obj.id == id)
                {
                    this.outPortList.Remove(obj);
                    return;
                }
            }
        }
        public outMapPort addOutPortForJsonParse(JObject jobj)
        {
            outMapPort opm = outMapPort.parlseJson(jobj);
            lock (outPortList)
                outPortList.Add(opm);
            return opm;
        }
        public outMapPort addOutPort(string host, string port, string httpsPort, int maxCount, bool needCheckLive)
        {
            var oldop = (from x in outPortList where x.port == port && x.host == host select x).FirstOrDefault();
            if (oldop != null)
                return oldop;
            outMapPort opm = new outMapPort(cid, host, port, httpsPort, needCheckLive, this, -1, maxCount, this.isfromEureka);
            lock (outPortList)
            {

                outPortList.Add(opm);
            }
            setstatus(serverStatusEnum.Ready);
            return opm;
        }
    }
 
    /// <summary>
    /// 一个端口转发的服务器（代表一个业务服务器）
    /// </summary>
    public class outMapPort : baseServer
    {

        /// <summary>
        /// 服务端监测是否在的url
        /// </summary>
        public string sayEchoUrl;
        /// <summary>
        /// 
        /// </summary>

        public InstanceInfo _ins { get; set; }
       

        public static outMapPort parlseJson(JObject jobj)
        {
            outMapPort omp = JsonConvert.DeserializeObject<outMapPort>(jobj.ToString());
            return omp;
        }
        public JObject toJson()
        {
            JObject jobj = JObject.FromObject(this);

            return jobj;
        }
        /// <summary>
        /// 最大连接数
        /// </summary>
        public long maxConnected
        {
            get { return _maxConnected; }
            private set
            {
                this.Change(new serverChangeEventArgs(this.id, serverChangeTypeEnum.serverParamsChanged, "maxConnected", _maxConnected, value));
                _maxConnected = value;
            }
        }
        private long _maxConnected;

        /// <summary>
        /// 对端服务累计处理时间（微秒）
        /// </summary>
        public long _msec_ServerProcess { get; private set; }
        public void add_msec_ServerProcess(long count)
        {
            var oldvalue = _msec_ServerProcess;
            lock (this)
            {
                _msec_ServerProcess = _msec_ServerProcess + count;
            }
            this.Change(new serverChangeEventArgs(this.id, serverChangeTypeEnum.serverSettDataChanged, "_msec_ServerProcess", oldvalue, _msec_ServerProcess));
        }
        /// <summary>
        /// 对端服务请求次数
        /// </summary>
        public long _serverProcessCount { get; private set; }
        public void ins_serverProcessCount()
        {
            var oldvalue = _serverProcessCount;
            lock (this)
            {
                _serverProcessCount++;
            }
            this.lastLive = DateTime.Now;
            this.Change(new serverChangeEventArgs(this.id, serverChangeTypeEnum.serverSettDataChanged, "_serverProcessCount", oldvalue, _serverProcessCount));
        }
        /// <summary>
        /// 累计发送字节
        /// </summary>
        public long _bytes_send { get; private set; }
        public void addSendBytes(long sendcount)
        {
            var oldvalue = _bytes_send;
            lock (this)
            {
                _bytes_send = _bytes_send + sendcount;
            }
          this.Change(new serverChangeEventArgs(this.id, serverChangeTypeEnum.serverSettDataChanged, "_bytes_send", oldvalue, _bytes_send));
        }
        /// <summary>
        /// 累计接收字节
        /// </summary>
        public long _bytes_recv { get; private set; }
        public void addRecvBytes(long recvcount)
        {
            var oldvalue = _bytes_recv;
            lock (this)
            {
                _bytes_recv = _bytes_recv + recvcount;
            }
         this.Change(new serverChangeEventArgs(this.id, serverChangeTypeEnum.serverSettDataChanged, "_bytes_recv", oldvalue, _bytes_recv));
        }
        /// <summary>
        /// 归属组
        /// </summary>
        [JsonIgnoreAttribute]
        [XmlIgnore]
        public mapPortGroup ownGroup { get; private set; }

        /// <summary>
        /// 输出总数占比；outPortSelectPolicy.planPercent模式下使用
        /// </summary>
        public int planPercentCount { get { return _planPercentCount; }
            set {
                this.Change(new serverChangeEventArgs(this.id, serverChangeTypeEnum.serverParamsChanged, "planPercentCount", _planPercentCount, value));
                _planPercentCount = value;
            }
        }

        private int _planPercentCount;

        /// <summary>
        /// 来自eureka的标志
        /// </summary>
        public bool isfromEureka = false;


        [JsonProperty]
        public bool needCheckLive { get { return _needCheckLive; }
            private set {
                this.Change(new serverChangeEventArgs(this.id, serverChangeTypeEnum.serverParamsChanged, "needCheckLive", _needCheckLive, value));
                _needCheckLive = value;
            } }
        private bool _needCheckLive;
        public outMapPort(string cid, string host, string port, string httpsport, bool needcheck,
            mapPortGroup opm, int _maxPerfData = 100, int _maxConnected = -1, bool _isfromeukera = false, int _planPercentCount = 1) : base()
        {
            this.clusterID = cid;
            this.host = host;
            this.port = port;
            this.httpsPort = httpsport;
            this.maxConnected = _maxConnected;
            this.ownGroup = opm;
            this.needCheckLive = needcheck;
            this.isfromEureka = _isfromeukera;
            this.planPercentCount = _planPercentCount;
            connectedCount = 0;            
           
            setStatus(serverStatusEnum.Ready);
        
        }
        [JsonConstructor]
        public outMapPort():this(null,null,null,null,false,null)
        {

        }
    }
    public enum outPortSelectPolicy
    {
        loadAverage = 0, //平均负载分配
        fastResponse = 1,  //响应速度
        planPercent = 2,//按设定的配比模式，根据每个输出的planPercentCount占总count的数量比分配输出
    }

   public enum listenHttpsEnum
    {
        onlyListenport = 0,
        onlyHttpsPort = 1,
        both = 2
    }

}
