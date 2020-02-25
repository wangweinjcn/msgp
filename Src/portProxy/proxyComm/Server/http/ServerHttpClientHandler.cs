// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Proxy.Comm.http
{
    using System;
    using System.Collections.Generic;
    using DotNetty.Buffers;
    using DotNetty.Transport.Channels;
    using DotNetty.Transport.Channels.Sockets;
    using Newtonsoft.Json;
    using proxyComm;

    public class ServerHttpClientHandler :SimpleChannelInboundHandler<object>
    {
        private  IChannelHandlerContext serverContext;
        
       

        public ServerHttpClientHandler(IChannelHandlerContext  serverContext):base()
        {
         
            this.serverContext = serverContext;
            
        }
        public override void ChannelActive(IChannelHandlerContext context) {
            Console.WriteLine("ServerHttpClientHandler now active channel");
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
            var ctssc = serverContext.Channel as CustHttpSocketChannel;
            if (ctssc == null || !ctssc.Active)
                throw new Exception("服务端链路失效");
            var clientChannel = ctx.Channel as CustHttpSocketChannel;
            if (clientChannel == null || !clientChannel.ChannelMata.tags.ContainsKey("channelKey"))
                return;
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
                    DateTime begin = (DateTime)outobj;
                    clientChannel.outMapPort.add_msec_ServerProcess((long)(end - begin).TotalMilliseconds);
                }
                clientChannel.ChannelMata.tags.TryRemove("readStart", out outobj);
            }

            clientChannel.outMapPort.addSendBytes(bb.ReadableBytes);
            Console.WriteLine(clientChannel.outMapPort.toJson());
            serverContext.WriteAndFlushAsync(msg);
        }
        private void removeServerRef(IChannelHandlerContext context)
        {
            if (context == null || serverContext == null || context.Channel == null || serverContext.Channel == null                )
                return;

            var ctsc = context.Channel as CustHttpSocketChannel;
            if ( !ctsc.ChannelMata.tags.ContainsKey("channelKey"))
                return;
            ctsc.outMapPort.delCount();
            var serverCtsc = serverContext.Channel as CustHttpSocketChannel;
            var clientchannelkey = ctsc.ChannelMata.tags["channelKey"].ToString();
            serverCtsc.removeOneClientChannel(clientchannelkey);
            object tmp;
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