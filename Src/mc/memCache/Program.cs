using DotNetty.Transport.Channels;
using msgp.common.dotnetty;
using System;
using DotNetty.Transport.Libuv;
using System.Security.Cryptography.X509Certificates;
using System.IO;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels.Sockets;
using DotNetty.Handlers.Logging;
using DotNetty.Handlers.Tls;
using DotNetty.Handlers.Timeout;
using msgp.mc.model;
using System.Threading.Tasks;
using DotNetty.Codecs;
using System.Runtime.InteropServices;
using System.Runtime;
using DotNetty.Codecs.Http;
using System.Net;

namespace msgp.mc.server
{
    class Program
    {


        static void Main(string[] args)
        {

           
            Console.WriteLine("Hello World!");
        }
    }
}
