using DotNetty.Buffers;
using System;
using System.Collections.Generic;
using System.Text;

namespace Proxy.Comm.http
{
    public class ProxyHttpObject
    {
        public string url { get; set; }
        public int length { get; set; }

     public    string appKey { get; set; }
     public IByteBuffer databuffer;
        public ProxyHttpObject()
        {
            databuffer = Unpooled.Buffer();
           
        }
    }
}
