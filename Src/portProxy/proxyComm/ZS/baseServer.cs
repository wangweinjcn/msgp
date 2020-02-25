using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace Proxy.Comm
{
    public enum enum_Actiondo
    {
        /// <summary>
        /// ZS服务器向主服务器通知本服务器有配置信息更改
        /// </summary>
        reportToMasterConfigData = 0,
        /// <summary>
        /// 主服务器向所有集群内的服务器广播有服务器配置信息已变化
        /// </summary>
        noticeToAllZServrConfigData = 1,
        /// <summary>
        /// ZS服务器向本服务器内所有端口转发服务器广播有服务器配置信息变化
        /// </summary>
        noticeToAllOMPConfigData = 2,
        /// <summary>
        /// 重新设置主服务器
        /// </summary>
        resetMasterServer = 3,
        /// <summary>
        /// 重新设置从服务器
        /// </summary>
        resetSlaveServer = 4,
        /// <summary>
        /// 缓存对象发生变化
        /// </summary>
        cachObjectChange = 5
    }
    public class actionMessage
    {
        public string clusterID { get; private set; }
        public enum_Actiondo action { get; private set; }
        /// <summary>
        /// 消息发送目的地所在，ClusterID|appHostIP:appPort:appHttpsPort
        /// </summary>
        public string destPoint { get; private set; }
        /// <summary>
        /// 消息本事ID；结构：YYYYMMDDHHMISS|guid
        /// </summary>
        public string ID { get; private set; }
        /// <summary>
        /// 消息来源所在，ClusterID|appHostIP:appPort:appHttpsPort
        /// </summary>
        public string fromPoint { get; private set; }
        /// <summary>
        /// 
        /// </summary>
        public string message { get; private set; }
        /// <summary>
        /// 失败次数
        /// </summary>
        public int failcount;
        private string parseClusterID(string fromapp)
        {
            int pos = fromapp.IndexOf('|');
            if (pos < 0)
                throw new Exception("fromapp格式错误");
            return fromapp.Substring(0, pos);
        }
        public string toJsonStr()
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(this);
        }
        public actionMessage(enum_Actiondo _action, string clusterid, string host, string port, string httpsport, string _message = "")
            : this(_action, clusterid + "|" + host + ":" + port + ":" + httpsport, _message)
        { }
        public actionMessage(enum_Actiondo _action, string _fromPoint,string _destPorint="", string _message = "")
        {
            this.ID = System.DateTime.Now.ToString("yyyyMMddHHmmss") + "-" + Guid.NewGuid().ToString();
            this.clusterID = parseClusterID(_fromPoint);
            this.fromPoint = _fromPoint;
            this.action = _action;
            this.message = _message;
            this.destPoint = _destPorint;
        }

        public static actionMessage parseJson(string jsonstr)
        {
            JObject jobj = JObject.Parse(jsonstr);
            return new actionMessage((enum_Actiondo)int.Parse(jobj["action"].ToString()), jobj["fromPoint"].ToString(),
                jobj["destPoint"].ToString(), jobj["message"].ToString());
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
    }
  
    public class serverChangeEventArgs : EventArgs
    {


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
    public abstract class baseServer
    {
        public delegate void changeEvnent(object sender, serverChangeEventArgs e);
        //在委托的机制下我们建立以个变更事件
        public event changeEvnent changeEventHandle;
        //声明一个可重写的OnChange的保护函数
        protected virtual void Change(serverChangeEventArgs e)
        {
            if (changeEventHandle != null)
            {

                this.changeEventHandle(this, e);
            }
        }
        /// <summary>
        /// 集群Id
        /// </summary>
        [JsonProperty]
        public string clusterID { get; private set; }
        /// <summary>
        /// 是否需要报告变更,待定
        /// </summary>
        [JsonProperty]
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
        /// <summary>
        /// 主机Id
        /// </summary>
        [JsonProperty]
        public string id { get; private set; }
        /// <summary>
        /// 
        /// </summary>
        [JsonIgnoreAttribute]
        [Obsolete]
        public List<pCounter> monitorCounter;
        /// <summary>
        /// 服务器地址
        /// </summary>
        public string host { get { return _host; }set {
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
        public string httpsPort { get { return _httpsPort; } set {
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
        public baseServer(string _clusterID, int _MapPortServerFailTime, int _PerformenceCountMax )

        {
            this.clusterID = _clusterID;
            this.mapPortServerFailTime = _MapPortServerFailTime;
            id = Guid.NewGuid().ToString();
            performence = new performenceData(_PerformenceCountMax);
            monitorCounter = new List<pCounter>();
            lastLive = System.DateTime.Now;
            this.host = "";
            port = "";
            httpsPort = "";
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
        /// <summary>
        /// 
        /// </summary>
        /// <param name="exceedMaxTimeOut"> 是否超时5倍时间</param>
        /// <returns>
        /// true:有状态修改
        /// false：没有状态修改
        /// </returns>
        public bool checkMeLive(out bool exceedMaxTimeOut)
        {
            baseServer obj = this;
            exceedMaxTimeOut = false;
            bool res = false;
            if (obj.status != serverStatusEnum.Ready && obj.status != serverStatusEnum.Fail)
                return res;

            TimeSpan ts = System.DateTime.Now.Subtract(obj.lastLive).Duration();
            if (obj.status == serverStatusEnum.Ready && ts.TotalSeconds > mapPortServerFailTime)
            {
             
                obj.setStatus(serverStatusEnum.Fail);
                res = true;
            }
            if (obj.status == serverStatusEnum.Fail && ts.TotalSeconds > 5 * mapPortServerFailTime)
            {
               
                exceedMaxTimeOut = true;
                res = true;
            }
            else
            {
                if (obj.status == serverStatusEnum.Fail && ts.TotalSeconds <= mapPortServerFailTime)
                {
                  
                    obj.setStatus(serverStatusEnum.Ready);
                    res = true;
                }

            }
            return res;
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

}
