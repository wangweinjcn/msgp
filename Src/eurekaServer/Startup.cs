using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.Swagger;

namespace eurekaServer
{
    public class nStartup : FrmLib.web.baseStartup
    {
        public static Dictionary<string, string> sdkReoteComs { get; set; }
        public nStartup(IHostingEnvironment env) : base(env)
        {
            swaggerDocTags = new List<Tag>()
            {
                new Tag { Name = "eureka", Description = "eureka兼容接口" },


            };
            swaggerXmlList = new List<string>()
            {
                "eurekaServer.xml"
            };
            sdkReoteComs = null;
        }


        protected override IList<string> swaggerXmlList { get; set; }
        protected override string appName { get { return "cbeLogisticsWeb"; } set => throw new NotImplementedException(); }
        protected override string deverName { get { return "wangwei"; } set {; } }
        public override void initOtherServices(IServiceCollection services)
        {
          //services.AddMvc().AddXmlSerializerFormatters();
            //  services.AddTimedJob();
        }
        public override void initOtherConfig(IApplicationBuilder app, IHostingEnvironment env)
        {
            //var jarrstr = Globals.Configuration["sdkApi:remoteTokens"];
            //JArray jarr = JArray.Parse(jarrstr);
            //foreach (JObject jobj in jarr)
            //{
            //    var tmpstr = jobj["comKey"].ToString();
            //    if (sdkReoteComs.ContainsKey(tmpstr))
            //        continue;
            //    sdkReoteComs.Add(tmpstr, jobj["secretKey"].ToString());
            //}
            //var str = Globals.Configuration["Task:Enable"];
            //bool enabletask = true;
            //bool.TryParse(str, out enabletask);
            //if (enabletask)
            //    app.UseTimedJob();
        }
    }

}
