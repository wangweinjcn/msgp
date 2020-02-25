// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Proxy.Server
{
    using System;
    using System.IO;
    using System.Net;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;
    using DotNetty.Codecs;
    using DotNetty.Handlers.Logging;
    using DotNetty.Handlers.Tls;
    using DotNetty.Transport.Bootstrapping;
    using DotNetty.Transport.Channels;
    using DotNetty.Transport.Channels.Sockets;
    using Proxy.Comm;
    using Proxy.Server.socket;

    class Program
    {
        static async Task RunServerAsync()
        {
           
              commSetting.SetConsoleLogger();
            var bossGroup = new MultithreadEventLoopGroup(1);
            var workerGroup = new MultithreadEventLoopGroup();


            var SERVER_HANDLER = new ProxyServerHandler();

            X509Certificate2 tlsCertificate = null;
            if (ServerSettings.IsSsl)
            {
                tlsCertificate = new X509Certificate2(Path.Combine(commSetting.ProcessDirectory, "dotnetty.com.pfx"), "password");
            }
            try
            {
                var bootstrap = new ServerBootstrap();
                bootstrap
                    .Group(bossGroup, workerGroup)
                    .Channel<CustTcpServerSocketChannel>()
                    .Option(ChannelOption.SoBacklog, 100)
                    .Handler(new LoggingHandler(LogLevel.INFO))
                    .ChildHandler(new ActionChannelInitializer<ISocketChannel>(channel =>
                    {
                        IChannelPipeline pipeline = channel.Pipeline;
                        if (tlsCertificate != null)
                        {
                            pipeline.AddLast(TlsHandler.Server(tlsCertificate));
                        }

                        pipeline.AddLast(new LengthFieldBasedFrameDecoder(commSetting.MAX_FRAME_LENGTH, commSetting.LENGTH_FIELD_OFFSET, commSetting.LENGTH_FIELD_LENGTH, commSetting.LENGTH_ADJUSTMENT, commSetting.INITIAL_BYTES_TO_STRIP, false));
                        pipeline.AddLast(SERVER_HANDLER);
                    }));

                IChannel bootstrapChannel = await bootstrap.BindAsync(ServerSettings.Port);

                Console.ReadLine();

                await bootstrapChannel.CloseAsync();
            }
            finally
            {
                Task.WaitAll(bossGroup.ShutdownGracefullyAsync(), workerGroup.ShutdownGracefullyAsync());
            }
        }

        static void Main()
        {
          //  test();
            var instance = RunConfig.Instance;
            instance.ownPNServer.loadPortProxyCfg();
            instance.ownPNServer.Start();
            Console.ReadKey();
        }

        static void test()
        {
            var group = new MultithreadEventLoopGroup();
            try
            {
                var bootstrap = new Bootstrap();
                var phandle = new ServerSocketClientHandler(null);
                bootstrap
                    .Group(group)
                    .Channel<TcpSocketChannel>()
                    .Option(ChannelOption.TcpNodelay, true)
                     .Option(ChannelOption.ConnectTimeout, TimeSpan.FromSeconds(1))
                    .Handler(new ActionChannelInitializer<ISocketChannel>(c =>
                    {
                        IChannelPipeline pipeline = c.Pipeline;
                        pipeline.AddLast(phandle);
                    }));
                bootstrap.RemoteAddress(new IPEndPoint(IPAddress.Parse("122.112.158.14"), int.Parse("3306")));
                var tmp = bootstrap.ConnectAsync();
                tmp.Wait();
                IChannel clientChannel = tmp.Result;
            }
            catch (Exception e)
            {

            }
            }
    }
}