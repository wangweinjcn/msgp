using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using DotNetty.Codecs.Http;
using DotNetty.Handlers.Logging;
using DotNetty.Handlers.Timeout;
using DotNetty.Handlers.Tls;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using DotNetty.Transport.Libuv;
using msgp.common.dotnetty;
using msgp.mc.model;

namespace msgp.mc.server
{
    public class mcServer :IDisposable
    {
        IChannel rpcServerChannel;
        #region IDisponse
        /// <summary>
        /// 获取对象是否已释放
        /// </summary>
        public bool IsDisposed { get; private set; }

        public void startSocketServer()
        { }
        public void startHttpServer()
        { }
        /// <summary>
        /// 关闭和释放所有相关资源
        /// </summary>
        public void Dispose()
        {
            if (this.IsDisposed == false)
            {
                this.Dispose(true);
                GC.SuppressFinalize(this);
            }
            this.IsDisposed = true;
        }
        MultithreadEventLoopGroup socketbossGroup,httpbossGroup;
        MultithreadEventLoopGroup socketworkerGroup,httpworkerGroup;
        private ConcurrentDictionary<string, IChannel> allchannels = new ConcurrentDictionary<string, IChannel>();

        /// <summary>
        /// 释放资源
        /// </summary>
        /// <param name="disposing">是否也释放托管资源</param>
        protected virtual void Dispose(bool disposing)
        {
            rpcServerChannel.CloseAsync();
            if(socketbossGroup!=null)
                socketbossGroup.ShutdownGracefullyAsync();
            if(socketworkerGroup!=null)
                socketworkerGroup.ShutdownGracefullyAsync();
            if (httpbossGroup != null)
                httpbossGroup.ShutdownGracefullyAsync();
            if (httpworkerGroup != null)
                httpworkerGroup.ShutdownGracefullyAsync();
            foreach (var obj in allchannels.Values)
                obj.CloseSafe();
            allchannels.Clear();
        }
        #endregion
        /// <summary>
        /// 连接事件
        /// </summary>
        /// <param name="channel"></param>
        internal void onConnect(IChannel channel)
        {
            IChannel fs;
            if (!allchannels.ContainsKey(channel.Id.AsLongText()))
            {
                if (allchannels.TryRemove(channel.Id.AsLongText(), out fs))
                {
                    fs.CloseSafe();
                }
            }
        }
        /// <summary>
        /// 处理断开连接
        /// </summary>
        /// <param name="channel"></param>
        internal void onDisconnect(IChannel channel)
        {
            
            if (allchannels.ContainsKey(channel.Id.AsLongText()))
            {
               
                allchannels.TryAdd(channel.Id.AsLongText(), channel);
            }
        }
        internal async void ProcessPacketAsync(IChannel channel, cmdMessage packet)
        {

            baseMcObject packageData = null;
            switch (packet.datatype)
            {

                case (datatypeEnum.system_int):
                    packageData=MessagePack.MessagePackSerializer.Deserialize<mcObject<int>>(packet.data, MessagePack.Resolvers.ContractlessStandardResolver.Instance);
                    break;
                case (datatypeEnum.system_int32):
                      packageData=MessagePack.MessagePackSerializer.Deserialize<mcObject<Int32>>(packet.data, MessagePack.Resolvers.ContractlessStandardResolver.Instance);
                    break;
                case (datatypeEnum.system_int16):
                     packageData=MessagePack.MessagePackSerializer.Deserialize<mcObject<Int16>>(packet.data, MessagePack.Resolvers.ContractlessStandardResolver.Instance);
                    break;
                case (datatypeEnum.system_int64):

                case (datatypeEnum.system_long):
                    packageData=MessagePack.MessagePackSerializer.Deserialize<mcObject<Int64>>(packet.data, MessagePack.Resolvers.ContractlessStandardResolver.Instance);
                    break;
                case (datatypeEnum.system_int_array):
                  packageData=MessagePack.MessagePackSerializer.Deserialize<mcObject<int[]>>(packet.data, MessagePack.Resolvers.ContractlessStandardResolver.Instance);
                    break;
                case (datatypeEnum.system_int32_array):
                      packageData=MessagePack.MessagePackSerializer.Deserialize<mcObject<Int32[]>>(packet.data, MessagePack.Resolvers.ContractlessStandardResolver.Instance);
                    break;
                case (datatypeEnum.system_int16_array):
                     packageData=MessagePack.MessagePackSerializer.Deserialize<mcObject<Int16[]>>(packet.data, MessagePack.Resolvers.ContractlessStandardResolver.Instance);
                    break;
                case (datatypeEnum.system_int64_array):
                case (datatypeEnum.system_long_array):
                     packageData=MessagePack.MessagePackSerializer.Deserialize<mcObject<Int64[]>>(packet.data, MessagePack.Resolvers.ContractlessStandardResolver.Instance);
                    break;
                case (datatypeEnum.system_string):
                     packageData=MessagePack.MessagePackSerializer.Deserialize<mcObject<string>>(packet.data, MessagePack.Resolvers.ContractlessStandardResolver.Instance);
                    break;
                case (datatypeEnum.system_bool):
                     packageData=MessagePack.MessagePackSerializer.Deserialize<mcObject<bool>>(packet.data, MessagePack.Resolvers.ContractlessStandardResolver.Instance);
                    break;
                case (datatypeEnum.system_byte):
                    packageData = MessagePack.MessagePackSerializer.Deserialize<mcObject<byte>>(packet.data, MessagePack.Resolvers.ContractlessStandardResolver.Instance);
                    break;
                case (datatypeEnum.system_float):
                    packageData = MessagePack.MessagePackSerializer.Deserialize<mcObject<float>>(packet.data, MessagePack.Resolvers.ContractlessStandardResolver.Instance);

                    break;
                case (datatypeEnum.system_double):
                    packageData = MessagePack.MessagePackSerializer.Deserialize<mcObject<double>>(packet.data, MessagePack.Resolvers.ContractlessStandardResolver.Instance);

                    break;
                case (datatypeEnum.system_decimal):
                     packageData = MessagePack.MessagePackSerializer.Deserialize<mcObject<decimal>>(packet.data, MessagePack.Resolvers.ContractlessStandardResolver.Instance);
                    break;
                case (datatypeEnum.system_decimal_array):
                     packageData = MessagePack.MessagePackSerializer.Deserialize<mcObject<decimal[]>>(packet.data, MessagePack.Resolvers.ContractlessStandardResolver.Instance);
                    break;
                case (datatypeEnum.system_double_array):
                     packageData = MessagePack.MessagePackSerializer.Deserialize<mcObject<double[]>>(packet.data, MessagePack.Resolvers.ContractlessStandardResolver.Instance);
                    break;
                case (datatypeEnum.system_float_array):
                     packageData = MessagePack.MessagePackSerializer.Deserialize<mcObject<double[]>>(packet.data, MessagePack.Resolvers.ContractlessStandardResolver.Instance);
                    break;
                case (datatypeEnum.system_byte_array):
                     packageData = MessagePack.MessagePackSerializer.Deserialize<mcObject<byte[]>>(packet.data, MessagePack.Resolvers.ContractlessStandardResolver.Instance);
                    break;
                case (datatypeEnum.system_bool_array):
                     packageData = MessagePack.MessagePackSerializer.Deserialize<mcObject<bool[]>>(packet.data, MessagePack.Resolvers.ContractlessStandardResolver.Instance);
                    break;
                case (datatypeEnum.system_string_array):
                     packageData = MessagePack.MessagePackSerializer.Deserialize<mcObject<string[]>>(packet.data, MessagePack.Resolvers.ContractlessStandardResolver.Instance);
                    break;

                case (datatypeEnum.system_collections_arraylist):
                     packageData = MessagePack.MessagePackSerializer.Deserialize<mcObject<ArrayList>>(packet.data, MessagePack.Resolvers.ContractlessStandardResolver.Instance);
                    break;
                case (datatypeEnum.system_collections_hashtable):
                     packageData = MessagePack.MessagePackSerializer.Deserialize<mcObject<Hashtable>>(packet.data, MessagePack.Resolvers.ContractlessStandardResolver.Instance);
                    break;
                default:

                    break;

            }
            if (packageData == null)
            { return; }
            switch (packet.cmd)
            {
                case (commandEnum.add):
                    break;
                case (commandEnum.connect):
                    break;
                case (commandEnum.addlock):
                    break;
                case (commandEnum.delete):
                    break;
                case (commandEnum.read):
                    break;
                case (commandEnum.update):
                    break;
                case (commandEnum.unknown):
                default:
                    break;

            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="httpport"></param>
        /// <param name="blength"></param>
        /// <returns></returns>
        async Task RunHttpServerAsync(int httpport,int blength)
        {
            Console.WriteLine(
                $"\n{RuntimeInformation.OSArchitecture} {RuntimeInformation.OSDescription}"
                + $"\n{RuntimeInformation.ProcessArchitecture} {RuntimeInformation.FrameworkDescription}"
                + $"\nProcessor Count : {Environment.ProcessorCount}\n");

            bool useLibuv = ServerSettings.UseLibuv;
            Console.WriteLine("Transport type : " + (useLibuv ? "Libuv" : "Socket"));

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency;
            }

            Console.WriteLine($"Server garbage collection: {GCSettings.IsServerGC}");
            Console.WriteLine($"Current latency mode for garbage collection: {GCSettings.LatencyMode}");


            if (useLibuv)
            {
                throw new NotImplementedException();
            }
            else
            {
                httpbossGroup = new MultithreadEventLoopGroup(1);
                httpworkerGroup = new MultithreadEventLoopGroup();
            }

            X509Certificate2 tlsCertificate = null;
            if (ServerSettings.IsSsl)
            {
                tlsCertificate = new X509Certificate2(Path.Combine(dotneetyCommonHelper.ProcessDirectory, "dotnetty.com.pfx"), "password");
            }
            try
            {
                var bootstrap = new ServerBootstrap();
                bootstrap.Group(httpbossGroup, httpworkerGroup);

                if (useLibuv)
                {
                    bootstrap.Channel<TcpServerChannel>();
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
                        || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    {
                        bootstrap
                            .Option(ChannelOption.SoReuseport, true)
                            .ChildOption(ChannelOption.SoReuseaddr, true);
                    }
                }
                else
                {
                    bootstrap.Channel<TcpServerSocketChannel>();
                }

                bootstrap
                    .Option(ChannelOption.SoBacklog, blength)
                    .ChildHandler(new ActionChannelInitializer<IChannel>(channel =>
                    {
                        IChannelPipeline pipeline = channel.Pipeline;
                        if (tlsCertificate != null)
                        {
                            pipeline.AddLast(TlsHandler.Server(tlsCertificate));
                        }
                        pipeline.AddLast("encoder", new HttpResponseEncoder());
                        pipeline.AddLast("decoder", new HttpRequestDecoder(dotneetyCommonHelper.MAX_HTTP_Initial_line_LENGTH, dotneetyCommonHelper.MAX_HTTP_Headers_LENGTH, dotneetyCommonHelper.MAX_HTTP_ChunkSize_LENGTH, false));
                        pipeline.AddLast("decoder2", new HttpObjectAggregator(dotneetyCommonHelper.MAX_HTTP_Body_LENGTH)); //40M大小
                        pipeline.AddLast("handler", new HttpServerHandler());
                    }));

                IChannel bootstrapChannel = await bootstrap.BindAsync(IPAddress.IPv6Any, httpport);

                Console.WriteLine($"Httpd started. Listening on {bootstrapChannel.LocalAddress}");
                Console.ReadLine();

              
            }
            finally
            {
               // group.ShutdownGracefullyAsync().Wait();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="port">端口号</param>
        /// <param name="bLength">队列等待线程数量</param>
        /// <returns></returns>
        public async Task RunMessagePackSocketServerAsync(int port,int bLength)
        {
            dotneetyCommonHelper.SetConsoleLogger();



            if (ServerSettings.UseLibuv)
            {
                throw new NotImplementedException();
            }
            else
            {
                socketbossGroup = new MultithreadEventLoopGroup(1);
                socketworkerGroup = new MultithreadEventLoopGroup();
            }

            X509Certificate2 tlsCertificate = null;
            if (ServerSettings.IsSsl)
            {
                tlsCertificate = new X509Certificate2(Path.Combine(dotneetyCommonHelper.ProcessDirectory, "dotnetty.com.pfx"), "password");
            }
            try
            {
                var bootstrap = new ServerBootstrap();
                bootstrap.Group(socketbossGroup, socketworkerGroup);

                 bootstrap.Channel<CustTcpServerSocketChannel>();

                bootstrap
                    .Option(ChannelOption.SoBacklog, 100)
                    .Handler(new LoggingHandler("SRV-LSTN"))
                    .ChildHandler(new ActionChannelInitializer<IChannel>(channel =>
                    {
                        IChannelPipeline pipeline = channel.Pipeline;
                        if (tlsCertificate != null)
                        {
                            pipeline.AddLast("tls", TlsHandler.Server(tlsCertificate));
                        }
                        pipeline.AddLast("framing-enc", new CommandPacketEncoder());
                        pipeline.AddLast("framing-dec", new CommandPPacketDecode(dotneetyCommonHelper.MAX_FRAME_LENGTH, dotneetyCommonHelper.LENGTH_FIELD_OFFSET, dotneetyCommonHelper.LENGTH_FIELD_LENGTH, dotneetyCommonHelper.LENGTH_ADJUSTMENT, dotneetyCommonHelper.INITIAL_BYTES_TO_STRIP, false));

                        pipeline.AddLast(new IdleStateHandler(150, 0, 0));

                        socketServerHandler ServerHandler = new socketServerHandler(this)
                        {
                           
                        };
                       
                        pipeline.AddLast(ServerHandler);
                    }));

                IChannel boundChannel = await bootstrap.BindAsync(ServerSettings.Port);

                Console.ReadLine();

               
            }
            finally
            {
             
            }
        }
    }
}
