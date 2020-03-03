// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Proxy.Comm.socket
{
    using System;
    using System.Net;
    using System.Text;
    using System.Threading.Tasks;
    using DotNetty.Buffers;
    using DotNetty.Transport.Channels;
    using DotNetty.Transport.Channels.Sockets;
    using Newtonsoft.Json;
    using proxyComm;
    using System.Collections;
    using System.Collections.Generic;
    using DotNetty.Codecs;
    using Proxy.Comm;
    using DotNetty.Transport.Bootstrapping;
    using System.Security.Cryptography.X509Certificates;
    using FrmLib.Extend;

    public class ProxyServerHandler : SimpleChannelInboundHandler<object>
    {
        ServerSocketClientHandler pclientHandle;
        MultithreadEventLoopGroup group;
       
        public ProxyServerHandler()
        {
             group = new MultithreadEventLoopGroup();

        }
       
        public override void ChannelRegistered(IChannelHandlerContext context)
        {
            Console.WriteLine("ProxyServerHandler ChannelRegistered");
            var ctssc = context.Channel as CustTcpSocketChannel;
            string ClientId;
            if (ctssc != null)
            {
                ClientId = ctssc.ChannelMata.tags["channelKey"].ToString();
                Console.WriteLine("activate client CustTcpServerSocketChannel:{0}", ClientId);

            }
            else
            {
                throw new Exception("channel errror");
            }
            var lendp = (IPEndPoint)context.Channel.LocalAddress;
            if (!localRunServer.Instance.ownServer.PortGroupContainKey(lendp.Port.ToString()))
            {
                throw new Exception("not port found");
            }
            ctssc.mapPortG = localRunServer.Instance.ownServer.getPortGroupByKey(lendp.Port.ToString());
            var mapto =  ctssc.mapPortG.selectOutPortMaped();

            try
            {

                pclientHandle = new ServerSocketClientHandler(context);
                var bsp = new Bootstrap();
                bsp
                    .Group(group)
                    .Channel<CustTcpSocketChannel>()
                    .Option(ChannelOption.TcpNodelay, true)
                    .Option(ChannelOption.SoKeepalive,true)
                     .Option(ChannelOption.ConnectTimeout, TimeSpan.FromSeconds(1))
                    .Handler(new ActionChannelInitializer<ISocketChannel>(c =>
                    {
                        IChannelPipeline pipeline = c.Pipeline;
                        pipeline.AddLast(pclientHandle);
                    }));
                bsp.RemoteAddress(new IPEndPoint(IPAddress.Parse(mapto.host), int.Parse(mapto.port)));
               
                CustTcpSocketChannel clientChannel = AsyncHelpers.RunSync<IChannel>(() => bsp.ConnectAsync()) as CustTcpSocketChannel;
                var ctsc = clientChannel as CustTcpSocketChannel;
                var meta = ctsc.ChannelMata as CustChannelMetadata;
                meta.tags.AddOrUpdate("channelKey",ClientId,  (key, value) => value); //存放clientchannel的key值
                meta.tags.AddOrUpdate("serverChannelContext", context, (key, value) => value);
                ctsc.outMapPort = mapto;
                ctsc.outMapPort.addCount();
                ctssc.allclientchannel.Add(ClientId, clientChannel);
                ctssc.allclientCounter.Add(ClientId, 1);
                ctssc.bsp_dic.Add(ClientId, bsp);
            }
            catch (Exception e)
            {
                throw new Exception("cann't connect server;" + e.Message);
            }

            finally
            {

            }

        }
        /// <summary>
        /// 客户端连接过来
        /// </summary>
        /// <param name="context"></param>
        public override void HandlerAdded(IChannelHandlerContext context)
        {
            string ClientId = Guid.NewGuid().ToString();
            var type = context.Channel.GetType();
            var ctssc = context.Channel as CustTcpSocketChannel;
            if (ctssc != null)
            {
                Console.WriteLine("new client CustTcpServerSocketChannel:{0}", ClientId);
                ctssc.ChannelMata.tags.TryAdd("channelKey", ClientId);
            }
            else
            {
                throw new Exception("channel errror");
            }
           
            base.HandlerAdded(context);
        }
        public override void ChannelActive(IChannelHandlerContext context)
        {
              Console.WriteLine("ProxyServerHandler ChannelActive");

            var ctssc = context.Channel as CustTcpSocketChannel;
            string ClientId;
            if (ctssc != null)
            {
                ClientId = ctssc.ChannelMata.tags["channelKey"].ToString();
              
                Console.WriteLine("activate client CustTcpServerSocketChannel:{0}", ClientId);

            }
            else
            {
                throw new Exception("channel errror");
            }
            
            
          

        }

        protected override void ChannelRead0(IChannelHandlerContext contex, object msg)
        {
         
            string ClientId = "noId";
            var ctssc = contex.Channel as CustTcpSocketChannel;
            if (ctssc != null)
            {
               
                if (ctssc.ChannelMata.tags.ContainsKey("channelKey"))
                {

                    ClientId = ctssc.ChannelMata.tags["channelKey"].ToString();
                    Console.WriteLine(" ProxyServerHandler:{0}  forward send  msg", ClientId);
                    var clientchannel = ctssc.allclientchannel[ClientId] as CustTcpSocketChannel;

                    if (clientchannel != null && clientchannel.Active)
                    {
                        var bb = (msg as IByteBuffer);
                        bb.Retain();
                        if (!clientchannel.ChannelMata.tags.ContainsKey("readStart"))
                            clientchannel.ChannelMata.tags.AddOrUpdate("readStart", DateTime.Now, (key, value) => value);

                        (clientchannel as CustTcpSocketChannel).outMapPort.addRecvBytes(bb.ReadableBytes);
                        clientchannel.WriteAndFlushAsync(msg);
                    }
                    else
                    {
                        ctssc.cleanData();

                    }

                }
            }
            else
            {
                  throw new Exception("ChannelRead0 errror");
            }
            
            //string response;
            //bool close = false;
            //string str = msg.GetString(0, msg.ReadableBytes, System.Text.Encoding.Default);
            //var pack = JsonConvert.DeserializeObject<testpackage>(str);
            //pack.msg = pack.msg + " returned";
           
            //var content2 = System.Text.Encoding.Default.GetBytes(JsonConvert.SerializeObject(pack));
            //var bb= Unpooled.Buffer(content2.Length+4);
            //bb.WriteInt(content2.Length);
            //bb.WriteBytes(content2);
            //Console.WriteLine("length:{0}", bb.ReadableBytes);

            //Task wait_close = contex.WriteAndFlushAsync(bb);
            //Task.WaitAll(wait_close);
            //contex.CloseAsync();
            
           

        }
        
       
        public override void ChannelReadComplete(IChannelHandlerContext contex)
        {
            Console.WriteLine("ChannelReadComplete");
            contex.Flush();
        }

        public override void ExceptionCaught(IChannelHandlerContext contex, Exception e)
        {
            Console.WriteLine("{0}", e);
             removeOneClient(contex);
            contex.CloseAsync();

        }
        public override void ChannelInactive(IChannelHandlerContext context)
        {
            Console.WriteLine("ProxyServerHandler ChannelInactive");

            var ctssc = context.Channel as CustTcpSocketChannel;
            string ClientId;
            if (ctssc != null)
            {
                ClientId = ctssc.ChannelMata.tags["channelKey"].ToString();
               
                Console.WriteLine("Inactivate client CustTcpServerSocketChannel:{0}", ClientId);

            }
            else
            {
                throw new Exception("channel errror");
            }

        }
        private void removeOneClient(IChannelHandlerContext context)
        {
            string ClientId = "noId";
            var ctssc = context.Channel as CustTcpSocketChannel;
            if (ctssc != null)
            {

                if (ctssc.ChannelMata.tags.ContainsKey("channelKey"))
                {

                    if (ctssc.allclientchannel.ContainsKey(ClientId))
                    {
                        var clientchannel = ctssc.allclientchannel[ClientId] as CustTcpSocketChannel;

                        clientchannel.cleanData();
                    }
                }
            }
            else
            {
                throw new Exception("removeOneClient errror");
            }

        }
        public override void ChannelUnregistered(IChannelHandlerContext context)
        {
            removeOneClient(context);
        }
        public override bool IsSharable => true;
    }
}