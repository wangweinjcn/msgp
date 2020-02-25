using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Telnet.Client
{

   public class clientPackage
    {
        static long packId = 0L;
        public long pid;
        public string clientid;
        public string data;
        static string endPackage = "\r\n0\r\n\r\n";
        public static clientPackage create(string _data, string _cid)
        {
            clientPackage cp = new clientPackage();
            cp.pid = Interlocked.Increment(ref packId);
            cp.data = _data.Replace(endPackage, "\r\n0\r\n");
            cp.clientid = _cid;
            return cp;

        }
        public string toSendString()
        {
            var str = JsonConvert.SerializeObject(this);
            return str + endPackage;
             
        }
    }
}
