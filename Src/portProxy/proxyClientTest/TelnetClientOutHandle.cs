using DotNetty.Transport.Channels;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Threading;

namespace Telnet.Client
{
    public class TelnetClientOutHandle<I> : ChannelHandlerAdapter
    {
        static long packId = 0L;
        public bool AcceptInboundMessage(object msg) => msg is I;
        public override Task WriteAsync(IChannelHandlerContext context, object message)
        {
            JObject  jobj= new JObject();
            var pid = Interlocked.Increment(ref packId);
            jobj.Add("Id", Program.ClientId);
            jobj.Add("packetId", pid);
            jobj.Add("data", message.ToString());
            Console.WriteLine("write obj:{0}",jobj.ToString());
            return base.WriteAsync(context, jobj.ToString()+"\r\n0\r\n");
        }
    }
}
