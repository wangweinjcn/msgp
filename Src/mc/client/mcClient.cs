using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using DotNetty.Codecs;
using DotNetty.Handlers.Tls;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using msgp.common.dotnetty;
using msgp.mc.model;
using Newtonsoft.Json;

namespace msgp.mc.client
{
    /// <summary>
    /// 表示Fast协议的tcp客户端
    /// </summary>
    public class mcClient 
    {
        private IPAddress host;
        private int port;
        private bool useSSl;
        private string sslFile;
        private string sslPassword;
        
        /// <summary>
        /// 获取或设置请求等待超时时间(毫秒) 
        /// 默认30秒
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public TimeSpan TimeOut { get; set; }
        /// <summary>
        /// 
        /// </summary>
        private string ClientId = null;
        /// <summary>
        /// 
        /// </summary>
        protected IChannel clientChannel { get; set; }
        /// <summary>
        /// Fast协议的tcp客户端
        /// </summary>
        public mcClient():this(ClientSettings.Host, ClientSettings.Port,false,"")
        {
          }

        public mcClient(IPAddress _host,int _port,bool _usessl,string _sslfile,string _sslpassword="")
        {
            host = _host;
            port =_port;
            useSSl = _usessl;
            sslFile = _sslfile;
            sslPassword = _sslpassword;
            this.Init();
           
        }
        public async Task connect()
        {
          await  this.startClientAsync();
        }
        public async Task DisposeAsync()
        {
            await clientChannel.CloseAsync();

        }

        private bool Send(cmdMessage pack)
        {
            this.clientChannel.WriteAndFlushAsync(pack);
            return true;
        }
        

        /// <summary>
        /// 初始化
        /// </summary>
        private void Init()
        {
          
            this.TimeOut = TimeSpan.FromSeconds(30);
        }

       
        /// <summary>
        /// 处理接收到服务发来的数据包
        /// </summary>
        /// <param name="packet">数据包</param>
        internal async void ProcessPacketAsync(cmdMessage packet)
        {
           
        }

     

        /// <summary>
        /// 发送数据包
        /// </summary>
        /// <param name="package">数据包</param>
        /// <returns></returns>
        private bool TrySendPackage(cmdMessage package)
        {
            try
            {
                return this.Send(package);
            }
            catch (Exception)
            {
                return false;
            }
        }


        /// <summary>
        ///  当操作中遇到处理异常时，将触发此方法
        /// </summary>
        /// <param name="packet">数据包对象</param>
        /// <param name="exception">异常对象</param> 
        protected virtual void OnException(cmdMessage packet, Exception exception)
        {
        }

        

      
        /// <summary>
        /// 断开时清除数据任务列表  
        /// </summary>
        protected  void OnDisconnected()
        {
           
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public  void Dispose()
        {
          

           
          //  this.clientChannel.CloseSafe();
        }
        async Task startClientAsync()
        {
            ClientId = DateTime.Now.ToString("yyyyMMdd-HHmmss");
           

            var group = new MultithreadEventLoopGroup();

            X509Certificate2 cert = null;
            string targetHost = null;
            if (useSSl)
            {
                cert = new X509Certificate2(Path.Combine(ClientSettings.ProcessDirectory,sslFile), sslPassword);
                targetHost = cert.GetNameInfo(X509NameType.DnsName, false);
            }
            try
            {
                var bootstrap = new Bootstrap();
                bootstrap
                    .Group(group)
                    .Channel<CustTcpSocketChannel>()
                    .Option(ChannelOption.TcpNodelay, true)
                    .Handler(new ActionChannelInitializer<ISocketChannel>(channel =>
                    {

                        IChannelPipeline pipeline = channel.Pipeline;

                        if (cert != null)
                        {
                            pipeline.AddLast(new TlsHandler(stream => new SslStream(stream, true, (sender, certificate, chain, errors) => true), new ClientTlsSettings(targetHost)));
                        }
                        pipeline.AddLast("framing-enc", new CommandPacketEncoder());
                        pipeline.AddLast("framing-dec", new CommandPPacketDecode(dotneetyCommonHelper.MAX_FRAME_LENGTH, dotneetyCommonHelper.LENGTH_FIELD_OFFSET, dotneetyCommonHelper.LENGTH_FIELD_LENGTH, dotneetyCommonHelper.LENGTH_ADJUSTMENT, dotneetyCommonHelper.INITIAL_BYTES_TO_STRIP, false));
                       

                        pipeline.AddLast("client", new mcClientHandler(this));



                    }));

                this.clientChannel =  bootstrap.ConnectAsync(new IPEndPoint(host, port)).GetAwaiter().GetResult();

                Console.WriteLine("now connect");

            }
            finally
            {

            }
        }
    }
   
}
