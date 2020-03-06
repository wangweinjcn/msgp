using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Proxy.Comm.model
{
    /// <summary>
    /// 变更响应
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
       public delegate void serverChangeEvnent(object sender, serverChangeEventArgs e);
        public abstract class baseServer
    {
        /// <summary>
        /// 服务创建时间
        /// </summary>
        public DateTime createDt;
       
        //在委托的机制下我们建立以个变更事件，继承子类需要重写处理函数
        public virtual event serverChangeEvnent changeEventHandle;
        //声明一个可重写的OnChange的保护函数
        protected virtual void onChonage(serverChangeEventArgs e)
        {
            
            if (changeEventHandle != null)
            {

                this.changeEventHandle(this, e);
            }
        }
        public virtual void ServerChanged(object sender, serverChangeEventArgs e)
        {
            if (!this.needReportChange)
            {
                baseServer pserver = getOwnerServer();
                if (pserver != null)
                {
                    e.fromServerList.Add(_serverName);
                    pserver.onChonage(e);
                }
            }


        }
        protected virtual baseServer getOwnerServer()
        {
            return null;
        }
        
        /// <summary>
        /// 集群Id
        /// </summary>
        [JsonProperty]
        public string clusterID { get; protected set; }
        /// <summary>
        /// 是否需要报告变更,是否需要处理变更事件的消息，
        /// false时，调用getOwnerServer，继续上报变更；为true时需要处理变更事件并增加待处理消息
        /// 
        /// </summary>

        public bool needReportChange { get { return _needReportChange; } set {
            //不需要监控变更的事件，暂时注释
                //    this.Change(new serverChangeEventArgs(this.id, serverChangeTypeEnum.serverParamsChanged, "needReportChange", needReportChange, value));
                _needReportChange = value; } }


        public bool _needReportChange  { get; protected set; }
        /// <summary>
        /// 最大失效时间（秒）
        /// </summary>
        [JsonProperty]
        public int mapPortServerFailTime { get { return _mapPortServerFailTime; }
            protected set
            {
                this.onChonage(new serverChangeEventArgs(this.id, serverChangeTypeEnum.serverParamsChanged, "_mapPortServerFailTime", _mapPortServerFailTime, value));
                _mapPortServerFailTime = value;

            } }

        private int _mapPortServerFailTime;
      
        /// <summary>
        /// 请求响应时间
        /// </summary>
        public double callResponeTime { get { return _callResponseTime; }
            set {
                this.onChonage(new serverChangeEventArgs(this.id, serverChangeTypeEnum.serverParamsChanged, "_callResponseTime", _callResponseTime, value));
                _callResponseTime = value;
            }
        }

        public double _callResponseTime;
        public string _serverName {
            get {
                return string.Format("{3} from {0}@{1}:{2} ", id, host, port,this.GetType().Name);
            }
        }
        /// <summary>
        /// 主机Id
        /// </summary>
        [JsonProperty]
        public string id { get; protected set; }
        /// <summary>
        /// 
        /// </summary>
        [JsonIgnoreAttribute]
        [Obsolete]
        public List<pCounter> monitorCounter;
        /// <summary>
        /// 服务器地址
        /// </summary>
        public virtual string host { get { return _host; }set {
                this.onChonage(new serverChangeEventArgs(this.id, serverChangeTypeEnum.serverParamsChanged, "host", _host, value));
                _host = value;
            } }
        private string _host;
        /// <summary>
        /// 服务端口
        /// </summary>
        public virtual string port { get { return _port; } set {
             this.onChonage(new serverChangeEventArgs(this.id, serverChangeTypeEnum.serverParamsChanged, "port", _port, value));
                _port = value;
            } }
        private string _port;
        /// <summary>
        /// 可选的https服务端口
        /// </summary>
        public virtual string httpsPort { get { return _httpsPort; } set {
                 this.onChonage(new serverChangeEventArgs(this.id, serverChangeTypeEnum.serverParamsChanged, "_httpsPort", _httpsPort, value));
                _httpsPort = value;
            } }

        private string _httpsPort;
        /// <summary>
        /// 最后存活时间
        /// </summary>
       
        public DateTime lastLive { get { return _lastLive; } set {
                             this.onChonage(new serverChangeEventArgs(this.id, serverChangeTypeEnum.serverSettDataChanged, "_lastLive", _lastLive, value));
                _lastLive = value;
            } }

        private DateTime _lastLive;

        private serverStatusEnum _status;
        /// <summary>
        /// 服务器状态
        /// </summary>
        public serverStatusEnum status { get { return _status; } protected set {
                this.onChonage(new serverChangeEventArgs(this.id, serverChangeTypeEnum.serverStatusChanged, "status", _status, value));
                _status = value;
            } }

        [JsonIgnoreAttribute]
        public performenceData performence;

        public baseServer(string _clusterID="", int _MapPortServerFailTime=100, int _PerformenceCountMax =0)

        {
           
            this._needReportChange = false;
            this.clusterID = _clusterID;
            this.mapPortServerFailTime = _MapPortServerFailTime;
            id = Guid.NewGuid().ToString();
            if (_PerformenceCountMax > 0)
            {
                performence = new performenceData(_PerformenceCountMax);
                monitorCounter = new List<pCounter>();
            }
            lastLive = System.DateTime.Now;
            this.host = "";
            port = "";
            httpsPort = "";
            this.status = serverStatusEnum.Ready;
            createDt = DateTime.Now;
             this.changeEventHandle += ServerChanged;
        }

       
        public string getServerMessageFrom()
        {
            return clusterID + "|" + this.host + ":" + this.port + ":" + this.httpsPort;
        }
        public void setStatus(serverStatusEnum _status)
        {
            lock (this)
            {
                this.status = _status;
                _needReportChange = true;
            }
           
                
           
        }

        private int _connectedCount;
        public int connectedCount
        {
            get
            { return _connectedCount; }
            set
            {
                serverChangeEventArgs e = new serverChangeEventArgs(this.id, serverChangeTypeEnum.serverSettDataChanged,"connectedCount",_connectedCount,value);
                _connectedCount = value;
                this.onChonage(e);
            }
        }
        public JObject toJson()
        {
            JObject jobj = JObject.FromObject(this);
            return jobj;

        }
        public void addCount()
        {
            lock (this)
            {
                connectedCount++;

            }

        }
        public void delCount()
        {
            lock (this)
            {
                if (connectedCount > 0)
                    connectedCount--;

            }

        }
        public virtual async Task Start()
        {
            throw new NotImplementedException();
        }
        public virtual async Task Stop()
        {
             throw new NotImplementedException();
        }

    }    

}
