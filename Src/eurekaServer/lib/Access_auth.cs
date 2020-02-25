using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FrmLib.Interface;
using System.Collections;
using System.Web;
using System.Xml;
using Newtonsoft.Json.Linq;
using System.IO;

namespace FrmLib.Extend
{
    public delegate bool  IsValidTokenHandle(string token);
    public delegate bool IsValidTokenWithUserIdHandle(string userid, string token);

    /* xml模版

    <?xml version="1.0"?>
  <configuration>
    <!--
        首先根据controller和http取得action列表；
        如果controller和http都配成*，则所有url都可以请求；

        参数的名字在属性name上，参数的值在属性value上
        如果不需要判断参数，只需要设置一组 <param name="*"  value="*"></param>就可以；
        禁止设置name=*,value!=*;或者name!=* value=*;
        如果设置多组不为*--*的参数，则传入的routedata必须要都包括且符合才是符合要求的
        如果有重复name的param，只有最后一个有效；
        例如,请求url：/CmsContent/Grouplist/newsCchool/pictureCenter
        路由规则： "CmsContent/{action}/{groupkey}/{itemkey}/{id}",
        设置的配置是：
        <authitem roleid="testrole" controller="CmsContent" action="Grouplist" http="*">
        <param name="groupkey"  value="newschool"></param>
        <param name="itemkey"  value="item2"></param>
      </authitem>
      那么该请求的url在这条记录的匹配结果是false；

        -->
    <webmvc>
      <authitem roleid="*" controller="anonListdata" action="" http="*">

        <param name="*"  value="*"></param>
      </authitem>
      <authitem roleid="*" controller="test" action="" http="*">
        <param name="*"  value="*"></param>
      </authitem>

    </webmvc>
    <webapi>
      <authitem roleid="*" controller="anonListdata" action="" http="*">
        <param name="*"  value="*"></param>
      </authitem>


    </webapi>
  </configuration>

    */
    /// <summary>
    /// 基于主机地址的黑白名单判断
    /// </summary>
    public class AccessHost
    {
        private bool _isWhiteModel;
        private Hashtable HostList;
        public AccessHost(bool isWhiteModel)
        {
            this._isWhiteModel = isWhiteModel;
            this.HostList = new Hashtable();
        }
        public bool WhiteModel { get { return _isWhiteModel; } }
        public void addNewHost(string Host, string token = "no")
        {
            if (!this.HostList.Contains(Host))
            {
                this.HostList.Add(Host, token);
            }
        }
        public string getHostToken(string Host)
        {
            if (_isWhiteModel)
            {
                if (this.HostList.Contains(Host))
                    return this.HostList[Host].ToString();
                else
                    return null;
            }
            else
            { throw new Exception("NotImpl"); };
        }
        public bool canAccess(string Host)
        {
            bool containhost = this.HostList.Contains(Host);
            if (_isWhiteModel)
                return containhost;
            else
                return !containhost;


        }
    }

    public class param_info
    {
        private string _name;
        private string _value;
        public string name { get { return _name; } }
        public string value { get { return _value; } }
        public param_info(string name, string value)
        {
            this._name = name.ToLower().Trim();
            this._value = value.ToLower().Trim();
        }
        public bool containParam(string name)
        {
            return (_name == "*" || _name == name);
        }
        public void setValue(string value)
        {
            this._value = value;
        }
    }
    public class action_info
    {
        
        private string actionname;
        private string httpmethod;
        List<param_info> paramlist;
        public action_info(string action, string http)
        {
            this.actionname = action;
            this.httpmethod = http;
            this.paramlist = new List<param_info>();
        }
        public param_info addparm(string name, string value)
        {
            //禁止不同时使用name=* 和value=*
            if((name=="*" && value!="*")||
            ( name!="*" && value=="*"))
                return null;
            param_info pi;
            if (haveContainParam(name))
            {
                pi = getParamInfo(name);
            pi.setValue(value);
            }
            else
                pi = new param_info(name, value);
            this.paramlist.Add(pi);
            return pi;
         
        }
        private bool haveContainParam(string name)
        {
            bool iscontail = false;
            foreach (var obj in this.paramlist)
            {
                if (obj.containParam(name))
                {
                    iscontail = true;
                    break;
                }
            }
            return iscontail;
        }
        private param_info getParamInfo(string name)
        {
            foreach (var obj in this.paramlist)
            {
                if (obj.containParam(name))
                {
                    return obj;
                }
            }
            return null;
        }
        public bool containParamlist(string jsonParam)
        {
            if (haveContainParam("*"))
                return true;
            JObject jobjParam = Newtonsoft.Json.Linq.JObject.Parse(jsonParam);

            
            foreach (var obj in this.paramlist)
            {
                bool haskey = jobjParam.Properties().Any(p => p.Name == obj.name);
                if (!haskey)
                    return false;
                JToken  jobj= jobjParam[obj.name];
                if (obj.value != jobj.ToString().ToLower().Trim())
                    return false;
            }
            return true;
        }
        public bool containaction(string action)
        {
            return (actionname == action || actionname=="*");
        }
        public bool containhttpmethod(string http)
        {
            return (httpmethod == http || httpmethod=="*");
        }
        
    }
    public class controllacttionlist
    { 
    private string _controller;
    private string _httpmethod;
    List<action_info> actionlist;
    public Boolean containhttpmethod(string httpmethod)
    {
        foreach (action_info ai in actionlist)
        {
            if (ai.containhttpmethod(httpmethod))
                return true;
        }
        return false;
    }
       
    public Boolean containaction (string action)
    {
        foreach (action_info ai in actionlist)
        {
            if (ai.containaction(action))
                return true;
        }
        return false;
    }
    public bool contain(string action, string httpmethod,string jsonParam=null)
    {
       
        foreach (action_info ai in actionlist)
        {

            if (ai.containaction(action) && ai.containhttpmethod(httpmethod))
            {
                if (jsonParam != null)
                    return ai.containParamlist(jsonParam);
                else

                    return true; 
            
            }
        }
        return false;
    }
   public controllacttionlist(string controller)
    {
        this._controller = controller;
        this.actionlist = new List<action_info>();
    }
   public action_info add(string action, string httpmethod)
   {
       action_info obj = null;
       if (!contain(action, httpmethod))
       { 
       obj=new action_info(action,httpmethod);
       this.actionlist.Add(obj);    
       }
       return obj;
   }

    }

    public class BlockControllers

    {

        private Dictionary<string, controllacttionlist> controllerlist;


        /// <summary>
        /// 初始化一个块的所有controller（例如一个区域，或者一个设备）
        /// </summary>
        /// <param name="useBlack">true使用黑名单（默认）</param>
        public BlockControllers()
        {
            controllerlist = new Dictionary<string, controllacttionlist>();
            
        }
        
        private controllacttionlist contailcontroller(string controller)
        {
            if (controllerlist.ContainsKey(controller))
            {
                return controllerlist[controller];
            }
            else
                return null;
        }
        public bool contain(string controller, string action, string httpmethod,string jsonParam=null)
        {
            if (controllerlist.ContainsKey("*") && controllerlist["*"].contain(action,httpmethod))
                return true;
            if (controllerlist.ContainsKey(controller))
            {
                controllacttionlist actionlist = controllerlist[controller];
                return (actionlist.contain(action, httpmethod,jsonParam));
                   
            }
          return false;
        }
       public action_info  add(string controller, string action,string httpmethod)
        {
            if (contain(controller, action,httpmethod))
                return null;
            controllacttionlist objlist = this.contailcontroller(controller);
            if (objlist == null)
            {
                objlist = new controllacttionlist(controller);
                controllerlist.Add(controller, objlist);
            }
         return   objlist.add(action, httpmethod);
           
        }
    }
    public class roleBlockControllers
    {
        /// <summary>
        /// 基于角色的控制器块
        /// </summary>
        private Dictionary<string, BlockControllers> blocks;
        private IList<string> iplists;
        private bool isblack;

        public roleBlockControllers(bool useBlack=true)
        {
            iplists = new List<string>();
            isblack = useBlack;
            blocks = new Dictionary<string, BlockControllers>(StringComparer.CurrentCultureIgnoreCase);
        }
        public bool containRoleid(string roleid)
        {
            return (blocks.Keys.Contains(roleid, StringComparer.CurrentCultureIgnoreCase));
                
        }
        public BlockControllers GetBlockControllers(string roleid)
        {
            if (blocks.Keys.Contains(roleid, StringComparer.CurrentCultureIgnoreCase))
                return blocks[roleid];
            else
                return null;

        }
        public BlockControllers addNew(string roleid)
        {
            if (blocks.Keys.Contains(roleid, StringComparer.CurrentCultureIgnoreCase))
                return blocks[roleid];
            else
            {
                BlockControllers obj = new BlockControllers();
                blocks.Add(roleid, obj);
                return obj;
            }
        }
        public void setAccessModel(bool useblack)
        {
            isblack = useblack;
        }
        public void addIP(string ip)
        {

            if (!iplists.Contains(ip))
                iplists.Add(ip);

        }
        public bool allowIP(string ip)
        {
            if (isblack)
            {
                if (iplists.Contains(ip))
                    return false;
                else
                    return true;
            }
            else
            {
                if (iplists.Contains(ip))
                    return true;
                else
                    return false;
            }
        }

    }
    /// <summary>
    /// 对于用户访问列表，基于area的key分组
    /// 对于设备访问列表，基于设备的key进行分组
    /// 如果不使用key，默认key值为-1(字符串)
    /// 
    /// </summary>

    public class AllAreas_ht
    {
        private Dictionary<string, roleBlockControllers> allcontrollers = new Dictionary<string, roleBlockControllers>(StringComparer.OrdinalIgnoreCase);

        public roleBlockControllers getRoleBlockControllers(string areakey="-1")
        {
            if (allcontrollers.Keys.Contains(areakey, StringComparer.CurrentCultureIgnoreCase))
                return allcontrollers[areakey];
            else
                return null;
        }
        public roleBlockControllers addNew(string areakey)
        {
            if (allcontrollers.Keys.Contains(areakey, StringComparer.CurrentCultureIgnoreCase))
                return allcontrollers[areakey];
            else
            {
                roleBlockControllers rbc = new roleBlockControllers();
                allcontrollers.Add(areakey, rbc);
                return rbc;
            }

        }
    }
    public enum enum_webtype
    {
       webmvc,webapi,webform,nwSocket
    }
    /// <summary>
    /// 对于设备还是用户的判定，对于http，基于请求头的DeviceToken,
    /// </summary>
   public class baseAccess_auth:IAuthorizefilter
    {
        private const string devicePrefix = "Device_";
        private const string userPrefix = "User_";
        /// <summary>
        /// 基于设备（deviceApi）还是基于用户（userApi）的判断
        /// </summary>
        private string _itemName=""; 
        //xml中权限数据的xmlitempath
       
        private AllAreas_ht Access_ht = new AllAreas_ht();
        private AccessHost accessHost;
        private bool _defaultallow = false;
     
        public bool defaultallow { get { return _defaultallow; } set { _defaultallow = value; } }

        private string _confFullFileName="access_auth.config";
        private IsValidTokenHandle _validTokenFunc;
        private IsValidTokenWithUserIdHandle _validTokenWithUserIdFunc;
        public baseAccess_auth(string conffileFullName, bool defaultallow, IsValidTokenHandle tokenvalidHandle, IsValidTokenWithUserIdHandle tokenWithUidHandle)
        {
            _defaultallow = defaultallow;
            _validTokenFunc = tokenvalidHandle;
            _validTokenWithUserIdFunc = tokenWithUidHandle;
            if (string.IsNullOrEmpty(conffileFullName))
            {

                string rootdir;
                rootdir = System.AppDomain.CurrentDomain.BaseDirectory;

                string xmlfilename = System.IO.Path.Combine(rootdir, _confFullFileName);

                _confFullFileName = xmlfilename;
            }
            else
                _confFullFileName = conffileFullName;

            fillcanaccess_ht();
        }
        public baseAccess_auth(string conffileFullName, bool defaultallow, IsValidTokenWithUserIdHandle tokenvalidHandle = null) : this(conffileFullName, defaultallow, null, tokenvalidHandle)
        {



        }
        public baseAccess_auth(string conffileFullName, bool defaultallow, IsValidTokenHandle tokenvalidHandle=null):this(conffileFullName,defaultallow,tokenvalidHandle,null)
        {
            

            
        }


        public bool IsAllowed(string roleidlist, string str_controller, string str_action,
            string httpmethod,string jsonParam, string areacode = "-1", bool isdevice = false)
        {
            string prefix = null;
            if (isdevice)
                prefix = devicePrefix;
            else
                prefix = userPrefix;
            string areakey = prefix + areacode;

            var roleBlockControllers = Access_ht.getRoleBlockControllers(areakey);
            if (roleBlockControllers == null)
                return false;
            string[] rolelist = roleidlist.Split(',');
            foreach (string str_roleid in rolelist)
            {
                if (string.IsNullOrEmpty(str_roleid))
                    continue;
                if (roleBlockControllers.containRoleid(str_roleid))
                {
                    BlockControllers objcontrollerht = roleBlockControllers.GetBlockControllers(str_roleid);
                    if (objcontrollerht.contain(str_controller, str_action, httpmethod,jsonParam))
                        return true;
                }
             }
            return this._defaultallow;
            
        }
        public virtual bool Isvalidtoken(string str_token)
       {
            if (_validTokenFunc == null)
                return true;
           return _validTokenFunc(str_token);
       }
        public virtual bool Isvalidtoken(string userid,string str_token)
        {
            if (_validTokenWithUserIdFunc == null)
                return true;
            return _validTokenWithUserIdFunc(userid,str_token);
        }
        public bool IsHostAllowed(string remoteHost, string protocal)
        {
            if(!string.Equals("http",protocal,StringComparison.CurrentCultureIgnoreCase))
                 throw new Exception("NotImpl");
            return this.accessHost.canAccess(remoteHost);
        }
      public bool AnonymousAllowed(string controllername, string actionname, 
          string httpmethod,string jsonParam, string areacode = "-1", bool isdevice = false)
    {
            string prefix = null;
            if (isdevice)
                prefix = devicePrefix;
            else
                prefix = userPrefix;
            string areakey = prefix + areacode;

            var roleBlockControllers = Access_ht.getRoleBlockControllers(areakey);
            if (roleBlockControllers == null)
                return _defaultallow;
            BlockControllers objcontrollerht=null;
          if (roleBlockControllers.containRoleid("*"))
              objcontrollerht = roleBlockControllers.GetBlockControllers("*");
         if (objcontrollerht != null && objcontrollerht.contain(controllername, actionname, httpmethod))
                return true;
            else
            {
                

                return this._defaultallow;
            }
    }
        private void processOneBlockCotrollers( XmlNodeList alllist,string areaPrefix)
        {
            string xpathstr2 = "./authitem";
            string xpathstr3 = "./Access";
            roleBlockControllers roleBControllers;
            foreach (XmlNode node in alllist)
            {
                string areaKey = xmlTools.GetXmlAttr(node, "key").ToLower();
                if (string.IsNullOrEmpty(areaKey))
                    areaKey = "-1";
                areaKey = areaPrefix + areaKey;
                roleBControllers = Access_ht.addNew(areaKey);
              var  alllist2 = xmlTools.SelectXmlNodes(node, xpathstr2);
                //processOneBlockCotrollers(roleBControllers, alllist2);

                foreach (XmlNode x in alllist2)
                {
                    string strRoleIdList = xmlTools.GetXmlAttr(x, "roleid").ToLower();
                    if (string.IsNullOrEmpty(strRoleIdList))
                        continue;
                    string str_controller = xmlTools.GetXmlAttr(x, "controller").ToLower();
                    if (string.IsNullOrEmpty(str_controller))
                        continue;
                    string str_action = xmlTools.GetXmlAttr(x, "action").ToLower();
                    string httpmethod = xmlTools.GetXmlAttr(x, "http").ToLower();
                    XmlNodeList paramlist = x.SelectNodes("param");
                    if (string.IsNullOrEmpty(str_action))
                        str_action = "*";
                   
                    var allroleid = strRoleIdList.Split(',');
                    foreach (var strRoleId in allroleid)
                    {
                        action_info currActionInfo = null;
                        if (roleBControllers.containRoleid(strRoleId))
                        {
                            BlockControllers obj = roleBControllers.GetBlockControllers(strRoleId);
                            if (obj.contain(str_controller, str_action, httpmethod))
                                continue;
                            else
                            {
                                currActionInfo = obj.add(str_controller, str_action, httpmethod);

                            }
                        }
                        else
                        {
                            BlockControllers obj = roleBControllers.addNew(strRoleId);
                            currActionInfo = obj.add(str_controller, str_action, httpmethod);

                        }
                        foreach (XmlNode paramNode in paramlist)
                        {
                            currActionInfo.addparm(xmlTools.GetXmlAttr(paramNode, "name"), xmlTools.GetXmlAttr(paramNode, "value"));

                        }
                    }
                }
                var ipacccessNode = xmlTools.SelectXmlNode(node, xpathstr3);
                string accessmode = xmlTools.GetXmlAttr(ipacccessNode, "Model").ToLower();
                if (string.IsNullOrEmpty(accessmode))
                    continue;
                roleBControllers.setAccessModel(string.Equals("black", accessmode));
                var alllist3 = xmlTools.SelectXmlNodes(ipacccessNode, "./ip");
                foreach(XmlNode ipnode in alllist3)
                {
                    roleBControllers.addIP(ipnode.Value);

                }
            }
           
        }
        private void fillcanaccess_ht()
        {

            if (!System.IO.File.Exists(_confFullFileName))
                return;
            XmlDocument tmpxmldoc = xmlTools.GetXmlDocument(_confFullFileName);
            XmlNodeList alllist = null ;
            //处理userapi的area

            string xpathstr = "/configuration/userApi/area";
            
            alllist = xmlTools.SelectXmlNodes(tmpxmldoc, xpathstr);
            processOneBlockCotrollers(alllist,userPrefix);
            xpathstr = "/configuration/deviceApi";
            alllist = xmlTools.SelectXmlNodes(tmpxmldoc, xpathstr);
            processOneBlockCotrollers(alllist,devicePrefix);

           

        }
    }
    internal class xmlTools
    {
       
        /// <summary>  
        /// 得到程序工作目录  
        /// </summary>  
        /// <returns></returns>  
        private static string GetWorkDirectory()
        {
            try
            {
                return Path.GetDirectoryName(typeof(xmlTools).Assembly.Location);
            }
            catch
            {
                return "";
            }
        }
        /// <summary>  
        /// 判断字符串是否为空串  
        /// </summary>  
        /// <param name="szString">目标字符串</param>  
        /// <returns>true:为空串;false:非空串</returns>  
        private static bool IsEmptyString(string szString)
        {
            if (szString == null)
                return true;
            if (szString.Trim() == string.Empty)
                return true;
            return false;
        }
        /// <summary>  
        /// 创建一个制定根节点名的XML文件  
        /// </summary>  
        /// <param name="szFileName">XML文件</param>  
        /// <param name="szRootName">根节点名</param>  
        /// <returns>bool</returns>  
        public static bool CreateXmlFile(string szFileName, string szRootName)
        {
            if (szFileName == null || szFileName.Trim() == "")
                return false;
            if (szRootName == null || szRootName.Trim() == "")
                return false;

            XmlDocument clsXmlDoc = new XmlDocument();
            clsXmlDoc.AppendChild(clsXmlDoc.CreateXmlDeclaration("1.0", "GBK", null));
            clsXmlDoc.AppendChild(clsXmlDoc.CreateNode(XmlNodeType.Element, szRootName, ""));
            try
            {
                clsXmlDoc.Save(szFileName);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>  
        /// 从XML文件获取对应的XML文档对象  
        /// </summary>  
        /// <param name="szXmlFile">XML文件</param>  
        /// <returns>XML文档对象</returns>  
        public static XmlDocument GetXmlDocument(string szXmlFile)
        {
            if (IsEmptyString(szXmlFile))
                return null;
            if (!File.Exists(szXmlFile))
                return null;
            XmlDocument clsXmlDoc = new XmlDocument();
            try
            {
                clsXmlDoc.Load(szXmlFile);
            }
            catch
            {
                return null;
            }
            return clsXmlDoc;
        }

        /// <summary>  
        /// 将XML文档对象保存为XML文件  
        /// </summary>  
        /// <param name="clsXmlDoc">XML文档对象</param>  
        /// <param name="szXmlFile">XML文件</param>  
        /// <returns>bool:保存结果</returns>  
        public static bool SaveXmlDocument(XmlDocument clsXmlDoc, string szXmlFile)
        {
            if (clsXmlDoc == null)
                return false;
            if (IsEmptyString(szXmlFile))
                return false;
            try
            {
                if (File.Exists(szXmlFile))
                    File.Delete(szXmlFile);
            }
            catch
            {
                return false;
            }
            try
            {
                clsXmlDoc.Save(szXmlFile);
            }
            catch
            {
                return false;
            }
            return true;
        }

        /// <summary>  
        /// 获取XPath指向的单一XML节点  
        /// </summary>  
        /// <param name="clsRootNode">XPath所在的根节点</param>  
        /// <param name="szXPath">XPath表达式</param>  
        /// <returns>XmlNode</returns>  
        public static XmlNode SelectXmlNode(XmlNode clsRootNode, string szXPath)
        {
            if (clsRootNode == null || IsEmptyString(szXPath))
                return null;
            try
            {
                return clsRootNode.SelectSingleNode(szXPath);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>  
        /// 获取XPath指向的XML节点集  
        /// </summary>  
        /// <param name="clsRootNode">XPath所在的根节点</param>  
        /// <param name="szXPath">XPath表达式</param>  
        /// <returns>XmlNodeList</returns>  
        public static XmlNodeList SelectXmlNodes(XmlNode clsRootNode, string szXPath)
        {
            if (clsRootNode == null || IsEmptyString(szXPath))
                return null;
            try
            {
                return clsRootNode.SelectNodes(szXPath);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>  
        /// 创建一个XmlNode并添加到文档  
        /// </summary>  
        /// <param name="clsParentNode">父节点</param>  
        /// <param name="szNodeName">结点名称</param>  
        /// <returns>XmlNode</returns>  
        private static XmlNode CreateXmlNode(XmlNode clsParentNode, string szNodeName)
        {
            try
            {
                XmlDocument clsXmlDoc = null;
                if (clsParentNode.GetType() != typeof(XmlDocument))
                    clsXmlDoc = clsParentNode.OwnerDocument;
                else
                    clsXmlDoc = clsParentNode as XmlDocument;
                XmlNode clsXmlNode = clsXmlDoc.CreateNode(XmlNodeType.Element, szNodeName, string.Empty);
                if (clsParentNode.GetType() == typeof(XmlDocument))
                {
                    clsXmlDoc.LastChild.AppendChild(clsXmlNode);
                }
                else
                {
                    clsParentNode.AppendChild(clsXmlNode);
                }
                return clsXmlNode;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>  
        /// 设置指定节点中指定属性的值  
        /// </summary>  
        /// <param name="parentNode">XML节点</param>  
        /// <param name="szAttrName">属性名</param>  
        /// <param name="szAttrValue">属性值</param>  
        /// <returns>bool</returns>  
        private static bool SetXmlAttr(XmlNode clsXmlNode, string szAttrName, string szAttrValue)
        {
            if (clsXmlNode == null)
                return false;
            if (IsEmptyString(szAttrName))
                return false;
            if (IsEmptyString(szAttrValue))
                szAttrValue = string.Empty;
            XmlAttribute clsAttrNode = clsXmlNode.Attributes.GetNamedItem(szAttrName) as XmlAttribute;
            if (clsAttrNode == null)
            {
                XmlDocument clsXmlDoc = clsXmlNode.OwnerDocument;
                if (clsXmlDoc == null)
                    return false;
                clsAttrNode = clsXmlDoc.CreateAttribute(szAttrName);
                clsXmlNode.Attributes.Append(clsAttrNode);
            }
            clsAttrNode.Value = szAttrValue;
            return true;
        }

        /// <summary>  
        /// 获取指定节点中指定属性的值  
        /// </summary>  
        /// <param name="parentNode">XML节点</param>  
        /// <param name="szAttrName">属性名</param>  
        /// <returns>bool</returns>  
        public static string GetXmlAttr(XmlNode clsXmlNode, string szAttrName)
        {
            if (clsXmlNode == null)
                return "";
            if (IsEmptyString(szAttrName))
                return "";

            XmlAttribute clsAttrNode = clsXmlNode.Attributes.GetNamedItem(szAttrName) as XmlAttribute;
            if (clsAttrNode == null)
            {
                return "";
            }
            else
                return clsAttrNode.Value;

        }
        public static void RemoveXmlNode(XmlNode xn)
        {
            if (xn.ParentNode != null)
                xn.ParentNode.RemoveChild(xn);
        }
        public static void AppendNewNode(XmlNode parent, XmlNode newnode)
        {
            if (parent != null)
                parent.AppendChild(newnode);
        }
        public static XmlDocument LoadXmlFromString(string str)
        {
            XmlDocument xml = new XmlDocument();
            xml.LoadXml(str);
            return xml;
        }


    }
}


