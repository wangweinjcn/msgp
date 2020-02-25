using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Collections;

namespace FrmLib.Extend
{
 public class Json2Xml
    {
        [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2612.0")]
        [System.SerializableAttribute()]
        [System.Xml.Serialization.XmlTypeAttribute(Namespace = "urn:ebay:apis:eBLBaseComponents")]
        public enum ItemSpecificSourceCodeType
        {

            /// <remarks/>
            ItemSpecific,

            /// <remarks/>
            Attribute,

            /// <remarks/>
            Product,

            /// <remarks/>
            CustomCode,
        }

        [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2612.0")]
        [System.SerializableAttribute()]
        [System.Diagnostics.DebuggerStepThroughAttribute()]
        [System.ComponentModel.DesignerCategoryAttribute("code")]
        [System.Xml.Serialization.XmlTypeAttribute(Namespace = "urn:ebay:apis:eBLBaseComponents")]
        public partial class NameValueListType
        {

            private string nameField;

            private string[] valueField;

            private ItemSpecificSourceCodeType sourceField;

            private bool sourceFieldSpecified;

            private System.Xml.XmlElement[] anyField;

            /// <remarks/>
            public string Name
            {
                get
                {
                    return this.nameField;
                }
                set
                {
                    this.nameField = value;
                }
            }

            /// <remarks/>
            [System.Xml.Serialization.XmlElementAttribute("Value")]
            public string[] Value
            {
                get
                {
                    return this.valueField;
                }
                set
                {
                    this.valueField = value;
                }
            }

            /// <remarks/>
            public ItemSpecificSourceCodeType Source
            {
                get
                {
                    return this.sourceField;
                }
                set
                {
                    this.sourceField = value;
                }
            }

            /// <remarks/>
            [System.Xml.Serialization.XmlIgnoreAttribute()]
            public bool SourceSpecified
            {
                get
                {
                    return this.sourceFieldSpecified;
                }
                set
                {
                    this.sourceFieldSpecified = value;
                }
            }

            /// <remarks/>
            [System.Xml.Serialization.XmlAnyElementAttribute()]
            public System.Xml.XmlElement[] Any
            {
                get
                {
                    return this.anyField;
                }
                set
                {
                    this.anyField = value;
                }
            }
        }

        public class CustomItemSpecifics
        {
            public NameValueListType[] ItemSpecifics;
        }

        public class Item
        {
            public string Name { get; set; }

            public string Value { get; set; }

            public string[] RecommValues { get; set; }

            //public void DataTable2Entity(Item item, DataRow dr) {
            //    item.Name=dr.
            //}
        }

        public static string ObjectToText(Object inObject, Type inType)
        {
            String XmlizedString = null;
            MemoryStream ms = new MemoryStream();
            XmlSerializer xs = new XmlSerializer(inType);
            XmlTextWriter xmlTextWriter = new XmlTextWriter(ms, Encoding.UTF8);
            xs.Serialize(xmlTextWriter, inObject);
            ms = (MemoryStream)xmlTextWriter.BaseStream;
            XmlizedString = UTF8ByteArrayToString(ms.ToArray());

            return XmlizedString;
        }

        private static String UTF8ByteArrayToString(Byte[] characters)
        {
            UTF8Encoding encoding = new UTF8Encoding();
            String constructedString = encoding.GetString(characters);

            if (constructedString.Length > 0)
            {
                constructedString = constructedString.Substring(1);
            }
            return (constructedString);
        }

        public static Object TextToObject(string inText, Type inType)
        {
            try
            {
                if (string.IsNullOrEmpty(inText))
                {
                    return null;
                }
                else
                {
                    XmlSerializer xs = new XmlSerializer(inType);
                    MemoryStream ms = new MemoryStream(StringToUTF8ByteArray(inText));
                    XmlTextWriter xmlTextWriter = new XmlTextWriter(ms, Encoding.UTF8);
                    return (Object)xs.Deserialize(ms);
                }
            }
            catch
            {
                return null;
            }
        }
        private static byte[] StringToUTF8ByteArray(string inText)
        {
            UTF8Encoding encoding = new UTF8Encoding();
            Byte[] byteArray = encoding.GetBytes(inText);

            return byteArray;
        }
    }
    public class Static_xmltools
  {
      #region"基本操作函数"
      /// <summary>  
      /// 得到程序工作目录  
      /// </summary>  
      /// <returns></returns>  
      private static string GetWorkDirectory()
      {
          try
          {
              return Path.GetDirectoryName(typeof(Static_xmltools).Assembly.Location);
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
      #endregion

  }
     public class XmlToJSONHelper
    {
        public static string XmlToJSON(XmlDocument xmlDoc)
        {
            StringBuilder sbJSON = new StringBuilder();
            sbJSON.Append("{ ");
            XmlToJSONnode(sbJSON, xmlDoc.DocumentElement, true);
            sbJSON.Append("}");
            return sbJSON.ToString();
        }

        public static void XmlToJSONnode(StringBuilder sbJSON, XmlElement node, bool showNodeName)
        {
            if (showNodeName)
                sbJSON.Append("\"" + SafeJSON(node.Name) + "\": ");
            sbJSON.Append("{");

            SortedList childNodeNames = new SortedList();

            if (node.Attributes != null)
                foreach (XmlAttribute attr in node.Attributes)
                    StoreChildNode(childNodeNames, attr.Name, attr.InnerText);

            foreach (XmlNode cnode in node.ChildNodes)
            {
                if (cnode is XmlText)
                    StoreChildNode(childNodeNames, "value", cnode.InnerText);
                else if (cnode is XmlElement)
                    StoreChildNode(childNodeNames, cnode.Name, cnode);
            }
            foreach (string childname in childNodeNames.Keys)
            {
                ArrayList alChild = (ArrayList)childNodeNames[childname];
                if (alChild.Count == 1)
                    OutputNode(childname, alChild[0], sbJSON, true);
                else
                {
                    sbJSON.Append(" \"" + SafeJSON(childname) + "\": [ ");
                    foreach (object Child in alChild)
                        OutputNode(childname, Child, sbJSON, false);
                    sbJSON.Remove(sbJSON.Length - 2, 2);
                    sbJSON.Append(" ], ");
                }
            }
            sbJSON.Remove(sbJSON.Length - 2, 2);
            sbJSON.Append(" }");
        }

        public static void StoreChildNode(SortedList childNodeNames, string nodeName, object nodeValue)
        {
            if (nodeValue is XmlElement)
            {
                XmlNode cnode = (XmlNode)nodeValue;
                if (cnode.Attributes.Count == 0)
                {
                    XmlNodeList children = cnode.ChildNodes;
                    if (children.Count == 0)
                        nodeValue = null;
                    else if (children.Count == 1 && (children[0] is XmlText))
                        nodeValue = ((XmlText)(children[0])).InnerText;
                }
            }
            object oValuesAL = childNodeNames[nodeName];
            ArrayList ValuesAL;
            if (oValuesAL == null)
            {
                ValuesAL = new ArrayList();
                childNodeNames[nodeName] = ValuesAL;
            }
            else
                ValuesAL = (ArrayList)oValuesAL;
            ValuesAL.Add(nodeValue);
        }

        public static void OutputNode(string childname, object alChild, StringBuilder sbJSON, bool showNodeName)
        {
            if (alChild == null)
            {
                if (showNodeName)
                    sbJSON.Append("\"" + SafeJSON(childname) + "\": ");
                sbJSON.Append("null");
            }
            else if (alChild is string)
            {
                if (showNodeName)
                    sbJSON.Append("\"" + SafeJSON(childname) + "\": ");
                string sChild = (string)alChild;
                sChild = sChild.Trim();
                sbJSON.Append("\"" + SafeJSON(sChild) + "\"");
            }
            else
                XmlToJSONnode(sbJSON, (XmlElement)alChild, showNodeName);
            sbJSON.Append(", ");
        }

        public static string SafeJSON(string sIn)
        {
            StringBuilder sbOut = new StringBuilder(sIn.Length);
            foreach (char ch in sIn)
            {
                if (char.IsControl(ch) || ch == '\'')
                {
                    int ich = (int)ch;
                    sbOut.Append(@"\u" + ich.ToString("x4"));
                    continue;
                }
                else if (ch == '\"' || ch == '\\' || ch == '/')
                { sbOut.Append('\\'); }

                sbOut.Append(ch);
            }
            return sbOut.ToString();
        }

        public static void StoreChildNode(IDictionary childNodeNames, string nodeName, object nodeValue)
        {
            ArrayList list2;
            if (nodeValue is XmlElement)
            {
                XmlNode node = (XmlNode)nodeValue;
                if (node.Attributes.Count == 0)
                {
                    XmlNodeList childNodes = node.ChildNodes;
                    if (childNodes.Count == 0)
                    {
                        nodeValue = null;

                    }
                    else if ((childNodes.Count == 1) && (childNodes[0] is XmlText))
                    {

                        nodeValue = childNodes[0].InnerText;

                    }

                }
            }
            object obj2 = childNodeNames[nodeName];
            if (obj2 == null)
            {
                list2 = new ArrayList();
                childNodeNames[nodeName] = list2;
            }
            else
            {
                list2 = (ArrayList)obj2;
            }
            list2.Add(nodeValue);
        }
    }
}
