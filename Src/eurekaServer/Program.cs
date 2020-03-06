using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FrmLib.web;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Proxy.Comm;

namespace eurekaServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            //CreateWebHostBuilder(args).Build().Run();
          
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance); /* 支持中文 */
            string contentRoot = Directory.GetCurrentDirectory();

            var hostBuilder = WebHost.CreateDefaultBuilder(args);
            var host = hostBuilder
                  .BindUrls(args, contentRoot)
                  .UseContentRoot(contentRoot)
                .UseStartup<nStartup>()
                 .UseKestrel(o => { o.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(30); })
                .Build();

            host.Run();
            // Server.start(-1,false, contentRoot);
        }

    }
}
