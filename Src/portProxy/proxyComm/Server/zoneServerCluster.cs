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

namespace Proxy.Comm
{
   
    /// <summary>
    /// 服务管理实例，一个用于转发和注册的服务器描述
    /// </summary>
    public class proxyNettyServer:baseServer
    {
        [XmlIgnore]
          [JsonIgnore]
        int maxInitialLineLength = 4096, maxHeaderSize = 8192, maxChunkSize = 8192, maxContentLength = 1024 * 1024 * 40;
        [XmlIgnore]
        [JsonIgnore]
        MultithreadEventLoopGroup bossGroup,httpbossGroup;
        [XmlIgnore]
        [JsonIgnore]
        MultithreadEventLoopGroup workerGroup,httpworkerGroup;
        [XmlIgnore]
        [JsonIgnore]
        ProxyServerHandler SERVER_HANDLER=new ProxyServerHandler();
        [XmlIgnore]
        [JsonIgnore]
        ServerBootstrap portbootstrap;
        [XmlIgnore]
        [JsonIgnore]
        ServerBootstrap httpbootstrap;
        [XmlIgnore]
        [JsonIgnore]
        IChannel bootstrapChannel;
        [XmlIgnore]
        [JsonIgnore]
        IChannel httpbootstrapChannel;
        public async Task<ServerBootstrap> startOnePortAsync(string lport)
        {
            //X509Certificate2 tlsCertificate = null;
            //if (ServerSettings.IsSsl)
            //{
            //    tlsCertificate = new X509Certificate2(Path.Combine(commSetting.ProcessDirectory, "dotnetty.com.pfx"), "password");
            //}
            try
            {
               
                portbootstrap
                    .Group(bossGroup, workerGroup)
                    .Channel<CustTcpServerSocketChannel>()
                    .Option(ChannelOption.SoBacklog, 100)
                    .Handler(new LoggingHandler(LogLevel.WARN))
                    .ChildHandler(new ActionChannelInitializer<IChannel>(channel =>
                    {
                        IChannelPipeline pipeline = channel.Pipeline;
                        //if (tlsCertificate != null)
                        //{
                        //    pipeline.AddLast(TlsHandler.Server(tlsCertificate));
                        //}

                        // pipeline.AddLast(new LengthFieldBasedFrameDecoder(commSetting.MAX_FRAME_LENGTH, commSetting.LENGTH_FIELD_OFFSET, commSetting.LENGTH_FIELD_LENGTH, commSetting.LENGTH_ADJUSTMENT, commSetting.INITIAL_BYTES_TO_STRIP, false));
                        pipeline.AddLast(SERVER_HANDLER);
                    }));

                bootstrapChannel = await portbootstrap.BindAsync(int.Parse(lport));
                Console.WriteLine($"Socket started. Listening on {lport}");
                return portbootstrap;




            }
            catch (Exception ex)
            {

                Task.WaitAll(bossGroup.ShutdownGracefullyAsync(), workerGroup.ShutdownGracefullyAsync());
                throw new Exception("服务启动失败");
            }
        }
        public async Task<ServerBootstrap> startHttpPortAsync(string lport)
        {
            //X509Certificate2 tlsCertificate = null;
            //if (ServerSettings.IsSsl)
            //{
            //    tlsCertificate = new X509Certificate2(Path.Combine(commSetting.ProcessDirectory, "dotnetty.com.pfx"), "password");
            //}
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency;
            }
            try
            {
                
                httpbootstrap.Group(httpbossGroup, httpworkerGroup);
                httpbootstrap.Channel<CustHttpServerSocketChannel>();              

                httpbootstrap
                    .Option(ChannelOption.SoBacklog, 8192)
                    
                    .ChildHandler(new ActionChannelInitializer<IChannel>(channel =>
                    {
                        IChannelPipeline pipeline = channel.Pipeline;

                      //  pipeline.AddLast("encoder", new HttpResponseEncoder());
                        pipeline.AddLast("decoder", new HttpRequestDecoder(maxInitialLineLength, maxHeaderSize, maxChunkSize, true));
                        pipeline.AddLast("decoder2", new HttpObjectAggregator(maxContentLength)); 
                        pipeline.AddLast("handler", new HttpProxyServerHandler());
                    }))
                     .ChildOption(ChannelOption.SoKeepalive, true);

                 httpbootstrapChannel = await httpbootstrap.BindAsync(IPAddress.IPv6Any,int.Parse( lport));
                Console.WriteLine($"Httpd started. Listening on {bootstrapChannel.LocalAddress}");

                return httpbootstrap;




            }
            catch (Exception ex)
            {

                Task.WaitAll(httpbossGroup.ShutdownGracefullyAsync(), httpworkerGroup.ShutdownGracefullyAsync());
                throw new Exception("服务启动失败");
            }
        }

        public override async Task Start()
        {


            foreach (var obj in mapPortGroup_dic)
            {
                var bs = startOnePortAsync(obj.Key).GetAwaiter().GetResult();
               
            }
            startHttpPortAsync(this.port);
        }
        public override async Task Stop()
        {
           await bootstrapChannel.CloseAsync();
           Task.WaitAll(bossGroup.ShutdownGracefullyAsync(), workerGroup.ShutdownGracefullyAsync());

            await httpbootstrapChannel.CloseAsync();
            Task.WaitAll(httpbossGroup.ShutdownGracefullyAsync(), httpworkerGroup.ShutdownGracefullyAsync());
        }
        public bool needReportChange { get { return _needReportChange; } set { _needReportChange = value; } }
        public bool PortGroupContainKey(string key)
        {
            return mapPortGroup_dic.ContainsKey(key);
        }
        public mapPortGroup getPortGroupByKey(string key)
        {
            if (mapPortGroup_dic.ContainsKey(key))
                return mapPortGroup_dic[key];
            else
                return null;
        }
        public bool httpGroupContainKey(string key)
        {
            return maphttpGroup_dic.ContainsKey(key);
        }
        public mapPortGroup getHttpGroupByKey(string key)
        {
            if (maphttpGroup_dic.ContainsKey(key))
                return maphttpGroup_dic[key];
            else
                return null;
        }
        /// <summary>
        /// 是否是服务器注册中心，提供服务注册与服务查询
        /// </summary>
        [JsonProperty]
        public bool isServerRegister { get; private set; }
        /// <summary>
        /// 是否是网关中心，提供端口转发与路由功能
        /// </summary>
        [JsonProperty]
        public bool isServerGater { get; private set; }
        /// <summary>
        /// 该server下的端口转发列表
        /// key是监听的端口号
        /// </summary>
        [JsonProperty]
        private Dictionary<string, mapPortGroup> mapPortGroup_dic { get; set; }
        /// <summary>
        /// 该server下的http转发列表，
        /// key是appKey
        /// </summary>
[JsonProperty]
        private Dictionary<string, mapPortGroup> maphttpGroup_dic { get; set; }

        public zoneServerRoleEnum servertype { get; private set; }
        [JsonIgnoreAttribute]
        public List<serverMessage> unConsumeMessage;
        public void setType(zoneServerRoleEnum _serverType)
        {
            lock (this)
                this.servertype = _serverType;
        }       
        
        public void addMessage(serverMessage _message)
        {
            lock (this)
                this.unConsumeMessage.Add(_message);

        }
        public void removeMessage(serverMessage _message)
        {
            lock (this)
                this.unConsumeMessage.Remove(_message);

        }
       
      
        public proxyNettyServer(string cid, string httpport, string httpsport=null, int _maxPerfData=100) : this(cid,100,_maxPerfData)
        {          
            this.port = httpport;
            this.httpsPort = httpsport;
        }
        public proxyNettyServer(string cid,int failtime, int _maxPerfData):base(cid,failtime,_maxPerfData)
        {
            mapPortGroup_dic = new Dictionary<string, mapPortGroup>(StringComparer.OrdinalIgnoreCase);
            maphttpGroup_dic = new Dictionary<string, mapPortGroup>(StringComparer.OrdinalIgnoreCase);
            unConsumeMessage = new List<serverMessage>();
            this.servertype = zoneServerRoleEnum.repetiton;
            bossGroup = new MultithreadEventLoopGroup(1);
            workerGroup = new MultithreadEventLoopGroup();
            SERVER_HANDLER = new ProxyServerHandler();
            portbootstrap = new ServerBootstrap();
            httpbossGroup = new MultithreadEventLoopGroup(1);
            httpworkerGroup = new MultithreadEventLoopGroup();
            httpbootstrap = new ServerBootstrap();
        }
        public proxyNettyServer(string cid):this(cid,100,100)
        {

        }
        public JObject toJson()
        {            
            //JObject jobj = base.toJson();
            //jobj.Add("servertype", ((int)this.servertype));
            //JArray jarr = new JArray();
            //foreach (var obj in this.mapPortGroup_dic.Values)
            //{
            //    jarr.Add(obj.toJson());
            //}
            //jobj.Remove("mapPortGroup_dic");
            //jobj.Add("mapPortGroup_dic", jarr);

            return JObject.FromObject(this);
        }
       
               
        /// <summary>
        /// 
        /// </summary>
        public void loadPortProxyCfg()
        {
            string filename = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, RunConfig.portProxyFileName);
            if (!File.Exists(filename))
                return;
            XmlDocument tmpxmldoc = Static_xmltools.GetXmlDocument(filename);
            if (tmpxmldoc == null)
                return;
            XmlNodeList groupList = null;

            string xpathstr = "//application/group";

            groupList = Static_xmltools.SelectXmlNodes(tmpxmldoc, xpathstr);
            foreach (XmlNode obj in groupList)
            {
                //todo
                string appkey = Static_xmltools.GetXmlAttr(obj, "Appkey");
                string lisnport = Static_xmltools.GetXmlAttr(obj, "listenPort");
                string str = Static_xmltools.GetXmlAttr(obj, "Policy");                
                int policy = 0;
                int.TryParse(str, out policy);

                int mapType=0;
                 str = Static_xmltools.GetXmlAttr(obj, "mapType");
                int.TryParse(str, out mapType);


                string _httpsPort = Static_xmltools.GetXmlAttr(obj, "listenHttpsPort");

                outPortSelectPolicy selectpolicy = (outPortSelectPolicy)policy;
                str = Static_xmltools.GetXmlAttr(obj, "MaxWaitQueue");
                int maxQueueCount = -1;
                int.TryParse(str, out maxQueueCount);
                string host = Static_xmltools.GetXmlAttr(obj, "Host");
                int usehttps = 0;
                str = Static_xmltools.GetXmlAttr(obj, "useHttps");
                int.TryParse(str, out usehttps);
                mapPortGroup mpg = new mapPortGroup(host, lisnport, appkey, maxQueueCount, selectpolicy
                    ,_httpsPort,this, (listenHttpsEnum)(usehttps),mapType);
                xpathstr = ".//portMap";
                XmlNodeList maplist = Static_xmltools.SelectXmlNodes(obj, xpathstr);
                foreach (XmlNode onenode in maplist)
                {

                    string outHost = Static_xmltools.GetXmlAttr(onenode, "host");
                    string outPort = Static_xmltools.GetXmlAttr(onenode, "port");
                    string _outhttpsPort = Static_xmltools.GetXmlAttr(onenode, "httpsPort");
                    str = Static_xmltools.GetXmlAttr(onenode, "maxConnect");
                    int maxcount = int.Parse(str);
                    str = Static_xmltools.GetXmlAttr(onenode, "minConnect");
                    int mincount = int.Parse(str);
                    bool needcheck = true;
                    string needcl = Static_xmltools.GetXmlAttr(onenode, "needCheckLive");
                    bool.TryParse(needcl, out needcheck);
                    
                    mpg.addOutPort(outHost, outPort, _outhttpsPort, maxcount, mincount,needcheck);
                }
                if (mapType == 0)
                {
                    if (mapPortGroup_dic.ContainsKey(lisnport))
                    {
                        continue;
                    }


                    mapPortGroup_dic.Add(lisnport, mpg);
                }
                else
                {
                    if (maphttpGroup_dic.ContainsKey(appkey))
                    {
                        continue;
                    }
                    maphttpGroup_dic.Add(appkey, mpg);
                }
            }
        }

    }
    public enum zoneServerRoleEnum
    {
        master = 0, //主节点
        slave = 1,   //从节点
        repetiton = 2//副本
    }
    public enum eventServerChangeType
    {
        add=0,
        delete=1,
        roleChange=2,
        statuChange=3,
    }
    public class ClustChangeEventArgs : EventArgs
    {
        public readonly proxyNettyServer changeServer;
        public readonly eventServerChangeType changeType;
        public readonly string memo;
        public ClustChangeEventArgs()
        {

        }
        /// <summary>
        /// 集群服务变更事件
        /// </summary>
        /// <param name="_changeServer">变更服务器</param>
        /// <param name="_changeType">变更类型</param>
        public ClustChangeEventArgs(proxyNettyServer _changeServer, eventServerChangeType _changeType)
        {
            this.changeServer = _changeServer;
            this.changeType = _changeType;
        }
    }
    /// <summary>
    /// 服务集群实例,代表一个zone
    /// </summary>
    public class zoneServerCluster
    {
        [JsonProperty]
        public string id { get; private set; }
        //声明一个变更时的委托
        public delegate void ClusterChangeEventHander(object sender, ClustChangeEventArgs e);
        //在委托的机制下,建立变更事件
        public event ClusterChangeEventHander ChangeEvent;
        //声明一个可重写的OnChange的保护函数
        protected virtual void OnChange(ClustChangeEventArgs e)
        {
            if (ChangeEvent != null)
            {
                //Sender = this，也就是serverZooCluster
                this.ChangeEvent(this, e);
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

        public proxyNettyServer getAvailableZS(string appkey)
        {
            return null;
        }
        internal zoneServerCluster(string region, string zone, proxyNettyServer masterServer)
        {
            this.zoneName = zone;
            this.master = masterServer;
            this.regionName = region;


        }

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
            slave.setType(zoneServerRoleEnum.slave);
            this.OnChange(new ClustChangeEventArgs(_slave, eventServerChangeType.roleChange));
        }
        public void slaveToMaster()
        {
            master = slave;
            master.setType(zoneServerRoleEnum.master);
            slave = null;
            this.OnChange(new ClustChangeEventArgs(master, eventServerChangeType.roleChange));
        }
        public void setMaster(proxyNettyServer _master)
        {
            if (repetionList.Contains(_master))
                removeRepetion(_master);
            this.master = _master;
            master.setType(zoneServerRoleEnum.master);
            this.OnChange(new ClustChangeEventArgs(_master, eventServerChangeType.roleChange));
        }
        public void addRepetion(ref proxyNettyServer _server)
        {
            if (!repetionList.Contains(_server))
            {
                this.repetionList.Add(_server);
                _server.setType(zoneServerRoleEnum.repetiton);
                this.OnChange(new ClustChangeEventArgs(_server, eventServerChangeType.add));
            }
        }
        public void removeRepetion(proxyNettyServer _server)
        {
            if (repetionList.Contains(_server))
            {
                this.repetionList.Remove(_server);
                this.OnChange(new ClustChangeEventArgs(_server, eventServerChangeType.delete));
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
            if (this.id != offobj.id)
            {
                //todoXout.LogInf("");
                return;// 有异常问题，待处理
            }

            if (offobj.master != null && offobj.master.id != RunConfig.Instance.ownPNServer.id)
            {
                this.master = offobj.master;
            }
            if (offobj.master == null)
                this.master = null;
            if (offobj.slave != null && offobj.slave.id != RunConfig.Instance.ownPNServer.id)
            {
                this.slave = offobj.slave;
            }
            if (offobj.slave == null)
               this.slave=null;

            this.regionName = offobj.regionName;
            this.zoneName = offobj.zoneName;
            foreach (var one in offobj.repetionList)
            {
                if (one.id == RunConfig.Instance.ownPNServer.id)
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
                jobj.Add("master", master.toJson());
            if (this.slave != null)
                jobj.Add("slave", slave.toJson());
            foreach (var obj in repetionList)
                jarr.Add(obj.toJson());
            jobj.Add("repetionList", jarr);
            return jobj;

           
        }

       
        public void setClusterID(string cid)
        {
            this.id = cid;
        }
        public async Task startServerAsync()
        {
            try
            {
                if (RunConfig.Instance.thisRole != zoneServerRoleEnum.master)
                {
                  
                    
                }
                string basepath = AppDomain.CurrentDomain.BaseDirectory;

              

                RunConfig.Instance.ownPNServer.setStatus(serverStatusEnum.Ready);
                
                RunConfig.Instance.addActionMessage(
                   new actionMessage(enum_Actiondo.reportToMasterConfigData, 
                   RunConfig.Instance.ownPNServer.getServerMessageFrom(), ""));


            }
            catch (Exception exp)
            {
                Console.WriteLine("start server fail:{0}", exp.Message);
                throw exp;
            }            
        }
        public async Task stopServerAsync()
        {
         
            //tcplistener.Stop();
        }
        public void checkZSserver()
        {
            Console.WriteLine("checkZSserver:{0}", RunConfig.Instance.thisRole);
            if (RunConfig.Instance.thisRole == zoneServerRoleEnum.master)
            {

              
                bool shouldremove = false;
                if (slave!=null && slave.checkMeLive(out shouldremove))
                {
                    if (shouldremove)
                        slave = null;
                    RunConfig.Instance.addActionMessage(
                        new actionMessage(enum_Actiondo.noticeToAllZServrConfigData, master.getServerMessageFrom(), ""));
                    RunConfig.Instance.addActionMessage(
                        new actionMessage(enum_Actiondo.noticeToAllOMPConfigData, master.getServerMessageFrom(), ""));
                }
                //foreach (var obj in repetionList)
                for(int i=repetionList.Count-1;i>=0;i--)
                {
                    var obj = repetionList[i];
                    shouldremove = false;
                    if (obj.checkMeLive(out shouldremove) )
                    {
                        if (shouldremove)
                        {
                            lock (repetionList)
                                repetionList.Remove(obj);
                        }
                        RunConfig.Instance.addActionMessage(
                      new actionMessage(enum_Actiondo.noticeToAllZServrConfigData, master.getServerMessageFrom(), ""));
                        RunConfig.Instance.addActionMessage(
                      new actionMessage(enum_Actiondo.noticeToAllOMPConfigData, master.getServerMessageFrom(), ""));

                       
                    }
                }
                if (slave == null && repetionList.Count > 0)
                {
                    RunConfig.Instance.addActionMessage(
                     new actionMessage(enum_Actiondo.resetSlaveServer, master.getServerMessageFrom(), ""));
                   

                }
               
            }
        }
    }


    public class regionZoneServer
    {
        [JsonProperty]
        public string region { get; private set; }
        public proxyNettyServer regionMaster;
        public Dictionary<string, zoneServerCluster> zoneServer_dic;

        public JObject toJson()
        {
            var jobj = JObject.FromObject(this);
            return jobj;
        }
        public proxyNettyServer getServerById(string id)
        {
            foreach (var cluster in zoneServer_dic.Values)
            {
                return cluster.getzoneServerById(id);
            }
            return null;
        }
        public void rsycUpdate(regionZoneServer offobj)
        {
            if (offobj.regionMaster.id != RunConfig.Instance.ownPNServer.id)
            {
                this.regionMaster = offobj.regionMaster;
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
        public regionZoneServer(string regionName)
        {
            this.region = regionName;
            zoneServer_dic = new Dictionary<string, zoneServerCluster>(StringComparer.CurrentCultureIgnoreCase);
        }
    }
}
