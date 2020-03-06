using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Proxy.Comm.model
{
    public enum messageTypeEnum
    {
        unUsed = -1,
        /// <summary>
        /// 需要上报给集群主控
        /// </summary>
        needReportToZoneMaster = 0,
        /// <summary>
        /// 需要上报给域主控
        /// </summary>
        needReportToRegionMaster =1,
        /// <summary>
        /// 需要向集群主控同步集群信息
        /// </summary>
        zoneNeedRsycFromMaster=10,
        /// <summary>
        /// 需要向域主控同步域信息
        /// </summary>
        regionNeedRsycFromMaster=11

    }


    public class regionBroadCastMessage
    {
        public proxyNettyServer regionMaster;
        public List<string> receivedServerId;
        public DateTime sendDt;
        public string memo;
    }
    /// <summary>
    /// 队列消息类型
    /// </summary>
    public class queueMessage
    {
        private string _id;
        private string _receiptHandle;
        private string _bodyMD5;
        private string _body;

        private DateTime _enqueueTime;
        private DateTime _nextVisibleTime;
        private DateTime _firstDequeueTime;

        private uint _dequeueCount;

        private uint _priority;

        /// <summary>
        /// Empty constructor
        /// </summary>
        public queueMessage() { }

        /// <summary>
        /// Gets and sets the property Id. 
        /// </summary>
        public string Id
        {
            get { return this._id; }
            set { this._id = value; }
        }

        // Check to see if Id property is set
        internal bool IsSetId()
        {
            return this._id != null;
        }

        /// <summary>
        /// Gets and sets the property ReceiptHandle.
        /// </summary>
        public string ReceiptHandle
        {
            get { return this._receiptHandle; }
            set { this._receiptHandle = value; }
        }

        // Check to see if ReceiptHandle property is set
        internal bool IsSetReceiptHandle()
        {
            return this._receiptHandle != null;
        }

        /// <summary>
        /// Gets and sets the property Body. 
        /// </summary>
        public string Body
        {
            get { return this._body; }
            set { this._body = value; }
        }

        // Check to see if Body property is set
        internal bool IsSetBody()
        {
            return this._body != null;
        }

        /// <summary>
        /// Gets and sets the property BodyMD5. 
        /// </summary>
        public string BodyMD5
        {
            get { return this._bodyMD5; }
            set { this._bodyMD5 = value; }
        }

        // Check to see if BodyMD5 property is set
        internal bool IsSetBodyMD5()
        {
            return this._bodyMD5 != null;
        }

        /// <summary>
        /// Gets and sets the property EnqueueTime. 
        /// </summary>
        public DateTime EnqueueTime
        {
            get { return this._enqueueTime; }
            set { this._enqueueTime = value; }
        }

        /// <summary>
        /// Gets and sets the property NextVisibleTime. 
        /// </summary>
        public DateTime NextVisibleTime
        {
            get { return this._nextVisibleTime; }
            set { this._nextVisibleTime = value; }
        }

        /// <summary>
        /// Gets and sets the property FirstDequeueTime. 
        /// </summary>
        public DateTime FirstDequeueTime
        {
            get { return this._firstDequeueTime; }
            set { this._firstDequeueTime = value; }
        }

        /// <summary>
        /// Gets and sets the property DequeueCount. 
        /// </summary>
        public uint DequeueCount
        {
            get { return this._dequeueCount; }
            set { this._dequeueCount = value; }
        }

        /// <summary>
        /// Gets and sets the property Priority. 
        /// </summary>
        public uint Priority
        {
            get { return this._priority; }
            set { this._priority = value; }
        }
        public string appkey
        { get; set; }
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
        /// 域广播，消息主体为broadcastMessage
        /// </summary>
        broadcastFromRegionMaster = 9,

        /// <summary>
        /// 集群广播，消息主体为broadcastMessage
        /// </summary>
        broadcastFromZoneMaster = 10,
        /// <summary>
        /// 
        /// </summary>
        ServerChanged =100,
            /// <summary>
            /// 服务器新增
            /// </summary>
            serverAdd=101,
            /// <summary>
            /// 服务器移除
            /// </summary>
            serverRemove=102,
            /// <summary>
            /// 需要同步服务器信息变更，参数是{"url":"","region":"","zone":"","id":""}
            /// </summary>
            needRsycOneServer=103,
            /// <summary>
            /// 需要通知集群内其他服务器，通知内容{"Actiondo":, "url":"","region":"","zone":"","id":""}
            /// </summary>
            needNoticeServer=104,
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
    public class broadcastMessage
    {
        public string fromId;
        public DateTime sendDt;
        public int forwardCount;
        public DateTime serverCreateDt;
        public int proxyServerCount;
        public int outGroupCount;
        public int outServerCount;
        public ServerRoleEnum fromRole;
        public List<string> forwardServerId;
        public broadcastMessage()
        {
            sendDt = DateTime.Now;
            forwardCount = 1;
            forwardServerId = new List<string>();
        }
    }
    [Obsolete]
    public class serverMessage
    {
        /// <summary>
        /// 发送服务器的ID
        /// </summary>
        public string fromServerId;
        /// <summary>
        /// 发送服务器的IP+端口号
        /// </summary>
        public string fromServerName;

        /// <summary>
        ///停用， 目标服务器appkey，多个以";"隔开;*代表全部;空代表此项不筛选,优先级中，如果有，就不处理后一个的条件
        /// </summary>
        [Obsolete]
        public string destAppKey;
        /// <summary>
        ///停用， 目标服务器{IP:Port}，多个以";"隔开;*代表全部,空代表此项不筛选,优先级低，必须是前两个条件为空时，在使用此条件
        /// </summary>
        [Obsolete]
        public string destServerHostPort;
        /// <summary>
        /// 消息类型
        /// </summary>
        /// 
        public messageTypeEnum messageType;

        public string messageJsonBody;
        public DateTime createDt;
        public serverMessage(string fromId, string fromServerName, messageTypeEnum mType, string message)
        {
            this.fromServerId = fromId;
            this.fromServerName = fromServerName;
            messageJsonBody = message;
            messageType = mType;
            createDt = System.DateTime.Now;
        }
        public serverMessage(baseServer server, messageTypeEnum mType, string message) : this(server.id, server._serverName, mType, message)
        { }
        [JsonConstructor]
        public serverMessage() : this("", "", messageTypeEnum.unUsed, null)
        { }
    }
    #endregion
}
