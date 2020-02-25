// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NettyRPC
{
    using System;
    using System.Net;
    using System.Threading.Tasks;
    using DotNetty.Transport.Channels;
    using DotNetty.Transport.Channels.Sockets;

    public class FastServerHandler : SimpleChannelInboundHandler<string>
    {
        public override void ChannelRegistered(IChannelHandlerContext context)
        {
           string ClientId = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            base.ChannelRegistered(context);
            var type = context.Channel.GetType();
            var ctssc = context.Channel as CustTcpSocketChannel;
            if (ctssc != null)
            {
                Console.WriteLine("new client CustTcpServerSocketChannel:{0}",ClientId);
                ctssc.ChannelMata.tags.TryAdd("custId", ClientId);
            }
        }
        public override void ChannelActive(IChannelHandlerContext contex)
        {
           
            contex.WriteAsync(string.Format("Welcome to {0} !\r\n", Dns.GetHostName()));
            contex.WriteAndFlushAsync(string.Format("It is {0} now !\r\n", DateTime.Now));
        }

        protected override void ChannelRead0(IChannelHandlerContext contex, string msg)
        {
            // Generate and write a response.
            string ClientId = "noId";
            var ctssc = contex.Channel as CustTcpSocketChannel;
            if (ctssc != null)
            {
                Console.WriteLine(" CustTcpServerSocketChannel ");
                if (ctssc.ChannelMata.tags.ContainsKey("custId"))
                {
                   
                    ClientId = ctssc.ChannelMata.tags["custId"].ToString();
                    Console.WriteLine(" client have custId:{0}", ClientId);
                }
            }

            string response;
            bool close = false;
            if (string.IsNullOrEmpty(msg))
            {
                response = "Please type something.\r\n";
            }
            else if (string.Equals("bye", msg, StringComparison.OrdinalIgnoreCase))
            {
                response = "Have a good day!\r\n";
                close = true;
            }
            else
            {
                response = "Did you say '" + msg + "'?\r\n";
            }
            Console.WriteLine("serverClientId:{2} ,client:{0},msg:{1}",contex.Channel.Id, response,ClientId);
            Task wait_close = contex.WriteAndFlushAsync(response);
            if (close)
            {
                Task.WaitAll(wait_close);
                contex.CloseAsync();
            }
        }
       
        public override void ChannelReadComplete(IChannelHandlerContext contex)
        {
            contex.Flush();
        }

        public override void ExceptionCaught(IChannelHandlerContext contex, Exception e)
        {
            Console.WriteLine("{0}", e.StackTrace);
            contex.CloseAsync();
        }

        public override bool IsSharable => true;
    }
}