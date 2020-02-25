// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Telnet.Client
{
    using System;
    using DotNetty.Buffers;
    using DotNetty.Transport.Channels;
    using Newtonsoft.Json;
    using proxyComm;

    public class TelnetClientHandler :ChannelHandlerAdapter
    {
        public override void ChannelActive(IChannelHandlerContext context) {
            Console.WriteLine("now active");
        }
        public override void ChannelRead(IChannelHandlerContext context, object message)
        {
            var msg = message as IByteBuffer;
            var rtask = Program.requestTask;
            string str = msg.GetString(0, msg.ReadableBytes, System.Text.Encoding.Default);
            var pack = JsonConvert.DeserializeObject<testpackage>(str);
            var ts = rtask[pack.id];
            ts.SetResult(pack);
        }
        
        public override void ChannelReadComplete(IChannelHandlerContext context) => context.Flush();
        public override void ExceptionCaught(IChannelHandlerContext contex, Exception e)
        {
            Console.WriteLine(DateTime.Now.Millisecond);
            Console.WriteLine("{0}", e.StackTrace);
            contex.CloseAsync();
        }
    }
}