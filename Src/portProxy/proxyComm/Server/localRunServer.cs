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





    /**
* 同步方案：
* 0.  region代表域，zone代表集群，一个域包含多个集群；通常一个集群是若干有公网地址服务器和本地服务器组成的服务器集合；一个zone为一个集群，一个集群中包含一个master和一个slave（参考zoneServerRoleEnum角色），集群中的master、slave、rep（副本）必须有公网访问地址；
* 1.每一个（次）服务启动时，根据参数中的regionUrls查询所有域服务器的集群信息；主域服务器的判断，根据获取到的所有的域服务器信息中，得票最多的是域主控，然后获取到自身服务的集群，根据配置文件的zone信息；如果没有集群，则新增集群
* 2.当一个域中存在多个域主控，会逐渐消除并唯一，所有的域主控会向各个zone服务器同步域信息，当zone服务器收到域的同步信息时，发现不同的域主控时，会根据域服务器创建的时间先后，先创建的为主控，如果时间也相同，则根据两个主控同步信息中的zone数量判断，数量多的为主控；如果zone数量相同，则根据包含的所有服务器数量判断，有效数量多的服务器为主控；如果都相同，不做处理，等稍后再比较
* 3.域主从服务器和zone中主从都参与主域的主控服务器信息同步和主控的变更发现；
* 4.zone中的主控判断与与中主控的设置类似，创建时间、包含master、slave、rep角色的服务器数量、注册的转发组数量；如果都相同，不做处理，等稍后再比较。
* 5.zone中的主从、rep参与主服务器的选择；
* 
*/
    public partial class localRunServer
    {
        

        private static localRunServer _instance = null;
        private static readonly object padlock = new object();
        private HttpHelper hclient { get; set; }
        private rsycClient _rsycClient = new rsycClient();
        #region 属性
        [JsonIgnore]
        [XmlIgnore]
        public zoneServerCluster ownCluster { get {
                if (region_dic.ContainsKey(this.region))
                {
                    if (region_dic[this.region].zoneServer_dic.ContainsKey(this.zone))
                        return region_dic[this.region].zoneServer_dic[this.zone];
                }
                     return null;
            }
       
        }
        [JsonIgnore]
        [XmlIgnore]
        public regionZoneServer ownRegion
        {
            get
            {
                if (region_dic.ContainsKey(this.region))
                {
                    return region_dic[this.region];
                }
                return null;
            }

        }
        /// <summary>
        /// 发送给本服务器的需要响应操作的消息
        /// </summary>
        private IList<actionMessage> messagesNeedAction;
        public static localRunServer Instance { get { return getInstance(); } }
        /// <summary>
        /// 集群Id
        /// </summary>
        public string clusterId { get; private set; }

        /// <summary>
        /// 活动处理时间间隔
        /// </summary>
        public int actionProcessTime = 60;
        /// <summary>
        /// 数据心跳定时器时间间隔，单位秒
        ///  </summary>
        public  int sayAliveTime = 60;

        /// <summary>
        /// 移除失效服务器时间，单位秒
        ///  </summary>
        public int serverRemoveTimes = 1500;
        /// <summary>
        /// 域服务器广播时间间隔，单位秒
        ///  </summary>
        public int regionBroadcastTime = 600;

        public const string portProxyFileName = "MapProxy.config";
        public proxyNettyServer ownServer { get; set; }


        public IList<string> localIP;
        [Obsolete]
        public IList<pCounter> perfCounters;

        public string TaskConfigFile { get; private set; }



        public FrmLib.Log.myLogger runlog = commLoger.runLoger;
        public myLogger devlog = commLoger.devLoger;

 

        static localRunServer()
        {
            _instance = null;
        }

        //是否允许自动更新客户端，如果设为false，CompareClientFiles方法总返回空列表，表示无更新。
        public bool AutoUpdate = false;

        /// <summary>
        /// 访问协议
        /// </summary>
        public string protocol { get; private set; }

        /// <summary>
        /// 监听主机地址(http）
        /// </summary>
        internal string ownHost;
        /// <summary>
        /// 监听主机端口(http）
        /// 
        /// </summary>
        internal object lPort;
        /// <summary>
        /// 服务器失效时间，按超出上次心跳时间计算，单位秒
        /// </summary>
        public int serverFailTimes { get; private set; }
        /// <summary>
        /// region的控制服务器url列表
        /// </summary>
        public List<string> regionUrls { get; internal set; }

        /// <summary>
        /// 集群控制服务器列表
        /// </summary>
        public List<string> zoneUrls { get; private set; }


        public ServerRoleEnum zoneRole;
        public ServerRoleEnum regionRole;
        /// <summary>
        /// 性能收集数据数量
        /// </summary>
        /// 
        public int maxPerfDataCount { get; internal set; }
        /// <summary>
        /// 区域，一个区域分为多个zone，每个zone有多个代理服务器(proxyNettyServer)
        /// </summary>
        public string region { get; private set; }
        /// <summary>
        /// 同一个zone下的所有proxyNettyServer和代理的后置服务都是网络相通的，既可以相互替代；一个zone组成一个集群
        /// </summary>
        public string zone { get; private set; }
        /// <summary>
        /// http代理的端口
        /// </summary>
        public string httpProxyPort { get; private set; }

        public Dictionary<string, regionZoneServer> region_dic;
        /// <summary>
        /// 心跳报告
        /// </summary>
        private object aliveLockobj = new object();
        private bool sayaliveNowDo = false;
        private   Timer keepAliveTimer;
        /// <summary>
        /// 广播定时器
        /// </summary>
        private object  bcLockobj = new object();
        private bool bcNowDo = false;
        private Timer RegionBroadCastTimer;
        /// <summary>
        /// actionMessage 处理器
        /// </summary>
        private object actionLockobj = new object();
        private bool actionPNowDo = false;
        private Timer actionDoTimer;
        /// <summary>
        /// 检查在线（频度域心跳相同）
        /// </summary>
        private object checkAliveLockObj = new object();
        private bool checkAliveNowDo = false;
        private Timer checkAliveTimer;
        private static localRunServer getInstance()
        {
            lock (padlock)
            {
                if (localRunServer._instance == null)
                {
                    localRunServer._instance = new localRunServer();


                }
            }
            return localRunServer._instance;

        }
        public void Refresh()
        {
            localRunServer._instance = null;

        }
        public bool isLocalIP(string ip)
        {
            return localIP.Contains(ip);
        }
        private localRunServer()
        {


            try
            {
                localIP = new List<string>();
                messagesNeedAction = new List<actionMessage>();
                hclient = new HttpHelper(new TimeSpan(0, 0, 5)); //设置5秒的超时
                string AddressIP = string.Empty;
                foreach (IPAddress _IPAddress in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
                {
                    if (_IPAddress.AddressFamily.ToString() == "InterNetwork")
                    {
                        string ipstr = _IPAddress.ToString();
                        Console.WriteLine("ip:" + ipstr);
                        localIP.Add(ipstr);
                    }
                }
                this.zoneUrls = new List<string>();
                this.regionUrls = new List<string>();
                this.protocol = commSetting.Configuration["urls:protocol"];
                this.ownHost = commSetting.Configuration["urls:host"];
                this.lPort = commSetting.Configuration["urls:port"];
                this.regionRole = ServerRoleEnum.unkown;
                this.zoneRole = ServerRoleEnum.unkown;
                commSetting.Configuration.GetSection("").Bind(this.regionUrls);

                this.maxPerfDataCount = int.Parse(commSetting.Configuration["gateServer:maxPerfDataCount"]);
                this.serverFailTimes = int.Parse(commSetting.Configuration["gateServer:serverFailTimes"]);
                this.sayAliveTime = int.Parse(commSetting.Configuration["gateServer:sayAliveTime"]);
                this.serverRemoveTimes = int.Parse(commSetting.Configuration["gateServer:serverRemoveTimes"]);
                this.regionBroadcastTime = int.Parse(commSetting.Configuration["gateServer:regionBroadcastTime"]);
                this.region = commSetting.Configuration["gateServer:region"];
                if (string.IsNullOrEmpty(region))
                    this.region = "*";//没有配置region时的默认名称
                this.zone = commSetting.Configuration["gateServer:zone"];
                if (string.IsNullOrEmpty(zone))
                    throw new Exception("zone is not allow null");
                keepAliveTimer = new System.Timers.Timer(sayAliveTime);
                keepAliveTimer.Stop();
                RegionBroadCastTimer = new System.Timers.Timer(regionBroadcastTime);
                RegionBroadCastTimer.Stop();
                actionDoTimer = new Timer(actionProcessTime);
                actionDoTimer.Elapsed += new System.Timers.ElapsedEventHandler(actionMessageProcesser);
                actionDoTimer.AutoReset = true;
                checkAliveTimer = new Timer(sayAliveTime);
                checkAliveTimer.Stop();
                this.httpProxyPort = (commSetting.Configuration["gateServer:httpProxyPort"]);


                perfCounters = new List<pCounter>();
                region_dic = new Dictionary<string, regionZoneServer>(StringComparer.CurrentCultureIgnoreCase);


            }
            catch (Exception e)
            {
                runlog.Fatal("init system error;" + System.Environment.NewLine + e.Message);
                throw new Exception("init system error");
            }


        }
        #endregion
       
    }
}
