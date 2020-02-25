// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Proxy.Comm.socket
{
    using System;
    using System.Collections.Generic;
    using DotNetty.Buffers;
    using DotNetty.Transport.Channels;
    using DotNetty.Transport.Channels.Sockets;
    using Newtonsoft.Json;
    using proxyComm;

    public class ServerSocketClientHandler :SimpleChannelInboundHandler<object>
    {
        private  IChannelHandlerContext serverContext;
        
       

        public ServerSocketClientHandler(IChannelHandlerContext  serverContext):base()
        {
         
            this.serverContext = serverContext;
            
        }
        public override void ChannelActive(IChannelHandlerContext context) {
            Console.WriteLine("ServerSocketClientHandler  active");
        }

        
        public override void ChannelReadComplete(IChannelHandlerContext context) => context.Flush();
        public override void ExceptionCaught(IChannelHandlerContext contex, Exception e)
        {
            Console.WriteLine(DateTime.Now.Millisecond);
            Console.WriteLine("{0}", e.StackTrace);
            removeServerRef(contex);
            contex.CloseAsync();
        }

        protected override void ChannelRead0(IChannelHandlerContext ctx, object msg)
        {
            var ctssc = serverContext.Channel as CustTcpSocketChannel;
            if (ctssc == null || !ctssc.Active)
                throw new Exception("服务端链路失效");

            var clientChannel = ctx.Channel as CustTcpSocketChannel;
            if (clientChannel == null || !clientChannel.ChannelMata.tags.ContainsKey("channelKey"))
                return;
            var clientchannelkey = clientChannel.ChannelMata.tags["channelKey"].ToString();
            clientChannel.outMapPort.ins_serverProcessCount();
            var bb = (msg as IByteBuffer);           
            bb.Retain(); //计数加1
            var count = bb.ReadableBytes;
             object outobj = null;
            if (clientChannel.ChannelMata.tags.ContainsKey("readStart"))
            {
                DateTime end = DateTime.Now;              

                if (clientChannel.ChannelMata.tags.TryGetValue("readStart", out outobj) && outobj != null)
                { 
                  DateTime  begin= (DateTime)outobj;
                    clientChannel.outMapPort.add_msec_ServerProcess((long)(end - begin).TotalMilliseconds);
                }
                clientChannel.ChannelMata.tags.TryRemove("readStart" ,out outobj);
            }
            
            clientChannel.outMapPort.addSendBytes(bb.ReadableBytes);
            Console.WriteLine(clientChannel.outMapPort.toJson());
            serverContext.WriteAndFlushAsync(msg);
            
        }
        private void removeServerRef(IChannelHandlerContext context)
        {
            var ctsc = context.Channel as CustTcpSocketChannel;
            if (ctsc == null || !ctsc.ChannelMata.tags.ContainsKey("channelKey"))
                return;
           ctsc.outMapPort.delCount();
            var clientchannelkey = ctsc.ChannelMata.tags["channelKey"].ToString();

            var serverCtsc = serverContext.Channel as CustTcpSocketChannel;
            object tmp;
            if (serverCtsc == null)
            {
                
                ctsc.ChannelMata.tags.TryRemove("channelKey",out tmp);
                return;
            }
            if (serverCtsc.allclientchannel.ContainsKey(clientchannelkey))
                serverCtsc.allclientchannel.Remove(clientchannelkey);
            if (serverCtsc.bsp_dic.ContainsKey(clientchannelkey))
                serverCtsc.bsp_dic.Remove(clientchannelkey);
            if (serverCtsc.allclientCounter.ContainsKey(clientchannelkey))
                serverCtsc.allclientCounter.Remove(clientchannelkey);
            ctsc.ChannelMata.tags.TryRemove("channelKey",out tmp);
            
        }
        public override void ChannelInactive(IChannelHandlerContext context)
        {
            removeServerRef(context);
        }
        public override void ChannelUnregistered(IChannelHandlerContext context)
        {
             removeServerRef(context);
        }
    }
}