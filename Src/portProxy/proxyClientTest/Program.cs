// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Telnet.Client
{
    using System;
    using System.Collections.Concurrent;
    using System.IO;
    using System.Net;
    using System.Net.Security;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using DotNetty.Buffers;
    using DotNetty.Codecs;
    using DotNetty.Handlers.Tls;
    using DotNetty.Transport.Bootstrapping;
    using DotNetty.Transport.Channels;
    using DotNetty.Transport.Channels.Sockets;
    using Newtonsoft.Json;
    using Proxy.Comm;
    using proxyComm;

    public class ClientObj
    {
        public AutoResetEvent WaitHandler { get; set; } = new AutoResetEvent(false);
        public string ResponseString { get; set; }
    }
    public class ClientWait
    {
        private ConcurrentDictionary<string, ClientObj> _waits { get; set; } = new ConcurrentDictionary<string, ClientObj>();
        public void Start(string id)
        {
            _waits[id] = new ClientObj();
        }
        public void Set(string id, string responseStriong)
        {
            var theObj = _waits[id];
            theObj.ResponseString = responseStriong;
            theObj.WaitHandler.Set();
        }
        public ClientObj Wait(string id)
        {
            var clientObj = _waits[id];
            clientObj.WaitHandler.WaitOne();
            Task.Run(() =>
            {
                _waits.TryRemove(id, out ClientObj value);
            });
            return clientObj;
        }
    }
    public class Program
    {
      public static ConcurrentDictionary<long, TaskCompletionSource<object>> requestTask = new ConcurrentDictionary<long, TaskCompletionSource<object>>();

        public static string ClientId = "";
        static async Task RunClientAsync()
        {
            ClientId = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            commSetting.SetConsoleLogger();

            var group = new MultithreadEventLoopGroup();

            X509Certificate2 cert = null;
            string targetHost = null;
            if (ClientSettings.IsSsl)
            {
                cert = new X509Certificate2(Path.Combine(commSetting.ProcessDirectory, "dotnetty.com.pfx"), "password");
                targetHost = cert.GetNameInfo(X509NameType.DnsName, false);
            }
            try
            {
                var bootstrap = new Bootstrap();
                bootstrap
                    .Group(group)
                    .Channel<TcpSocketChannel>()
                    .Option(ChannelOption.TcpNodelay, true)
                    .Handler(new ActionChannelInitializer<ISocketChannel>(channel =>
                    {
                        
                        IChannelPipeline pipeline = channel.Pipeline;

                        if (cert != null)
                        {
                            pipeline.AddLast(new TlsHandler(stream => new SslStream(stream, true, (sender, certificate, chain, errors) => true), new ClientTlsSettings(targetHost)));
                        }

                        pipeline.AddLast(new LengthFieldBasedFrameDecoder(commSetting.MAX_FRAME_LENGTH,commSetting.LENGTH_FIELD_OFFSET,commSetting.LENGTH_FIELD_LENGTH,commSetting.LENGTH_ADJUSTMENT,commSetting.INITIAL_BYTES_TO_STRIP,false));
                        pipeline.AddLast( new TelnetClientHandler());
                    }));

                IChannel bootstrapChannel = await bootstrap.ConnectAsync(new IPEndPoint(ClientSettings.Host, ClientSettings.Port));

                for (int i=0;i<10;i++)
                {
                    //Console.WriteLine("input new line");
                    //string line = Console.ReadLine();

                    //if (string.IsNullOrEmpty(line))
                    //{
                    //    continue;
                    //}

                    try
                    {
                        for (int j = 0; j < 1; j++)
                        {
                            string line = i.ToString()+"-" + j.ToString();
                            DateTime startdt = DateTime.Now;
                            var cp = new testpackage("t" + j.ToString(), (i * 100 + j));



                            if (cp == null)
                                return;
                            TaskCompletionSource<object> tcs1 = new TaskCompletionSource<object>();
                            requestTask[cp.id] = tcs1;
                            Task<object> t1 = tcs1.Task;



                            var content = System.Text.Encoding.Default.GetBytes(JsonConvert.SerializeObject(cp));
                            var bb = Unpooled.Buffer(content.Length+4);
                            bb.WriteInt(content.Length);
                            bb.WriteBytes(content);
                            await bootstrapChannel.WriteAndFlushAsync(bb);
                           var pack=((t1.Result as testpackage));
                            Console.WriteLine("threadId:{0},msg:{1},resp:{2},time:{3}", System.Threading.Thread.CurrentThread.ManagedThreadId, line, pack.msg, (DateTime.Now - startdt).TotalSeconds);

                            await  Task.Factory.StartNew(async () =>                         {

                             return;
                         });
                            System.Threading.Thread.Sleep(1000);
                        }
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                    //if (string.Equals(line, "bye", StringComparison.OrdinalIgnoreCase))
                    //{
                    //    await bootstrapChannel.CloseAsync();
                    //    break;
                    //}
                }
                Console.WriteLine("input");
                Console.ReadLine();
                await bootstrapChannel.CloseAsync();
            }
            finally
            {
                group.ShutdownGracefullyAsync().Wait(1000);
            }
        }

        static void Main() => RunClientAsync().Wait();
    }
}