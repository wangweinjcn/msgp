using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Proxy.Comm
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
        protected virtual void Change(serverChangeEventArgs e)
        {
            
            if (changeEventHandle != null)
            {

                this.changeEventHandle(this, e);
            }
        }
        public void baseServerChanged(object sender, serverChangeEventArgs e)
        {
            if (!this.needReportChange)
            {
                baseServer pserver = getOwnerServer();
                if (pserver != null)
                {
                    e.fromServerList.Add(_serverName);
                    pserver.Change(e);
                }
            }


        }
        protected virtual baseServer getOwnerServer()
        {
            throw new NotImplementedException();
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
                this.Change(new serverChangeEventArgs(this.id, serverChangeTypeEnum.serverParamsChanged, "_mapPortServerFailTime", _mapPortServerFailTime, value));
                _mapPortServerFailTime = value;

            } }

        private int _mapPortServerFailTime;
      
        /// <summary>
        /// 请求响应时间
        /// </summary>
        public double callResponeTime { get { return _callResponseTime; }
            set {
                this.Change(new serverChangeEventArgs(this.id, serverChangeTypeEnum.serverParamsChanged, "_callResponseTime", _callResponseTime, value));
                _callResponseTime = value;
            }
        }

        public double _callResponseTime;
        public string _serverName {
            get {
                return string.Format("{4} from {0}@{1}:{2} ", id, host, port,this.GetType().Name);
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
                this.Change(new serverChangeEventArgs(this.id, serverChangeTypeEnum.serverParamsChanged, "host", _host, value));
                _host = value;
            } }
        private string _host;
        /// <summary>
        /// 服务端口
        /// </summary>
        public virtual string port { get { return _port; } set {
             this.Change(new serverChangeEventArgs(this.id, serverChangeTypeEnum.serverParamsChanged, "port", _port, value));
                _port = value;
            } }
        private string _port;
        /// <summary>
        /// 可选的https服务端口
        /// </summary>
        public virtual string httpsPort { get { return _httpsPort; } set {
                 this.Change(new serverChangeEventArgs(this.id, serverChangeTypeEnum.serverParamsChanged, "_httpsPort", _httpsPort, value));
                _httpsPort = value;
            } }

        private string _httpsPort;
        /// <summary>
        /// 最后存活时间
        /// </summary>
       
        public DateTime lastLive { get { return _lastLive; } set {
                             this.Change(new serverChangeEventArgs(this.id, serverChangeTypeEnum.serverSettDataChanged, "_lastLive", _lastLive, value));
                _lastLive = value;
            } }

        private DateTime _lastLive;

        private serverStatusEnum _status;
        /// <summary>
        /// 服务器状态
        /// </summary>
        public serverStatusEnum status { get { return _status; } protected set {
                this.Change(new serverChangeEventArgs(this.id, serverChangeTypeEnum.serverStatusChanged, "status", _status, value));
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
            this.changeEventHandle += baseServerChanged;
            createDt = DateTime.Now;
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
                this.Change(e);
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
public class serverChangeEventArgs : EventArgs
    {

        public List<string> fromServerList;
        public readonly string changeServerId;
        public readonly serverChangeTypeEnum changeType;
        public readonly string key;
        public readonly object oldValue;
        public readonly object newValue;

        public serverChangeEventArgs()
        {

        }
        public serverChangeEventArgs(string _changeServerId, serverChangeTypeEnum _changeType, string _key,object _oldValue,object _newValue)
        {
            this.changeServerId = _changeServerId;
            this.changeType = _changeType;
            this.key = _key;
            this.oldValue = _oldValue;
            this.newValue = _newValue;
        }
    }
    public enum serverStatusEnum
    {
        Startup=0,//启动中
        Fail = -1,//故障
        Ready = 1,//运行中
        Disable = -2,//停用
        StopFormaintenance = 2//停机维护
    }
    public enum serverChangeTypeEnum
    {
        serverStatusChanged=0, //服务器状态有变化，优先级最高
        serverParamsChanged=1, //服务器服务参数变化，
        serverSettDataChanged=2, //服务器统计数据有变化，优先级最低

        zoneMasterChanged=10, //集群主控变动
        zoneSlaveChanged = 11,
        zoneRepChanged = 12,
        zoneRepRemoved=13,

        regionMasterChanged = 20, //域主控变动
        regionSlaveChanged = 21,
        regionRepChanged = 22,
        regionZoneRemoved=23,

        serverRoleChanged = 100,//服务器角色有变化，应用在集群服务器角色中
    }

    public enum ServerRoleEnum
    {
        unkown=-1, //未至
        zoneMaster = 0, //集群主控节点
        zoneSlave = 1,   //集群从控节点
        zoneRepetiton = 2,//集群副本控制器
            regionMaster=10, //域主控节点
            regionSlave=11,  //域从控节点
    }
    public class actionMessage
    {
        [JsonProperty]
        public string fromRegion { get; private set; }
        [JsonProperty]
        public string destRegion { get; private set; }
        [JsonProperty]
        public enum_Actiondo action { get; private set; }
        /// <summary>
        /// 消息发送目的地所在
        /// </summary>
        [JsonProperty]
        public string destServerId { get; private set; }
        /// <summary>
        /// 消息本身ID
        /// </summary>
        [JsonProperty]
        public string ID { get; private set; }
        /// <summary>
        /// 消息来源所在，serverid
        /// </summary>
        [JsonProperty]
        public string fromServerId { get; private set; }
        /// <summary>
        /// 参数
        /// </summary>
        [JsonProperty]
        public string messageParam { get; private set; }
        /// <summary>
        /// 失败次数
        /// </summary>
        [JsonProperty]
        public int failcount;
        [JsonProperty]
        public DateTime createDt;

        public string toJsonStr()
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(this);
        }

        [JsonConstructor]
        public actionMessage()
        {

        }
        public actionMessage(enum_Actiondo _action, string _fromId,string _fromRegion, string _destId = "",string _destRegion="", string _message = "")
        {
            this.ID = System.DateTime.Now.ToString("yyyyMMddHHmmss") + "-" + Guid.NewGuid().ToString();
           
            this.fromServerId = _fromId;
            this.fromRegion = _fromRegion;
            this.destRegion = _destRegion;
            this.action = _action;
            this.messageParam = _message;
            this.destServerId = _destId;
            createDt = DateTime.Now;
        }

        public static actionMessage parseJson(string jsonstr)
        {

            return JsonConvert.DeserializeObject<actionMessage>(jsonstr);
        }
    }
    public enum enum_Actiondo
    {
            unknown=0,
        /// <summary>
        /// 集群主服务器向集群内的服务器通知，向主服务器取同步信息；在参数中包含需要的主服务器地址,参数名：url;全部更新Region
        /// </summary>
        noticeToRsycZoneMaster = 1,
        /// <summary>
        /// 域主服务器向域内的服务器通知，向主服务器取同步信息；在参数中包含需要的主服务器地址,参数名:url；全部更新cluster
        /// </summary>
        noticeToRsycRegionMaster = 2,

        /// <summary>
        /// 集群内设置主服务器；
        /// </summary>
        resetZoneMasterServer = 3,
        /// <summary>
        /// 集群服务器变更
        /// </summary>
        resetZoneServers = 4,

        /// <summary>
        /// 域内设置主服务器；
        /// </summary>
        resetRegionMasterServer = 5,
        /// <summary>
        /// 域服务器变更
        /// </summary>
        resetRegionServers = 6,
        /// <summary>
        /// 域服务器集群变更
        /// </summary>
        resetRegionZones = 7,
        /// <summary>
        /// 广播消息设置域主服务器，参数中是主服务器地址
        /// </summary>
        broadcastFromRegionMaster = 9,
        /// <summary>
        /// 服务器信息变更，参数是{"url":"","region":"","zone":"","id":""}
        /// </summary>
            ServerChanged=100,
            /// <summary>
            /// 服务器新增
            /// </summary>
            serverAdd=101,
            /// <summary>
            /// 服务器移除
            /// </summary>
            serverRemove=102,
    }
    #region willobsolute

    [Obsolete]
    public class perfDataItem
    {
        public pCounter pc;
        public object value;
        public DateTime reportDt;
        public perfDataItem(pCounter pc, object value)
        {
            this.pc = pc;
            this.value = value;
            this.reportDt = System.DateTime.Now;
        }
    }
    [Obsolete]
    public class performenceData
    {
        Dictionary<pCounter, Queue<perfDataItem>> pCData;
        int max = 100;
        public performenceData(int _max = 100)
        {
            max = _max;
            pCData = new Dictionary<pCounter, Queue<perfDataItem>>();

        }
        public JArray getDataJson()
        {
            JArray jarr =new JArray();
            foreach (var obj in pCData)
            {
                JObject jobj = new JObject();
                jobj.Add("Counter", obj.Key.toJson());
                JArray jarr2 = JArray.Parse(JsonConvert.SerializeObject(obj.Value.ToArray()));
                jobj.Add("data", jarr2);
                jarr.Add(jobj);
                    }
            return jarr ;
        }
        public void addData(perfDataItem pdi)
        {
            Queue<perfDataItem> dataq = null;
            if (!pCData.ContainsKey(pdi.pc))
            {
                dataq = new Queue<perfDataItem>(max);
                pCData.Add(pdi.pc, dataq);
            }
            else
                dataq = pCData[pdi.pc];
            if (dataq.Count >= max)
                dataq.Dequeue();
            dataq.Enqueue(pdi);
        }
    }
    #endregion
}
