   #if (NETCORE || NETSTANDARD2_0 )
using Ace;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

//namespace Microsoft.AspNetCore.Hosting
namespace FrmLib.web
{
    public static class WebHostBuilderListenerExtension
    {
        public static IWebHostBuilder BindUrls(this IWebHostBuilder hostBuilder, string[] appLaunchArgs, string contentRoot)
        {
            /*
             * 1.从启动参数中传入要监听的 urls，格式如：dotnet run -urls http://localhost:5001;http://localhost:5002
             * 2.如果启动参数中未包含 urls 参数，则从 config/appsetting.json 配置中查找要监听的 urls,"urls":{}
             * 3.如果appsetings.json 找不到相应的参数配置，则查找config/hosts.json, 
             * ！如果通过上述方式都找不到 urls，则使用默认方式！
             */

            Console.WriteLine(string.Format("input start args: {0}", JsonHelper.Serialize(appLaunchArgs)));

            string[] urls = new string[1];
            List<string> inputArgs = appLaunchArgs == null ? new List<string>() : appLaunchArgs.ToList();
            int indexOf_urls = inputArgs.IndexOf("-urls");
            if (indexOf_urls != -1 && inputArgs.Count > indexOf_urls + 1)
            {
                urls = inputArgs[indexOf_urls + 1].Split(';');
            }
            else
            {
                bool userhostconfig = false;
                var mainconfigFile=Path.Combine(contentRoot, "configs", "appsettings.json");
                if (File.Exists(mainconfigFile))
                {
                    var hostingConfig = new ConfigurationBuilder()
                                .SetBasePath(contentRoot)
                                .AddJsonFile(mainconfigFile, true)
                                .Build();
                    string protocol=string.IsNullOrEmpty( hostingConfig["urls:protocol"])?"http":hostingConfig["urls:protocol"];
                    string host =string.IsNullOrEmpty( hostingConfig["urls:host"])?"*":hostingConfig["urls:host"];
                    string port=  hostingConfig["urls:port"];
                    if (string.IsNullOrEmpty(port))
                        userhostconfig = true;
                    else
                       urls.SetValue( string.Format("{0}://{1}:{2}", protocol, host, port),0);
                }
               if(userhostconfig)
                {
                    string hostingFile = Path.Combine(contentRoot, "configs", "hosting.json");
                    if (File.Exists(hostingFile))
                    {
                        Console.WriteLine(string.Format("hosting.json exists: {0}", hostingFile));

                        var hostingConfig = new ConfigurationBuilder()
                                    .SetBasePath(contentRoot)
                                    .AddJsonFile(hostingFile, true)
                                    .Build();
                        string urlsValue = hostingConfig["urls"];
                        if (string.IsNullOrEmpty(urlsValue) == false)
                        {
                            urls = urlsValue.Split(';');
                        }
                    }
                }
            }

            if (urls.Length >= 0)
            {
                hostBuilder = hostBuilder.UseUrls(urls);
            }

            return hostBuilder;
        }
        public static IWebHostBuilder BindUrls(this IWebHostBuilder hostBuilder,  string contentRoot=null,int port=-1,bool usehttps=false)
        {
            /*
             * 1.从启动参数中传入要监听的 urls，格式如：dotnet run -urls http://localhost:5001;http://localhost:5002
             * 2.如果启动参数中未包含 urls 参数，则从 config/hosting.json 配置中查找要监听的 urls
             * ！如果通过上述两个方式都找不到 urls，则使用默认方式！
             */

         
            string[] urls = new string[0];
          
            if (port>0)
            {
                var httpprotol = usehttps ? "https" : "http";
                urls[0] = httpprotol + "://{}:" + port.ToString();
            }
            else
            {
                if (string.IsNullOrEmpty(contentRoot))
                    contentRoot = AppDomain.CurrentDomain.BaseDirectory;
                string hostingFile = Path.Combine(contentRoot, "configs", "hosting.json");
                if (File.Exists(hostingFile))
                {
                    Console.WriteLine(string.Format("hosting.json exists: {0}", hostingFile));

                    var hostingConfig = new ConfigurationBuilder()
                                .SetBasePath(contentRoot)
                                .AddJsonFile(hostingFile, true)
                                .Build();
                    string urlsValue = hostingConfig["urls"];
                    if (string.IsNullOrEmpty(urlsValue) == false)
                    {
                        urls = urlsValue.Split(';');
                    }
                }
            }

            if (urls.Length >= 0)
            {
                hostBuilder = hostBuilder.UseUrls(urls);
            }

            return hostBuilder;
        }
    }
}
#endif