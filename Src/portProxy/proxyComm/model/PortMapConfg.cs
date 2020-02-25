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

namespace Proxy.Comm.model
{

    public enum listenHttpsEnum
    {
        onlyListenport = 0,
        onlyHttpsPort = 1,
        both = 2
    }
    /// <summary>
    /// 一个端口转发的服务器（代表一个业务服务器）
    /// </summary>
    public class outMapPort:baseServer
    {
        /// <summary>
        /// 
        /// </summary>

             public InstanceInfo _ins { get; set; }
        public void outMapPortChanged(object sender, serverChangeEventArgs e)
        {
            

        }
        private outMapPort(string cid):base(cid,RunConfig.Instance.mapPortServerFailTime,100)
        {
            this.needCheckLive = true;
        }
        
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
        /// 
        /// </summary>
        public long maxConnected { get; private set; }
        /// <summary>
        /// 
        /// </summary>
        public long minConnected { get; private set; }
        /// <summary>
        /// 对端服务累计处理时间（微秒）
        /// </summary>
        public long _msec_ServerProcess { get; private set; }
        public void add_msec_ServerProcess(long count)
        {
            lock (this)
            {
                _msec_ServerProcess = _msec_ServerProcess + count;
            }
        }
        /// <summary>
        /// 对端服务请求次数
        /// </summary>
        public long _serverProcessCount { get; private set; }
        public void ins_serverProcessCount()
        {
            lock (this)
            {
                _serverProcessCount ++;
            }
            this.lastLive = DateTime.Now;
        }
        /// <summary>
        /// 累计发送字节
        /// </summary>
        public long _bytes_send { get; private set; }
        /// <summary>
        /// 累计接收字节
        /// </summary>
        public long _bytes_recv { get; private set; }
        /// <summary>
        /// 归属组
        /// </summary>
        [JsonIgnoreAttribute]
        [XmlIgnore]
        public mapPortGroup ownGroup { get; private set; }
        //最后一次使用时间
        public DateTime lastUse;
        /// <summary>
        /// 输出总数占比；outPortSelectPolicy.planPercent模式下使用
        /// </summary>
        public int planPercentCount = 1;
        /// <summary>
        /// 来自eureka的标志
        /// </summary>
        public bool isfromEureka = false;
        public void addSendBytes(long sendcount)
        {
            lock (this)
            {
                _bytes_send = _bytes_send + sendcount;
            }
        }

        public void addRecvBytes(long recvcount)
        {
            lock (this)
            {
                _bytes_recv = _bytes_recv + recvcount;
            }
        }
        public bool needCheckLive { get; private set; }
        public outMapPort(string cid,string host, string port, string httpsport, bool needcheck,
            mapPortGroup opm,int _maxPerfData=100, int _maxConnected = -1, int _minconecnt = -1,bool _isfromeukera=false,int _planPercentCount=1):
            base(cid,RunConfig.Instance.mapPortServerFailTime, _maxPerfData)
        {
            this.host = host;
            this.port = port;
            this.httpsPort = httpsport;
            this.maxConnected = _maxConnected;
            this.minConnected = _minconecnt;
            this.ownGroup = opm;
            this.needCheckLive = needcheck;
            this.isfromEureka = _isfromeukera;
            this.planPercentCount = _planPercentCount;
            connectedCount = 0;
            lastUse = System.DateTime.Now;
            setStatus( serverStatusEnum.Ready);
            this.changeEventHandle += outMapPortChanged;
        }
 
    }
    public enum outPortSelectPolicy
    {
        loadAverage = 0, //平均负载分配
        fastResponse=1,  //响应速度
        planPercent=2,//按设定的配比模式，根据每个输出的planPercentCount占总count的数量比分配输出
    }
    
  
    /// <summary>
    /// 端口转发组，同一业务服务归属一个组
    /// </summary>
    public class mapPortGroup
    {

        [JsonIgnore]
        [XmlIgnore]
        /// <summary>
        /// 归属的Server
        /// </summary>
         public  proxyNettyServer ownServer { get; set; }
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
        public int mapType { get; private set; }
        #region Data Members (4)
        /// <summary>
        /// 
        /// </summary>
        public long inCount { get;  set; }
        /// <summary>
        /// 累计发送字节
        /// </summary>
        public long _bytes_send;
        /// <summary>
        /// 累计接收字节
        /// </summary>
        public long _bytes_recv;
        /// <summary>
        /// 
        /// </summary>
        public int listenCount { get;  set; }
        /// <summary>
        /// 
        /// </summary>
        public string appkey;
        /// <summary>
        /// 
        /// </summary>
        public outPortSelectPolicy groupPolicy { get;  set; }
        [Obsolete]
        public int _id { get; set; }
        /// <summary>
        /// 监听ip地址
        /// </summary>
        public string inHost { get;  set; }
        /// <summary>
        /// 监听端口
        /// </summary>
        public  string inport { get;  set; }

       
        /// <summary>
        /// 是否使用https
        /// </summary>
        public listenHttpsEnum useHttps { get;  set; }
        /// <summary>
        /// 服务组状态
        /// </summary>
        public serverStatusEnum status=serverStatusEnum.Startup;
        /// <summary>
        /// https使用端口
        /// </summary>
        public string httpsInPort { get;  set; }
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
            if (this.inport != obj.inport
                || this.httpsInPort != obj.httpsInPort
                || this.inHost != obj.inHost
                 || this.useHttps!=obj.useHttps
                 || this.appkey!=obj.appkey)
                return true;
            return false;
        }
        public void update(mapPortGroup obj)
        {

            this.inport = obj.inport                            ;
            this.httpsInPort = obj.httpsInPort                            ;
            this.inHost = obj.inHost                             ;
            this.groupPolicy = obj.groupPolicy                             ;
            this.useHttps = obj.useHttps;
            this.appkey = obj.appkey;
            var list = obj.outPortList;
            obj.outPortList = null;
            foreach (var one in list)
            {
                var old = (from x in this.outPortList where x.host == one.host && x.port == one.port select x).FirstOrDefault();
                if (old != null)
                {

                }
                else
                {

                }
            }
        }
        public void updateOutPortList(mapPortGroup obj)
        {
            
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
            lock (this)
            {
                inCount++;


            }
            
        }
        public IList<outMapPort> getAllServer()
        {
          return  outPortList;
        }
        public void delCount()
        {
            lock (this)
            {
                if(inCount>0)
                    inCount--;

            }
            
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
                RunConfig.Instance.devlog.InfoFormat("status:{0},count:{1},ip:{2}",obj.status,obj.connectedCount,obj.host);
                if (obj.status == serverStatusEnum.Ready && (obj.maxConnected == -1 ||obj.connectedCount<obj.maxConnected ) && obj.connectedCount < minCount)
                {
                    minCount = obj.connectedCount;
                    minopm = obj;
                }
            }

            return minopm;

        }
        #endregion Data Members
        public mapPortGroup(string host, string port, string appkey, int _listenCount, outPortSelectPolicy grouppolicy ,proxyNettyServer oServer , bool fromEureka = false):this(host,port,appkey,_listenCount,grouppolicy,null,oServer,listenHttpsEnum.onlyHttpsPort,0,fromEureka)
        { }
        public mapPortGroup(string host, string port, string appkey, int _listenCount, outPortSelectPolicy grouppolicy , string httpsPort,proxyNettyServer oServer, listenHttpsEnum usehttps = listenHttpsEnum.onlyListenport,int _mapType=0,bool fromEureka=false) : this()
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
            this.inHost = host;
            this.inport = port;
            this.appkey = appkey;
            this.mapType = _mapType;
            this.groupPolicy = grouppolicy;
            this.listenCount = _listenCount;
            this.httpsInPort = httpsPort;
            this.useHttps = usehttps;
            this.isfromEureka = fromEureka;
            
        }
      

        public string toStateString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(string.Format("    key:{0},host:{1},port:{2},status:{3}", this.appkey, this.inHost, this.inport,this.status.ToString()));
            foreach (var obj in outPortList)
            {
                sb.AppendLine(string.Format("             map host:{0},port:{1},count:{2},status:{3},needCheck:{5},lastlive:{4}"
                    , obj.host, obj.port, obj.connectedCount, obj.status.ToString(),obj.lastLive,obj.needCheckLive));
            }
            return sb.ToString();
        }
        public JObject toJson()
        {
            JObject resobj = new JObject();
            resobj.Add("_id", this._id);
            resobj.Add("appkey", this.appkey);
            resobj.Add("inCount", this.inCount);
            resobj.Add("listenCount", this.listenCount);
            resobj.Add("groupPolicy", (int)this.groupPolicy);
            resobj.Add("inHost", this.inHost);
            resobj.Add("inPort", this.inport);

            resobj.Add("useHttps", (int)this.useHttps);
            resobj.Add("status", (int)this.status);
            resobj.Add("httpsInPort", this.httpsInPort);

            JArray resarray = new JArray();
            foreach(var oneobj in outPortList)
            {
             
                resarray.Add(oneobj.toJson());
            }
            resobj.Add("outPortList", resarray);
            return resobj;

            //todo
        }
        public void checkPortServer()
        {
            List<outMapPort> deleteobjlist = new List<outMapPort>();
            foreach (var obj in outPortList)
            {
                if (!obj.needCheckLive)
                    continue;
                bool shoulremove = false;
                if (obj.checkMeLive(out shoulremove))
                {
                    if (shoulremove)
                    {
                        deleteobjlist.Add(obj);
              
                    }
                   
                }
            }
            lock (outPortList)
            {
                foreach (var obj in deleteobjlist)
                {
                    outPortList.Remove(obj);

                }
            }
            bool allfail = true;
            foreach (var obj in outPortList)
            {
                if (obj.status == serverStatusEnum.Ready)
                {
                    setstatus(serverStatusEnum.Ready);
                    allfail = false;
                    return;

                }
            }
            if (allfail)
                setstatus(serverStatusEnum.Disable);
        }
        public static mapPortGroup ParseJson(string json)
        {
            //todo
            mapPortGroup mpg = JsonConvert.DeserializeObject<mapPortGroup>(json);
            return mpg;
        }
        [JsonConstructor]
        public mapPortGroup()
        {
            outPortList = new List<outMapPort>();
            this._bytes_recv = 0;
            this._bytes_send = 0;
            this.listenCount = 1000;
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
            lock(outPortList)
            outPortList.Add(opm);
            return opm;
        }
        public outMapPort addOutPort(string host, string port, string httpsPort, int maxCount, int minCount,bool needCheckLive)
        {
            var oldop = (from x in outPortList where x.port == port && x.host == host select x).FirstOrDefault();
            if (oldop != null)
                return oldop;
            outMapPort opm = new outMapPort(cid, host, port, httpsPort, needCheckLive, this,-1, maxCount, minCount,this.isfromEureka);
            lock (outPortList)
            {
               
                outPortList.Add(opm);
            }
            setstatus(serverStatusEnum.Ready);
            return opm;
        }
    }

}
