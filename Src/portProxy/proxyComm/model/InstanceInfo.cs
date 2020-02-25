using FrmLib.Extend;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace Proxy.Comm.model
{
    [XmlRoot("instance")]
    public class InstanceInfo
    {
        [XmlIgnore]
        [JsonIgnore]
        private outMapPort _outmap;
        [XmlIgnore]
        [JsonIgnore]
        public outMapPort outMap { get { return _outmap; } set { _outmap = value;_outmap._ins = this; } }
         [XmlIgnore] 
        public static  int DEFAULT_PORT = 7001;
         [XmlIgnore] 
        public static  int DEFAULT_SECURE_PORT = 7002;
         [XmlIgnore] 
        public static  int DEFAULT_COUNTRY_ID = 1; // US
         [XmlIgnore] 
        private static  String VERSION_UNKNOWN = "unknown";
          [XmlIgnore] 
        public static String SID_DEFAULT = "na";

        // The (fixed) instanceId for this instanceInfo. This should be unique within the scope of the appName.
        public String instanceId;

        public String appName;

        public String appGroupName;

        public String ipAddr;

      
        public String sid = SID_DEFAULT;

        public int port = DEFAULT_PORT;
        public int securePort = DEFAULT_SECURE_PORT;


        public String homePageUrl;

        public String statusPageUrl;

        public String healthCheckUrl;

        public String secureHealthCheckUrl;

        public String vipAddress;

        public String secureVipAddress;

        public String statusPageRelativeUrl;

        public String statusPageExplicitUrl;

        public String healthCheckRelativeUrl;

        public String healthCheckSecureExplicitUrl;

        public String vipAddressUnresolved;

        public String secureVipAddressUnresolved;

        public String healthCheckExplicitUrl;

        public int countryId = DEFAULT_COUNTRY_ID; // Defaults to US
        public bool isSecurePortEnabled = false;
        public bool isUnsecurePortEnabled = true;
        [XmlIgnore] 
        public DataCenterInfo dataCenterInfo;
        public string hostName;
        public InstanceStatus status = InstanceStatus.UP;
        public InstanceStatus overriddenStatus = InstanceStatus.UNKNOWN;

        public bool isInstanceInfoDirty = false;
        public LeaseInfo leaseInfo;

        public bool isCoordinatingDiscoveryServer = false;
        [XmlIgnore] 
        [JsonIgnore]
        public Dictionary<string,string> metadata;

        public long lastUpdatedTimestamp;

        public long lastDirtyTimestamp;

        public ActionType actionType;

        public String asgName;
        public String version = VERSION_UNKNOWN;

        public InstanceInfo()
        {
            metadata = new Dictionary<string, string>();
        }
        public static InstanceInfo fromJson(Stream stream)
        {
            string body;
            stream.Seek(0, SeekOrigin.Begin);
            using (var reader = new StreamReader(stream))
            {

                body = AsyncHelpers.RunSync<string>(() => reader.ReadToEndAsync());

            };
            var ins = JsonConvert.DeserializeObject<InstanceInfo>(body);
            var jobj = JObject.Parse(body);
            if (FrmLib.Extend.tools_static.jobjectHaveKey(jobj, "metadata"))
            {
                //待确定json格式
            }
            return ins;
        }
        public static InstanceInfo fromXml(Stream stream)
        {
            string body;
            stream.Seek(0, SeekOrigin.Begin);
            using (var reader = new StreamReader(stream))
            {

                body = AsyncHelpers.RunSync<string>(() => reader.ReadToEndAsync());

            };
              var xmldoc = FrmLib.Extend.Static_xmltools.LoadXmlFromString(body);
           // stream.Seek(0, SeekOrigin.Begin);
            XmlSerializer xmlserilize = new XmlSerializer(typeof(InstanceInfo));
            XmlReader xreader = XmlReader.Create((new StringReader(body)));
            var oneIns = (InstanceInfo)xmlserilize.Deserialize(xreader);
            

          
            oneIns.dataCenterInfo.name = Static_xmltools.SelectXmlNode(xmldoc, "//dataCenterInfo/name").InnerText;
            var nodelist = Static_xmltools.SelectXmlNodes(xmldoc, "//metadata/*");
            foreach (XmlNode node in nodelist)
            {

                oneIns.metadata.Add(node.Name, node.InnerText);
            }
            return oneIns;
        }
        public string toJson()
        {
            JObject jobj = JObject.FromObject(this);
            foreach (var one in this.metadata)
            { }
            return jobj.ToString() ;
        }
        public string toxml()
        {
            XmlDocument doc;
            using (MemoryStream Stream = new MemoryStream())
            {
                XmlSerializer xml = new XmlSerializer(this.GetType());
                //序列化对象
                xml.Serialize(Stream, this);
                Stream.Position = 0;
                StreamReader sr = new StreamReader(Stream);
                string str = sr.ReadToEnd();
                doc = FrmLib.Extend.Static_xmltools.LoadXmlFromString(str);
            }
            XmlNode node = doc.CreateElement("dataCenterInfo");
            doc.DocumentElement.AppendChild(node);
            XmlNode newnode = doc.CreateElement("name");
            newnode.InnerText = this.dataCenterInfo.name;
            node.AppendChild(newnode);
            node = doc.CreateElement("metadata");
            doc.DocumentElement.AppendChild(node);
            foreach (var md in this.metadata)
            {
                newnode = doc.CreateElement(md.Key);
                newnode.InnerText = md.Value;
                node.AppendChild(newnode);
            }
            return doc.InnerXml;
        }
    }

    

    public enum InstanceStatus
    {
        UP, // Ready to receive traffic
        DOWN, // Do not send traffic- healthcheck callback failed
        STARTING, // Just about starting- initializations to be done - do not
        // send traffic
        OUT_OF_SERVICE, // Intentionally shutdown for traffic
        UNKNOWN

       
}
    public enum ActionType
    {
        ADDED, // Added in the discovery server
        MODIFIED, // Changed in the discovery server
        DELETED
        // Deleted from the discovery server
    }
    public class LeaseInfo
    {

        public static  int DEFAULT_LEASE_RENEWAL_INTERVAL = 30;
        public static  int DEFAULT_LEASE_DURATION = 90;

        // Client settings
        private int renewalIntervalInSecs = DEFAULT_LEASE_RENEWAL_INTERVAL;
        private int durationInSecs = DEFAULT_LEASE_DURATION;

        // Server populated
        private long registrationTimestamp;
        private long lastRenewalTimestamp;
        private long evictionTimestamp;
        private long serviceUpTimestamp;
    }
    public class DataCenterInfo
    {
        public string name;
        public string className;
    }
}
