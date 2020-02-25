// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace msgp.mc.client
{
    using System;
    using System.Collections.Concurrent;
    using System.Threading.Tasks;
    using DotNetty.Transport.Channels;
    using msgp.mc.model;

    public class mcClientHandler : SimpleChannelInboundHandler<cmdMessage>
    {
        private mcClient client;
        public mcClientHandler(mcClient _client)
        {
            this.client = _client;
        }
        protected override void ChannelRead0(IChannelHandlerContext contex, cmdMessage msg)
        {
            try
            {
                client.ProcessPacketAsync(msg);
            }
            catch (Exception ex)
            {

            }
            finally
            { }
            
        }

        public override void ExceptionCaught(IChannelHandlerContext contex, Exception e)
        {
            Console.WriteLine(DateTime.Now.Millisecond);
            Console.WriteLine("{0}", e.StackTrace);
            contex.CloseAsync();
        }
    }
}