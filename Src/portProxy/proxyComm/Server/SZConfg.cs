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
    /**
     * 同步通信设计方案：
     * 1.一个zone为一个集群，一个集群中包含一个master和一个slave（参考zoneServerRoleEnum角色）；
     * 2.Zonemaster包含zone所有的注册服务信息，后端服务向master注册；slave监测master状态，master故障时，slave接管master
     * 3.Zonemaster 接收到服务器注册，会增加通知消息；后台定时向集群中的slave和副本进行通知进行更新。
     * 4.Zoemaster 定时与同一region的其他master同步消息，并获取region下的其他Zonemaster与slave信息。
     * 5.不同的region暂未实现
     */
    public class rsycUrlList
    {
        /// <summary>
        ///  slave和副本服务器向主服务器报告心跳url
        /// </summary>
        public static string clientHeartReportUrl = "";
        /// <summary>
        /// 向zone主服务器获取区域的集群信息（zone中的从服务器和副本向zone的主服务器，或者各zone主服务器向region主服务器获取）
        /// </summary>
        public static string getRegionClustUrl = "";
        /// <summary>
        /// 向zone主服务器获取本zone集群信息
        /// </summary>
        public static string GetZoneClustUrl = "";
        /// <summary>
        /// 主服务器向slave或者副本发有修改通知
        /// </summary>
        public static string zoneMasterNoticeUrl = "";
        /// <summary>
        /// 区域主服务器通知zone主服务器有zone集群的变更通知，各zone主服务器应该向区域主服务器获取新的Zone列表（包含该region的主服务器和从服务器信息）
        /// </summary>
        public static string regionMasterNoticeUrl = "";



    }
    public class RunConfig
    {
        /// <summary>
        /// 数据心跳定时器基础频度
        ///  </summary>
        public static int baseDataUpReportFreq = 100;

        private static RunConfig _instance = null;
        private static readonly object padlock = new object();
        private ConcurrentQueue<actionMessage> actions_queue;
        private IList<actionMessage> failActions;
        private static RunConfig getInstance()
        {
            lock (padlock)
            {
                if (RunConfig._instance == null)
                {
                    RunConfig._instance = new RunConfig();


                }
            }
            return RunConfig._instance;

        }
        public static RunConfig Instance { get { return getInstance(); } }

        public string clusterId { get; private set; }

        public void Alert(string info)
        {

            string alertstr = null;
            if (ownPNServer != null)
                alertstr = string.Format("from server:[{0}],port:[{1}],role:[{2}],alert message:[{3}]",
                    ownPNServer.host, ownPNServer.port, thisRole, info);
            else
                alertstr = string.Format("init error,from server:[{0}],port:[{1}],role:[{2}],alert message:[{3}]",
                "", "", thisRole, info);
            runlog.Fatal(alertstr);
            //do other
        }

        public const string portProxyFileName = "MapProxy.config";
        public proxyNettyServer ownPNServer { get; set; }
        public List<pCounter> monitorCounterlist;
        //   public Dictionary<ISession, baseServer> allServer_dic;
        public IList<string> localIP;
        public IList<pCounter> perfCounters;
        public System.Collections.Concurrent.ConcurrentQueue<serverMessage> messages;
        public string TaskConfigFile { get; private set; }



        public FrmLib.Log.myLogger runlog = commLoger.runLoger;
        public myLogger devlog = commLoger.devLoger;

 
        //  public Configuration siteconfig;
        static RunConfig()
        {
            _instance = null;
        }
        public zoneServerRoleEnum thisRole;
        //是否允许自动更新客户端，如果设为false，CompareClientFiles方法总返回空列表，表示无更新。
        public bool AutoUpdate = false;
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
        /// 端口转发服务器失效时间（秒）
        /// </summary>
        public int mapPortServerFailTime { get; private set; }
        /// <summary>
        /// region的主控制服务器url
        /// </summary>
        public string regionMasterUrl { get; internal set; }
        /// <summary>
        ///region主控，没有配置，默认为*
        /// </summary>
        public bool isRegionMaster { get; private set; }
        /// <summary>
        /// 同一个zone的主控
        /// </summary>
        public bool isZoneMaster { get; private set; }
        public string zoneMasterUrl { get; private set; }

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
        public void Refresh()
        {
            RunConfig._instance = null;

        }
        public bool isLocalIP(string ip)
        {
            return localIP.Contains(ip);
        }
        public void addActionMessage(actionMessage am)
        {
            this.actions_queue.Enqueue(am);
        }
        public actionMessage comsumActionMessage()
        {
            actionMessage am = null;
            if (this.actions_queue.TryDequeue(out am))
                return am;
            return null;
        }
        public void isFailActions(actionMessage am)
        {
            failActions.Add(am);
        }
        /// <summary>
        /// 向主集群服务器报告心跳（集群从服务器或副本）
        /// </summary>
        public void sayAliveToZoneMaster(Object state, System.Timers.ElapsedEventArgs e)
        {
       
        }
        /// <summary>
        /// 向主域服务器报告心跳（集群主服务器，非主域服务器）
        /// </summary>
        public void sayAliveToRegionMaster(Object state, System.Timers.ElapsedEventArgs e)
        {
            
        }

        /// <summary>
        /// 向主集群服务器注册（集群从服务器或副本）
        /// </summary>
        public proxyNettyServer registerForZoneMaster()
        {

            return null;
        }
        /// <summary>
        /// 向主域服务器注册（集群主服务器）
        /// </summary>
        public proxyNettyServer registerForRegionMaster()
        {
            return null;
            
        }
        /// <summary>
        //  从主集群服务器获取本集群信息
        /// </summary>
        /// <returns></returns>
        public string getClusterFromZoneMaster()
        {
            return null;
        }
        /// <summary>
        /// 从主域服务器获取全域信息
        /// </summary>
        /// <returns></returns>
        public string getClusterFromRegionMaster()
        {
            return null;
        }
        public IList<actionMessage> getFailActions()
        {
            return this.failActions;
        }
              private static  Timer keepAliveTimer;
        private RunConfig()
        {
            actions_queue = new ConcurrentQueue<actionMessage>();
            failActions = new List<actionMessage>();

            try
            {
                localIP = new List<string>();
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
                this.ownHost = commSetting.Configuration["urls:host"];
                this.lPort = commSetting.Configuration["urls:port"];
                this.isZoneMaster = bool.Parse(commSetting.Configuration["gateServer:isZoneMaster"]);
                this.zoneMasterUrl = commSetting.Configuration["gateServer:zoneMasterUrl"];
                this.regionMasterUrl = commSetting.Configuration["gateServer:regionMasterUrl"];
                this.maxPerfDataCount = int.Parse(commSetting.Configuration["gateServer:maxPerfDataCount"]);
                this.mapPortServerFailTime = int.Parse(commSetting.Configuration["gateServer:mapPortServerFailTime"]);
                this.region = string.IsNullOrEmpty(commSetting.Configuration["gateServer:region"]) ? "*" : commSetting.Configuration["gateServer:region"];
                this.zone = commSetting.Configuration["gateServer:zone"];
                keepAliveTimer = new System.Timers.Timer(baseDataUpReportFreq);
               

                if (string.IsNullOrEmpty(commSetting.Configuration["gateServer:isRegionMaster"]))
                {
                    if (this.region == "*")
                        this.isRegionMaster = true; //没有设置isRegionMaster，region为通用或没有配置，也是regionmaster
                    else
                        this.isRegionMaster = false;
                }
                else
                    this.isRegionMaster = bool.Parse(commSetting.Configuration["gateServer:isRegionMaster"]);

                this.httpProxyPort = (commSetting.Configuration["gateServer:httpProxyPort"]);

                messages = new System.Collections.Concurrent.ConcurrentQueue<serverMessage>();
                perfCounters = new List<pCounter>();
                region_dic = new Dictionary<string, regionZoneServer>(StringComparer.CurrentCultureIgnoreCase);
                if (isRegionMaster && isZoneMaster)
                {
                    //既是主域服务器也是主集群服务器
                    this.clusterId = Guid.NewGuid().ToString();
                    ownPNServer = new proxyNettyServer(this.clusterId, this.httpProxyPort);
                    regionZoneServer rz = new regionZoneServer(region);
                    rz.regionMaster = ownPNServer;
                    rz.zoneServer_dic.Add(zone, new zoneServerCluster(zone, ownPNServer));
                    //不需要心跳
                }
                else
                {
                    if (!isRegionMaster)
                    {
                        if (isZoneMaster)
                        {
                            this.ownPNServer=   registerForRegionMaster();                            
                            var str= getClusterFromRegionMaster();
                            //是主集群服务器，不是主域
                             keepAliveTimer.Elapsed += new System.Timers.ElapsedEventHandler(sayAliveToRegionMaster);
                        }
                        else
                        { 
                            //都不是
                            this.ownPNServer=  registerForZoneMaster();
                            var str = getClusterFromZoneMaster();
                            this.region_dic = JsonConvert.DeserializeObject<Dictionary<string, regionZoneServer>>(str);

                           
                             keepAliveTimer.Elapsed += new System.Timers.ElapsedEventHandler(sayAliveToZoneMaster);

                        }
                    }
                    else
                    {
                       //是主域，不是主集群
                         keepAliveTimer.Elapsed += new System.Timers.ElapsedEventHandler(sayAliveToZoneMaster);
                    }


                }
              
                keepAliveTimer.AutoReset = true;

            }
            catch (Exception e)
            {
                runlog.Fatal("init system error;" + System.Environment.NewLine + e.Message);
                throw new Exception("init system error");
            }


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
    }
}
