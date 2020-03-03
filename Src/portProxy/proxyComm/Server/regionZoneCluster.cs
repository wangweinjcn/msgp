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
    /// 服务实例，一个用于转发和注册的服务器描述;
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
        IChannel httpbootstrapChannel;

        public  void proxyServerChanged(object sender, serverChangeEventArgs e)
        {
            //处理消息,本身本服务器发生的变更，不需要处理
            if (localRunServer.Instance.ownServer == null || localRunServer.Instance.ownServer != this)
                return;
         
            localRunServer.Instance.addActions(new actionMessage(enum_Actiondo.ServerChanged, this.id, localRunServer.Instance.region));
         
        }
        public async Task<IChannel> startOnePortAsync(string lport)
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

                        pipeline.AddLast(SERVER_HANDLER);
                    }));

               var  bootstrapChannel = await portbootstrap.BindAsync(int.Parse(lport));
                Console.WriteLine($"Socket started. Listening on {lport}");
                return bootstrapChannel;




            }
            catch (Exception ex)
            {

                Task.WaitAll(bossGroup.ShutdownGracefullyAsync(), workerGroup.ShutdownGracefullyAsync());
                throw new Exception("服务启动失败");
            }
        }
        public async Task<IChannel> startHttpPortAsync(string lport)
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
                Console.WriteLine($"Httpd started. Listening on {httpbootstrapChannel.LocalAddress}");

                return httpbootstrapChannel;




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
                obj.Value.inputChannel = bs;
               
            }
           startHttpPortAsync(this.port);
        }
        public override async Task Stop()
        {
            foreach(var obj in mapPortGroup_dic.Values)
                 await obj.inputChannel.CloseAsync();
           Task.WaitAll(bossGroup.ShutdownGracefullyAsync(), workerGroup.ShutdownGracefullyAsync());

            await httpbootstrapChannel.CloseAsync();
            Task.WaitAll(httpbossGroup.ShutdownGracefullyAsync(), httpworkerGroup.ShutdownGracefullyAsync());
        }
        
        private bool needRestart;
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
        public int mapGroupCount { get {

                var count1 = (from x in maphttpGroup_dic.Values where x.status == serverStatusEnum.Ready select x).Count();
                var count2= (from x in mapPortGroup_dic.Values where x.status == serverStatusEnum.Ready select x).Count();
                return count1 + count2;
            } }
        /// <summary>
        /// 添加转发组
        /// </summary>
        /// <param name="mgp">转发组对象</param>
        public void addMapGroup(mapPortGroup mgp)
        {

            if (mgp.mapType == 0)
            {
                if (!mapPortGroup_dic.ContainsKey(mgp.port))
                {
                    mapPortGroup_dic.Add(mgp.port, mgp);
                    mgp.inputChannel= AsyncHelpers.RunSync<IChannel>(()=> startOnePortAsync(mgp.port));

                    this.Change(new serverChangeEventArgs(this.id, serverChangeTypeEnum.serverParamsChanged, "mapPortGroup_dic", null, "新增组:"+mgp.port));
                }
                return;
            }
            if(mgp.mapType==1)
            {
                if (!maphttpGroup_dic.ContainsKey(mgp.appkey))
                {
                    maphttpGroup_dic.Add(mgp.appkey, mgp);
                      this.Change(new serverChangeEventArgs(this.id, serverChangeTypeEnum.serverParamsChanged, "maphttpGroup_dic", null, "新增组:"+mgp.appkey));
                }
                return;
            }
           
        }
        /// <summary>
        /// 移除转发组
        /// </summary>
        /// <param name="mapKey"></param>
        /// <param name="mapType"></param>
        public void removeMapGroup(mapPortGroup mpg)
        {
            if (mpg.mapType == 0)
            {
                if (!mapPortGroup_dic.ContainsKey(mpg.port))
                {
                    mapPortGroup_dic.Remove(mpg.port);
                    AsyncHelpers.RunSync(() => mpg.inputChannel.CloseAsync());
                    this.Change(new serverChangeEventArgs(this.id, serverChangeTypeEnum.serverParamsChanged, "mapPortGroup_dic", null, "移除组:" + mpg.port));
                }
                return;
            }
            if (mpg.mapType == 1)
            {
                if (!maphttpGroup_dic.ContainsKey(mpg.appkey))
                {
                    maphttpGroup_dic.Remove(mpg.appkey);
                    this.Change(new serverChangeEventArgs(this.id, serverChangeTypeEnum.serverParamsChanged, "maphttpGroup_dic", null, "移除组:" + mpg.appkey));
                }
                return;
            }
        }
        /// <summary>
        /// 改变端口转发组的转发端口
        /// </summary>
        /// <param name="mpg"></param>
        /// <param name="newPort"></param>
        public void changePort(mapPortGroup mpg,string newPort)
        {
            if (mpg.mapType != 0)
                return;
            if (mpg.port == newPort)
                return;
            if (mpg.ownServerId != this.id || !mapPortGroup_dic.ContainsKey(mpg.port) || mpg.id !=mapPortGroup_dic[mpg.port].id)
                return;

          
            AsyncHelpers.RunSync(() => mpg.inputChannel.CloseAsync());
            mapPortGroup_dic.Remove(mpg.port);
            mpg.port = newPort;
            mapPortGroup_dic.Add(newPort, mpg);
            mpg.inputChannel=AsyncHelpers.RunSync<IChannel>(()=> startOnePortAsync(mpg.port));

        }
        /// <summary>
        /// 改变http转发的appkey
        /// </summary>
        /// <param name="mpg"></param>
        /// <param name="newKey"></param>
        public void changeAppkey(mapPortGroup mpg, string newKey)
        {
            if (mpg.mapType != 1)
                return;
            if (mpg.appkey == newKey)
                return;
            if (mpg.ownServerId != this.id || !maphttpGroup_dic.ContainsKey(mpg.appkey) || mpg.id != maphttpGroup_dic[mpg.appkey].id)
                return;
            maphttpGroup_dic.Remove(mpg.appkey);
            mpg.appkey = newKey;
            maphttpGroup_dic.Add(newKey, mpg);
        }
        /// <summary>
        /// 是否是服务器注册中心，提供服务注册与服务查询
        /// </summary>
        [JsonProperty]
        public bool isServerRegister { get { return _isServerRegister; } private set {
                this.Change(new serverChangeEventArgs(this.id, serverChangeTypeEnum.serverParamsChanged, "isServerRegister", isServerRegister, value));
                _isServerRegister = value;
            } }
        private bool _isServerRegister;
        public void enableServerRegister()
        {
            this.isServerRegister = true;
        }
        /// <summary>
        /// 是否是网关中心，提供端口转发与路由功能
        /// </summary>
        [JsonProperty]
        public bool isServerGater { get { return _isServerGater; } private set {
                this.Change(new serverChangeEventArgs(this.id, serverChangeTypeEnum.serverParamsChanged, "isServerGater", isServerGater, value));
                _isServerGater = value;
            } }
        public void enableServerGater()
        {
            this.isServerGater = true;
        }
        /// <summary>
        /// 集群通信服务Url（api调用）
        /// </summary>
        [JsonProperty]
        public string serviceUrl { get; private set; }

        private bool _isServerGater;
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
 
       
       
      
        public proxyNettyServer(string cid, string mapHttpPort,string _ServiceUrl, string mapHttpsPort=null,int failtime=100, int _maxPerfData=100) : base(cid,failtime,_maxPerfData)
        {          
            this.port = mapHttpPort;//http转发服务的端口，不是集群服务api的端口
            this.httpsPort = mapHttpsPort;//同上
           
            mapPortGroup_dic = new Dictionary<string, mapPortGroup>(StringComparer.OrdinalIgnoreCase);
            maphttpGroup_dic = new Dictionary<string, mapPortGroup>(StringComparer.OrdinalIgnoreCase);

  


            if (string.IsNullOrEmpty(cid)) //如果cid==null，非本地服务，不需要启动dotnett服务
            {
                bossGroup = new MultithreadEventLoopGroup(1);
                workerGroup = new MultithreadEventLoopGroup();
                SERVER_HANDLER = new ProxyServerHandler();
                portbootstrap = new ServerBootstrap();
                httpbossGroup = new MultithreadEventLoopGroup(1);
                httpworkerGroup = new MultithreadEventLoopGroup();
                httpbootstrap = new ServerBootstrap();
            }
            this.serviceUrl = _ServiceUrl;
            this.id = (cid+ this.serviceUrl).ToMD5(); //保证在同一个zone中，同一个服务url肯定的是同样的id；
            this.needReportChange = true;
            this.changeEventHandle += proxyServerChanged;
        }
        public proxyNettyServer(string cid,int failtime, int _maxPerfData):this(cid,null,null,null,failtime,_maxPerfData)
        {
           
        }
        [JsonConstructor]
        public proxyNettyServer():this("",100,100)
        {

        }
        public JObject toJObject()
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
        public string toJson()
        {
            return JsonConvert.SerializeObject(this);
        }
        public static proxyNettyServer parlseJson(string str)
        {
            return JsonConvert.DeserializeObject<proxyNettyServer>(str);
        }
               
        /// <summary>
        /// 
        /// </summary>
        public void loadPortProxyCfg()
        {
            string filename = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, localRunServer.portProxyFileName);
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
                    
                    mpg.addOutPort(outHost, outPort, _outhttpsPort, maxcount, needcheck);
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
        public void checkMapPortGroupAlive()
        {

        }
    }
   

    /// <summary>
    /// 服务集群实例,代表一个zone
    /// </summary>
    public class zoneServerCluster
    {

        public void  clusterServerChanged(object sender, serverChangeEventArgs e)
        {
            //处理消息
            if (e.newValue.ToString()!=localRunServer.Instance.ownCluster.clusterId)
                return;
            if (e.changeServerId != localRunServer.Instance.ownServer.id)
                return;
            enum_Actiondo ead = enum_Actiondo.unknown;
            if (e.changeType != serverChangeTypeEnum.zoneMasterChanged )
            {
                ead = enum_Actiondo.resetZoneServers;
            }
            else
                ead = enum_Actiondo.resetZoneMasterServer;
             actionMessage am = new actionMessage(ead, e.newValue.ToString(), this.regionName, "", "", "");
            localRunServer.Instance.addActions(am);
        }
        [JsonProperty]
        public string clusterId { get; private set; }

        //在委托的机制下,建立变更事件
        public event serverChangeEvnent clusterChangeEvent;
        //声明一个可重写的OnChange的保护函数
        protected virtual void OnChange(serverChangeEventArgs e)
        {
            if (clusterChangeEvent != null)
            {
                //Sender = this，也就是serverZooCluster
                this.clusterChangeEvent(this, e);
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

        public proxyNettyServer getAvailableServer(string appkey)
        {
            return null;
        }
        public IList<proxyNettyServer> allAvailableServers()
        {
            List<proxyNettyServer> servers = new List<proxyNettyServer>();
            if (master != null && master.status == serverStatusEnum.Ready)
                servers.Add(master);
            if (slave != null && slave.status == serverStatusEnum.Ready)
                servers.Add(slave);
            foreach(var one in repetionList)
            {

                if (one != null && one.status == serverStatusEnum.Ready)
                    servers.Add(one);
            }
            return servers;
        }
        public zoneServerCluster(string region, string zone, proxyNettyServer masterServer)
        {
            this.zoneName = zone;
            this.master = masterServer;
            this.regionName = region;
          

        }
        [JsonConstructor]
        public zoneServerCluster():this("","",null)
        { }


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
            if (_slave == localRunServer.Instance.ownServer)
                localRunServer.Instance.zoneRole = ServerRoleEnum.zoneSlave;

            this.OnChange(new serverChangeEventArgs(_slave.id, serverChangeTypeEnum.zoneSlaveChanged,"slave","",this.clusterId));
        }

        public void setMaster(proxyNettyServer _master)
        {
            if (repetionList.Contains(_master))
                removeRepetion(_master);
            if (slave == _master)
                this.slave = null;
            this.master = _master;
            if (_master == localRunServer.Instance.ownServer)
                localRunServer.Instance.zoneRole = ServerRoleEnum.zoneMaster;
            this.OnChange(new serverChangeEventArgs(_master.id, serverChangeTypeEnum.zoneMasterChanged,"master set","",this.clusterId));
        }
        public void addRepetion( proxyNettyServer _server)
        {
            if (!repetionList.Contains(_server))
            {
                if (_server == localRunServer.Instance.ownServer)
                    localRunServer.Instance.zoneRole = ServerRoleEnum.zoneRepetiton;
                this.repetionList.Add(_server);
               
                this.OnChange(new serverChangeEventArgs(_server.id, serverChangeTypeEnum.zoneRepChanged,"repetionList add","",this.clusterId));
            }
        }
        public void removeRepetion(proxyNettyServer _server)
        {
            if (repetionList.Contains(_server))
            {
                this.repetionList.Remove(_server);
                if (_server == localRunServer.Instance.ownServer)
                    localRunServer.Instance.zoneRole = ServerRoleEnum.unkown;
                this.OnChange(new serverChangeEventArgs(_server.id, serverChangeTypeEnum.zoneRepRemoved,"repetionList delete","",this.clusterId));
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
            if (this.clusterId != offobj.clusterId)
            {
                //todoXout.LogInf("");
                return;// 有异常问题，待处理
            }

            if (offobj.master != null && offobj.master.id != localRunServer.Instance.ownServer.id)
            {
                this.master = offobj.master;
            }
            if (offobj.master == null)
                this.master = null;
            if (offobj.slave != null && offobj.slave.id != localRunServer.Instance.ownServer.id)
            {
                this.slave = offobj.slave;
            }
            if (offobj.slave == null)
               this.slave=null;

            this.regionName = offobj.regionName;
            this.zoneName = offobj.zoneName;
            foreach (var one in offobj.repetionList)
            {
                if (one.id == localRunServer.Instance.ownServer.id)
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
                jobj.Add("master", master.toJObject());
            if (this.slave != null)
                jobj.Add("slave", slave.toJObject());
            foreach (var obj in repetionList)
                jarr.Add(obj.toJObject());
            jobj.Add("repetionList", jarr);
            return jobj;

           
        }

       
        public void setClusterID(string cid)
        {
            this.clusterId = cid;
        }

        
        /// <summary>
        /// 获取集群中有效服务器数量
        /// </summary>
        /// <returns></returns>
        public int getZoneServerCount()
        {
            int count = 0;
            var one = this;
            if (one.master != null && one.master.status == serverStatusEnum.Ready)
                count++;
            if (one.slave != null && one.slave.status == serverStatusEnum.Ready)
                count++;
            foreach (var rep in one.repetionList)
            {
                if (rep.status == serverStatusEnum.Ready)
                    count++;
            }
            return count;
        }
    }

    /// <summary>
    /// 域服务,代表一个region
    /// </summary>
    public class regionZoneServer
    {
        [JsonProperty]
        public string region { get; private set; }
        public proxyNettyServer regionMaster{ get; private set; }
        public proxyNettyServer regionSlave{ get; private set; }
        public Dictionary<string, zoneServerCluster> zoneServer_dic;


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
            if (this.regionMaster != null && this.regionMaster.status==serverStatusEnum.Ready)
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
                result= cluster.getzoneServerById(id);
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
        public void setMaster(proxyNettyServer _master)
        {
            this.regionMaster = _master;
        }
        public void setSlave(proxyNettyServer _slave)
        {
            this.regionSlave = _slave;
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
