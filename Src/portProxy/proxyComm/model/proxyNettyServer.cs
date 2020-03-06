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
using FrmLib.Http;

namespace Proxy.Comm.model
{
   
    /// <summary>
    /// 服务描述，一个用于转发和注册的服务器描述;
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
        [XmlIgnore]
        [JsonIgnore]
        HttpHelper httpClient;
        [XmlIgnore]
        [JsonIgnore]
        MultithreadEventLoopGroup checkGroup;


        public override void ServerChanged(object sender, serverChangeEventArgs e)
        {
            //处理消息,本身本服务器发生的变更，不需要处理
            if (localRunServer.Instance.ownServer == null || localRunServer.Instance.ownServer != this)
                return;
            if (localRunServer.Instance.zoneRole != ServerRoleEnum.zoneMaster)
                localRunServer.Instance.addActions(new actionMessage(enum_Actiondo.ServerChanged, this.id, localRunServer.Instance.region));
            else
                localRunServer.Instance.addActions(new actionMessage(enum_Actiondo.needNoticeServer, this.id, localRunServer.Instance.region));
         
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

                    this.onChonage(new serverChangeEventArgs(this.id, serverChangeTypeEnum.serverParamsChanged, "mapPortGroup_dic", null, "新增组:"+mgp.port));
                }
                return;
            }
            if(mgp.mapType==1)
            {
                if (!maphttpGroup_dic.ContainsKey(mgp.appkey))
                {
                    maphttpGroup_dic.Add(mgp.appkey, mgp);
                      this.onChonage(new serverChangeEventArgs(this.id, serverChangeTypeEnum.serverParamsChanged, "maphttpGroup_dic", null, "新增组:"+mgp.appkey));
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
                    this.onChonage(new serverChangeEventArgs(this.id, serverChangeTypeEnum.serverParamsChanged, "mapPortGroup_dic", null, "移除组:" + mpg.port));
                }
                return;
            }
            if (mpg.mapType == 1)
            {
                if (!maphttpGroup_dic.ContainsKey(mpg.appkey))
                {
                    maphttpGroup_dic.Remove(mpg.appkey);
                    this.onChonage(new serverChangeEventArgs(this.id, serverChangeTypeEnum.serverParamsChanged, "maphttpGroup_dic", null, "移除组:" + mpg.appkey));
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
                this.onChonage(new serverChangeEventArgs(this.id, serverChangeTypeEnum.serverParamsChanged, "isServerRegister", isServerRegister, value));
                _isServerRegister = value;
            } }
        private bool _isServerRegister;
        public void setServerRegisterEnable(bool isorNot)
        {
            this.isServerRegister = isorNot;
        }
        /// <summary>
        /// 是否是网关中心，提供端口转发与路由功能
        /// </summary>
        [JsonProperty]
        public bool isServerGater { get { return _isServerGater; } private set {
                this.onChonage(new serverChangeEventArgs(this.id, serverChangeTypeEnum.serverParamsChanged, "isServerGater", isServerGater, value));
                _isServerGater = value;
            } }
        public void setServerGaterEnable(bool isorNot)
        {
            this.isServerGater = isorNot;
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


        /// <summary>
        /// 测试主机的指定url是否有反应
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public bool canHaveEcho(string url)
        {
            var respone = httpClient.doAsycHttpRequest(url);
            if (respone.IsSuccessStatusCode)
            {
                return true;
            }
            return false;
        }
        /// <summary>
        /// 监测主机是否存活
        /// </summary>
        /// <param name="omp"></param>
        /// <param name="mpg"></param>
        /// <returns></returns>
        private bool checkOneMapHostOk(outMapPort omp, mapPortGroup mpg)
        {

            if (mpg.mapType == 1)
            {
                //http转发,如果设置sayEchoUrl，就使用http请求测试，否则
                if (!string.IsNullOrEmpty(omp.sayEchoUrl))
                {
                    var url = string.Format("{0}://{1}:{2}/{3}", string.IsNullOrEmpty(omp.httpsPort) ? "http" : "https", omp.host, string.IsNullOrEmpty(omp.httpsPort) ? omp.port : omp.httpsPort, omp.sayEchoUrl);
                    return canHaveEcho(url);
                }

            }
            try
            {
                Console.WriteLine("check tcp port");
                var bsp = new Bootstrap();
                bsp
                    .Group(checkGroup)
                    .Channel<TcpSocketChannel>()
                    .Option(ChannelOption.TcpNodelay, true)
                    .Option(ChannelOption.SoKeepalive, true)
                     .Option(ChannelOption.ConnectTimeout, TimeSpan.FromSeconds(1))
                    .Handler(new ActionChannelInitializer<ISocketChannel>(c =>
                    {
                        IChannelPipeline pipeline = c.Pipeline;

                    }));
                bsp.RemoteAddress(new IPEndPoint(IPAddress.Parse(omp.host), int.Parse(omp.port)));

                var clientChannel = AsyncHelpers.RunSync<IChannel>(() => bsp.ConnectAsync());
                AsyncHelpers.RunSync(() => clientChannel.CloseAsync());
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("check tcp port error:"+FrmLib.Extend.tools_static.getExceptionMessage(ex));
                return false;
            }


        }
        private bool checkOneGruopChange(mapPortGroup mpg)
        {
            var haveChanged = false;
            var allfailed = true;
            foreach (var oneHost in mpg.outPortList)
            {
                if (oneHost.status == serverStatusEnum.Disable)
                    continue;
                var ts = (DateTime.Now - oneHost.lastLive).TotalSeconds;
                var oldstatus = oneHost.status;
                if (ts > localRunServer.Instance.serverRemoveTimes)
                {
                    //
                    oneHost.setStatus(serverStatusEnum.Disable);
                }
                else
                {
                    if (ts > localRunServer.Instance.serverFailTimes)
                    {


                        if (checkOneMapHostOk(oneHost, mpg))
                        {
                            oneHost.lastLive = DateTime.Now;
                            oneHost.setStatus(serverStatusEnum.Ready);
                            allfailed = false;
                        }
                        else
                        {
                            oneHost.setStatus(serverStatusEnum.Fail);

                        }

                    }

                }
               
                if (oldstatus != oneHost.status)
                    haveChanged = true;
                if (oneHost.status == serverStatusEnum.Ready)
                    allfailed = false;
            }
            if (allfailed)
            {
                mpg.setstatus( serverStatusEnum.Fail);

            }
            else
            {
                mpg.setstatus(serverStatusEnum.Ready);
                mpg.lastLive = DateTime.Now;
            }
            return haveChanged;

        }
       /// <summary>
       /// 如果有变化返回true
       /// </summary>
       /// <returns></returns>
        public bool checkMapGroupChange()
        {
            bool haveChanged = false;
            foreach (var oneGroup in maphttpGroup_dic)
            {
                haveChanged = checkOneGruopChange(oneGroup.Value);
            }
            foreach (var oneGroup in mapPortGroup_dic)
            {
                haveChanged = checkOneGruopChange(oneGroup.Value);
            }
            return haveChanged;

        }

        public proxyNettyServer(string cid, string mapHttpPort,string _ServiceUrl, string mapHttpsPort=null,int failtime=100, int _maxPerfData=100) : base(cid,failtime,_maxPerfData)
        {          
            this.port = mapHttpPort;//http转发服务的端口，不是集群服务api的端口
            this.httpsPort = mapHttpsPort;//同上
           
            mapPortGroup_dic = new Dictionary<string, mapPortGroup>(StringComparer.OrdinalIgnoreCase);
            maphttpGroup_dic = new Dictionary<string, mapPortGroup>(StringComparer.OrdinalIgnoreCase);
            checkGroup=new MultithreadEventLoopGroup();
            httpClient = new HttpHelper(new TimeSpan(0, 0, 3));


            if (!string.IsNullOrEmpty(cid)) //如果cid==null，非本地服务，不需要启动dotnett服务
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
                string listenport = Static_xmltools.GetXmlAttr(obj, "listenPort");
                string str = Static_xmltools.GetXmlAttr(obj, "Policy");                
                int policy = 0;
                int.TryParse(str, out policy);

                int mapType=0;
                 str = Static_xmltools.GetXmlAttr(obj, "mapType");
                int.TryParse(str, out mapType);
                if (mapType == 1)
                {

                    listenport = this.port;
                }
                string _httpsPort = Static_xmltools.GetXmlAttr(obj, "listenHttpsPort");

                outPortSelectPolicy selectpolicy = (outPortSelectPolicy)policy;
                str = Static_xmltools.GetXmlAttr(obj, "MaxWaitQueue");
                int maxQueueCount = -1;
                int.TryParse(str, out maxQueueCount);
                string host = Static_xmltools.GetXmlAttr(obj, "Host");
                int usehttps = 0;
                str = Static_xmltools.GetXmlAttr(obj, "useHttps");
                int.TryParse(str, out usehttps);
                mapPortGroup mpg = new mapPortGroup(host, listenport, appkey, maxQueueCount, selectpolicy
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
                    if (mapPortGroup_dic.ContainsKey(listenport))
                    {
                        continue;
                    }


                    mapPortGroup_dic.Add(listenport, mpg);
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
   




}
