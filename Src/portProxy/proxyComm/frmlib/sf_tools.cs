using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Principal;
using System.Web;
using System.Globalization;
using System.Security.Cryptography;
using System.IO;
using System.Configuration;
using Newtonsoft.Json;
using System.Xml;
using System.Net.Http;
using System.Linq.Expressions;
using System.Drawing;
using System.Reflection;
using System.ComponentModel;
using System.Collections.Specialized;

using Newtonsoft.Json.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FrmLib.Extend
{
    public static class AsyncHelpers
    {
        /// <summary>
        /// Execute's an async Task<T> method which has a void return value synchronously
        /// </summary>
        /// <param name="task">Task<T> method to execute</param>
        public static void RunSync(Func<Task> task)
        {
            var oldContext = SynchronizationContext.Current;
            var synch = new ExclusiveSynchronizationContext();
            SynchronizationContext.SetSynchronizationContext(synch);
            synch.Post(async _ =>
            {
                try
                {
                    await task();
                }
                catch (Exception e)
                {
                    synch.InnerException = e;
                    throw;
                }
                finally
                {
                    synch.EndMessageLoop();
                }
            }, null);
            synch.BeginMessageLoop();

            SynchronizationContext.SetSynchronizationContext(oldContext);
        }

        /// <summary>
        /// Execute's an async Task<T> method which has a T return type synchronously
        /// </summary>
        /// <typeparam name="T">Return Type</typeparam>
        /// <param name="task">Task<T> method to execute</param>
        /// <returns></returns>
        public static T RunSync<T>(Func<Task<T>> task)
        {
            var oldContext = SynchronizationContext.Current;
            var synch = new ExclusiveSynchronizationContext();
            SynchronizationContext.SetSynchronizationContext(synch);
            T ret = default(T);
            synch.Post(async _ =>
            {
                try
                {
                    ret = await task();
                }
                catch (Exception e)
                {
                    synch.InnerException = e;
                    throw;
                }
                finally
                {
                    synch.EndMessageLoop();
                }
            }, null);
            synch.BeginMessageLoop();
            SynchronizationContext.SetSynchronizationContext(oldContext);
            return ret;
        }

        private class ExclusiveSynchronizationContext : SynchronizationContext
        {
            private bool done;
            public Exception InnerException { get; set; }
            readonly AutoResetEvent workItemsWaiting = new AutoResetEvent(false);
            readonly Queue<Tuple<SendOrPostCallback, object>> items =
                new Queue<Tuple<SendOrPostCallback, object>>();

            public override void Send(SendOrPostCallback d, object state)
            {
                throw new NotSupportedException("We cannot send to our same thread");
            }

            public override void Post(SendOrPostCallback d, object state)
            {
                lock (items)
                {
                    items.Enqueue(Tuple.Create(d, state));
                }
                workItemsWaiting.Set();
            }

            public void EndMessageLoop()
            {
                Post(_ => done = true, null);
            }

            public void BeginMessageLoop()
            {
                while (!done)
                {
                    Tuple<SendOrPostCallback, object> task = null;
                    lock (items)
                    {
                        if (items.Count > 0)
                        {
                            task = items.Dequeue();
                        }
                    }
                    if (task != null)
                    {
                        task.Item1(task.Item2);
                        if (InnerException != null) // the method threw an exeption
                        {
                            throw new AggregateException("AsyncHelpers.Run method threw an exception.", InnerException);
                        }
                    }
                    else
                    {
                        workItemsWaiting.WaitOne();
                    }
                }
            }

            public override SynchronizationContext CreateCopy()
            {
                return this;
            }
        }
    }

    /// <summary>
    /// CID 的摘要说明。
    /// </summary>
    public class IdentityCard
    {
        private string cid_;
        private string errmsg;
        private string[] aCity;
        private int sex;
        private DateTime birth;
        public IdentityCard(string CardID)
        {
            CID = CardID;
            aCity = new string[]{null,null,null,null,null,null,null,null,null,null,null,
         "北京","天津 ","河北","山西","内蒙古",
         null,null,null,null,null,
         "辽宁","吉林","黑龙江",
         null,null, null,null,null,null,null,
         "上海","江苏","浙江","安微","福建","江西","山东",
         null,null, null,
         "河南","湖北","湖南","广东","广西","海南",
         null,null,null,
         "重庆","四川","贵州","云南","西藏",
         null,null,null,null,null,null,
         "陕西","甘肃","青海","宁夏","新疆",
         null,null, null,null,null,
         "台湾",
         null,null,null,null,null,null,null,null,null,
         "香港","澳门",
         null,null,null,null,null,null,null,null,
         "国外"};
        }

        

        public bool Check()
        {
            //判断位数
            if (CID.Length == 15)
            {
                return Check15();
            }
            else if (CID.Length == 18)
            {
                return Check18(CID);
            }
            else
            {
                ErrMsg = "身份证位数不正确";
                return false;
            }
        }
        public DateTime gerBirthday()
        {
            if (Check())
                return birth;
            else
                return DateTime.MinValue;
        }
        public int getSex()
        {
            if (Check())
                return sex;
            else
                return -1;

        }

        public bool Check18(string cid)
        {
            double iSum = 0;
            //string info="";
            System.Text.RegularExpressions.Regex rg = new System.Text.RegularExpressions.Regex(@"^\d{17}(\d|x)$");
            System.Text.RegularExpressions.Match mc = rg.Match(cid);
            if (!mc.Success)
            {
                ErrMsg = "身份证输入错误";
                return false;
            }
            cid = cid.ToLower();
            cid = cid.Replace("x", "a");
            if (aCity[int.Parse(cid.Substring(0, 2))] == null)
            {
                ErrMsg = "非法地区";
                return false;
            }
            try
            {
              birth=  DateTime.Parse(cid.Substring(6, 4) + "-" + cid.Substring(10, 2) + "-" + cid.Substring(12, 2));
            }
            catch
            {
                ErrMsg = "非法生日";
                return false;
            }
            for (int i = 17; i >= 0; i--)
            {
                iSum += (System.Math.Pow(2, i) % 11) * int.Parse(cid[17 - i].ToString(), System.Globalization.NumberStyles.HexNumber);

            }
            if (iSum % 11 != 1)
            {
                ErrMsg = "非法证号";
                return false;
            }

            int s = int.Parse(cid.Substring(16, 1)) % 2;
            if (s == 1)
            {
                sex = 1;
               
            }
            else
            {
                sex = 0;
               
            }
            ErrMsg = (aCity[int.Parse(cid.Substring(0, 2))] + "," + cid.Substring(6, 4) + "-" + cid.Substring(10, 2) + "-" + cid.Substring(12, 2) + "," );
            return true;
        }

        public bool Check15()
        {
            string[] verifyID = new string[] { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "x" };
            CID = CID.Substring(0, 6) + "19" + CID.Substring(6, 9);
            foreach (string vid in verifyID)
            {
                bool result = Check18(CID + vid);
                if (result)
                {
                    ErrMsg += " your Card ID:" + CID + vid;
                    return true;
                }
            }
            return false;
        }

        public string CID
        {
            get { return cid_; }
            set { cid_ = value; }
        }

        public string ErrMsg
        {
            get { return errmsg; }
            set { errmsg = value; }
        }
    }

    public class hashTools
    {
        /// <summary>
        /// 计算SHA-512码
        /// </summary>
        /// <param name="word">字符串</param>
        /// <param name="toUpper">返回哈希值格式 true：英文大写，false：英文小写</param>
        /// <returns></returns>
        public static string Hash_SHA_512(string word, bool toUpper = true)
        {
            try
            {
                System.Security.Cryptography.SHA512CryptoServiceProvider SHA512CSP
                    = new System.Security.Cryptography.SHA512CryptoServiceProvider();

                byte[] bytValue = System.Text.Encoding.UTF8.GetBytes(word);
                byte[] bytHash = SHA512CSP.ComputeHash(bytValue);
                SHA512CSP.Clear();

                //根据计算得到的Hash码翻译为SHA-1码
                string sHash = "", sTemp = "";
                for (int counter = 0; counter < bytHash.Count(); counter++)
                {
                    long i = bytHash[counter] / 16;
                    if (i > 9)
                    {
                        sTemp = ((char)(i - 10 + 0x41)).ToString();
                    }
                    else
                    {
                        sTemp = ((char)(i + 0x30)).ToString();
                    }
                    i = bytHash[counter] % 16;
                    if (i > 9)
                    {
                        sTemp += ((char)(i - 10 + 0x41)).ToString();
                    }
                    else
                    {
                        sTemp += ((char)(i + 0x30)).ToString();
                    }
                    sHash += sTemp;
                }

                //根据大小写规则决定返回的字符串
                return toUpper ? sHash : sHash.ToLower();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        /// <summary>
        /// 计算SHA-384码
        /// </summary>
        /// <param name="word">字符串</param>
        /// <param name="toUpper">返回哈希值格式 true：英文大写，false：英文小写</param>
        /// <returns></returns>
        public static string Hash_SHA_384(string word, bool toUpper = true)
        {
            try
            {
                System.Security.Cryptography.SHA384CryptoServiceProvider SHA384CSP
                    = new System.Security.Cryptography.SHA384CryptoServiceProvider();

                byte[] bytValue = System.Text.Encoding.UTF8.GetBytes(word);
                byte[] bytHash = SHA384CSP.ComputeHash(bytValue);
                SHA384CSP.Clear();

                //根据计算得到的Hash码翻译为SHA-1码
                string sHash = "", sTemp = "";
                for (int counter = 0; counter < bytHash.Count(); counter++)
                {
                    long i = bytHash[counter] / 16;
                    if (i > 9)
                    {
                        sTemp = ((char)(i - 10 + 0x41)).ToString();
                    }
                    else
                    {
                        sTemp = ((char)(i + 0x30)).ToString();
                    }
                    i = bytHash[counter] % 16;
                    if (i > 9)
                    {
                        sTemp += ((char)(i - 10 + 0x41)).ToString();
                    }
                    else
                    {
                        sTemp += ((char)(i + 0x30)).ToString();
                    }
                    sHash += sTemp;
                }

                //根据大小写规则决定返回的字符串
                return toUpper ? sHash : sHash.ToLower();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        /// <summary>
        /// 计算SHA-256码
        /// </summary>
        /// <param name="word">字符串</param>
        /// <param name="toUpper">返回哈希值格式 true：英文大写，false：英文小写</param>
        /// <returns></returns>
        public static string Hash_SHA_256(string word, bool toUpper = true)
        {
            try
            {
                System.Security.Cryptography.SHA256CryptoServiceProvider SHA256CSP
                    = new System.Security.Cryptography.SHA256CryptoServiceProvider();

                byte[] bytValue = System.Text.Encoding.UTF8.GetBytes(word);
                byte[] bytHash = SHA256CSP.ComputeHash(bytValue);
                SHA256CSP.Clear();

                //根据计算得到的Hash码翻译为SHA-1码
                string sHash = "", sTemp = "";
                for (int counter = 0; counter < bytHash.Count(); counter++)
                {
                    long i = bytHash[counter] / 16;
                    if (i > 9)
                    {
                        sTemp = ((char)(i - 10 + 0x41)).ToString();
                    }
                    else
                    {
                        sTemp = ((char)(i + 0x30)).ToString();
                    }
                    i = bytHash[counter] % 16;
                    if (i > 9)
                    {
                        sTemp += ((char)(i - 10 + 0x41)).ToString();
                    }
                    else
                    {
                        sTemp += ((char)(i + 0x30)).ToString();
                    }
                    sHash += sTemp;
                }

                //根据大小写规则决定返回的字符串
                return toUpper ? sHash : sHash.ToLower();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        /// <summary>
        /// 计算SHA-1码
        /// </summary>
        /// <param name="word">字符串</param>
        /// <param name="toUpper">返回哈希值格式 true：英文大写，false：英文小写</param>
        /// <returns></returns>
        public static string Hash_SHA_1(string word, bool toUpper = true)
        {
            try
            {
                System.Security.Cryptography.SHA1CryptoServiceProvider SHA1CSP
                    = new System.Security.Cryptography.SHA1CryptoServiceProvider();

                byte[] bytValue = System.Text.Encoding.UTF8.GetBytes(word);
                byte[] bytHash = SHA1CSP.ComputeHash(bytValue);
                SHA1CSP.Clear();

                //根据计算得到的Hash码翻译为SHA-1码
                string sHash = "", sTemp = "";
                for (int counter = 0; counter < bytHash.Count(); counter++)
                {
                    long i = bytHash[counter] / 16;
                    if (i > 9)
                    {
                        sTemp = ((char)(i - 10 + 0x41)).ToString();
                    }
                    else
                    {
                        sTemp = ((char)(i + 0x30)).ToString();
                    }
                    i = bytHash[counter] % 16;
                    if (i > 9)
                    {
                        sTemp += ((char)(i - 10 + 0x41)).ToString();
                    }
                    else
                    {
                        sTemp += ((char)(i + 0x30)).ToString();
                    }
                    sHash += sTemp;
                }

                //根据大小写规则决定返回的字符串
                return toUpper ? sHash : sHash.ToLower();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        /// <summary>
         /// 计算16位2重MD5码
         /// </summary>
         /// <param name="word">字符串</param>
         /// <param name="toUpper">返回哈希值格式 true：英文大写，false：英文小写</param>
         /// <returns></returns>
        public static string Hash_2_MD5_16(string word, bool toUpper = true)
        {
            try
            {
                System.Security.Cryptography.MD5CryptoServiceProvider MD5CSP
                        = new System.Security.Cryptography.MD5CryptoServiceProvider();

                byte[] bytValue = System.Text.Encoding.UTF8.GetBytes(word);
                byte[] bytHash = MD5CSP.ComputeHash(bytValue);

                //根据计算得到的Hash码翻译为MD5码
                string sHash = "", sTemp = "";
                for (int counter = 0; counter < bytHash.Count(); counter++)
                {
                    long i = bytHash[counter] / 16;
                    if (i > 9)
                    {
                        sTemp = ((char)(i - 10 + 0x41)).ToString();
                    }
                    else
                    {
                        sTemp = ((char)(i + 0x30)).ToString();
                    }
                    i = bytHash[counter] % 16;
                    if (i > 9)
                    {
                        sTemp += ((char)(i - 10 + 0x41)).ToString();
                    }
                    else
                    {
                        sTemp += ((char)(i + 0x30)).ToString();
                    }
                    sHash += sTemp;
                }

                sHash = sHash.Substring(8, 16);

                bytValue = System.Text.Encoding.UTF8.GetBytes(sHash);
                bytHash = MD5CSP.ComputeHash(bytValue);
                MD5CSP.Clear();
                sHash = "";

                //根据计算得到的Hash码翻译为MD5码
                for (int counter = 0; counter < bytHash.Count(); counter++)
                {
                    long i = bytHash[counter] / 16;
                    if (i > 9)
                    {
                        sTemp = ((char)(i - 10 + 0x41)).ToString();
                    }
                    else
                    {
                        sTemp = ((char)(i + 0x30)).ToString();
                    }
                    i = bytHash[counter] % 16;
                    if (i > 9)
                    {
                        sTemp += ((char)(i - 10 + 0x41)).ToString();
                    }
                    else
                    {
                        sTemp += ((char)(i + 0x30)).ToString();
                    }
                    sHash += sTemp;
                }

                sHash = sHash.Substring(8, 16);

                //根据大小写规则决定返回的字符串
                return toUpper ? sHash : sHash.ToLower();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        /// <summary>
        /// 计算32位2重MD5码
        /// </summary>
        /// <param name="word">字符串</param>
        /// <param name="toUpper">返回哈希值格式 true：英文大写，false：英文小写</param>
        /// <returns></returns>
        public static string Hash_2_MD5_32(string word, bool toUpper = true)
        {
            try
            {
                System.Security.Cryptography.MD5CryptoServiceProvider MD5CSP
                    = new System.Security.Cryptography.MD5CryptoServiceProvider();

                byte[] bytValue = System.Text.Encoding.UTF8.GetBytes(word);
                byte[] bytHash = MD5CSP.ComputeHash(bytValue);

                //根据计算得到的Hash码翻译为MD5码
                string sHash = "", sTemp = "";
                for (int counter = 0; counter < bytHash.Count(); counter++)
                {
                    long i = bytHash[counter] / 16;
                    if (i > 9)
                    {
                        sTemp = ((char)(i - 10 + 0x41)).ToString();
                    }
                    else
                    {
                        sTemp = ((char)(i + 0x30)).ToString();
                    }
                    i = bytHash[counter] % 16;
                    if (i > 9)
                    {
                        sTemp += ((char)(i - 10 + 0x41)).ToString();
                    }
                    else
                    {
                        sTemp += ((char)(i + 0x30)).ToString();
                    }
                    sHash += sTemp;
                }

                bytValue = System.Text.Encoding.UTF8.GetBytes(sHash);
                bytHash = MD5CSP.ComputeHash(bytValue);
                MD5CSP.Clear();
                sHash = "";

                //根据计算得到的Hash码翻译为MD5码
                for (int counter = 0; counter < bytHash.Count(); counter++)
                {
                    long i = bytHash[counter] / 16;
                    if (i > 9)
                    {
                        sTemp = ((char)(i - 10 + 0x41)).ToString();
                    }
                    else
                    {
                        sTemp = ((char)(i + 0x30)).ToString();
                    }
                    i = bytHash[counter] % 16;
                    if (i > 9)
                    {
                        sTemp += ((char)(i - 10 + 0x41)).ToString();
                    }
                    else
                    {
                        sTemp += ((char)(i + 0x30)).ToString();
                    }
                    sHash += sTemp;
                }

                //根据大小写规则决定返回的字符串
                return toUpper ? sHash : sHash.ToLower();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        // <summary>
        /// 计算16位MD5码
        /// </summary>
        /// <param name="word">字符串</param>
        /// <param name="toUpper">返回哈希值格式 true：英文大写，false：英文小写</param>
        /// <returns></returns>
        public static string Hash_MD5_16(string word, bool toUpper = true)
        {
            try
            {
                string sHash = Hash_MD5_32(word).Substring(8, 16);
                return toUpper ? sHash : sHash.ToLower();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        /// <summary>
        /// 计算32位MD5码
        /// </summary>
        /// <param name="word">字符串</param>
        /// <param name="toUpper">返回哈希值格式 true：英文大写，false：英文小写</param>
        /// <returns></returns>
        public static string Hash_MD5_32(string word, bool toUpper = true)
        {
            try
            {
                System.Security.Cryptography.MD5CryptoServiceProvider MD5CSP
                    = new System.Security.Cryptography.MD5CryptoServiceProvider();

                byte[] bytValue = System.Text.Encoding.UTF8.GetBytes(word);
                byte[] bytHash = MD5CSP.ComputeHash(bytValue);
                MD5CSP.Clear();

                //根据计算得到的Hash码翻译为MD5码
                string sHash = "", sTemp = "";
                for (int counter = 0; counter < bytHash.Count(); counter++)
                {
                    long i = bytHash[counter] / 16;
                    if (i > 9)
                    {
                        sTemp = ((char)(i - 10 + 0x41)).ToString();
                    }
                    else
                    {
                        sTemp = ((char)(i + 0x30)).ToString();
                    }
                    i = bytHash[counter] % 16;
                    if (i > 9)
                    {
                        sTemp += ((char)(i - 10 + 0x41)).ToString();
                    }
                    else
                    {
                        sTemp += ((char)(i + 0x30)).ToString();
                    }
                    sHash += sTemp;
                }

                //根据大小写规则决定返回的字符串
                return toUpper ? sHash : sHash.ToLower();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
    }
   public enum enum_Apptype
    {
    winclient=0,webclient=1,webapiclient=2,mobile_ios=3,mobile_android=4,mobile_wp=5,nwServer=6
    }
   static public class static_sysdata
    {
        public static enum_Apptype clienttype = enum_Apptype.winclient;//default=0:windows client;1: web applicaion;2:Webapi;nwServer:6 //must set for all application

        public static string AppSiteKey;
        public static string DateTimeFormatter;
        public static string mongdbConn;
        public static string mongdbName;
        public static string mongdbCollectionName;
        public static string SignatureSecret;
        public static string mysqldbconn;
        public static Assembly modelasm;

       
   }
   public static class tools_static
   {
        public static Type[] GetTypesFromAssemblysByType(Type t)
        {
            IList<Assembly> allAssemblies = Assembly.GetEntryAssembly().GetReferencedAssemblies().Select(Assembly.Load).ToList();
            if (!allAssemblies.Contains(Assembly.GetEntryAssembly()))
                allAssemblies.Add(Assembly.GetEntryAssembly());
            List<Type> returnlist = new List<Type>();
            foreach (var obj in allAssemblies)
            {
                var list1 = (from x in obj.GetTypes() where t.IsAssignableFrom(x) select x).ToList(); //实现t接口或集成t的类型列表
                returnlist.AddRange(list1);
            }
            return returnlist.ToArray();

        }
        public static Type getTypeHaveMethodByName(Type[] types, string name)
        {
            foreach (var one in types)
            {
                var list1 = one.GetMethods(BindingFlags.Public | BindingFlags.Static |BindingFlags.Instance);
                var t = (from x in list1  where x.Name == name select x).FirstOrDefault();
                if (t != null)
                    return one;

            }
            return null;
        }
        public static void CopyDir(string srcPath, string destPath)
        {
            try
            {
                DirectoryInfo dir = new DirectoryInfo(srcPath);
                FileSystemInfo[] fileinfo = dir.GetFileSystemInfos();  //获取目录下（不包含子目录）的文件和子目录
                foreach (FileSystemInfo i in fileinfo)
                {
                    if (i is DirectoryInfo)     //判断是否文件夹
                    {
                        if (!Directory.Exists(Path.Combine( destPath , i.Name)))
                        {
                            Directory.CreateDirectory(Path.Combine(destPath, i.Name));   //目标目录下不存在此文件夹即创建子文件夹
                        }
                        CopyDir(i.FullName, Path.Combine(destPath, i.Name));    //递归调用复制子文件夹
                    }
                    else
                    {
                        File.Copy(i.FullName, Path.Combine(destPath, i.Name), true);      //不是文件夹即复制文件，true表示可以覆盖同名文件
                    }
                }
            }
            catch (Exception e)
            {
                throw;
            }
        }
        /// <summary>
        /// Base64解密
        /// </summary>
        /// <param name="codeName">解密采用的编码方式，注意和加密时采用的方式一致</param>
        /// <param name="result">待解密的密文</param>
        /// <returns>解密后的字符串</returns>
        public static string DecodeBase64(Encoding encode, string result)
        {
            string decode = "";
            byte[] bytes = Convert.FromBase64String(result);
            try
            {
                decode = encode.GetString(bytes);
            }
            catch
            {
                decode = result;
            }
            return decode;
        }

        /// <summary>
        /// Base64加密
        /// </summary>
        /// <param name="codeName">加密采用的编码方式</param>
        /// <param name="source">待加密的明文</param>
        /// <returns></returns>
        public static string EncodeBase64(Encoding encode, string source)
        {
            string res;
            byte[] bytes = encode.GetBytes(source);
            try
            {
                res = Convert.ToBase64String(bytes);
            }
            catch
            {
                res = source;
            }
            return res;
        }
        /// <summary>
        /// 获取星期的中文说明
        /// </summary>
        /// <param name="iweekNum">0-6,周日-周六</param>
        /// <returns></returns>
        public static string getWeekNameCn(int iweekNum)
        {
            string res;
            string[] weekNum = { "星期日", "星期一", "星期二", "星期三", "星期四", "星期五", "星期六" };
            if (iweekNum > 6 || iweekNum < 0)
                res = " ";
            else
                res = weekNum[iweekNum];
            return res;
        }
        ///<summary>
        /// 从枚举类型和它的特性读出并返回一个键值对,枚举值和中文描述
        ///</summary>
        ///<param name="enumType">Type,该参数的格式为typeof(需要读的枚举类型)</param>
        ///<returns>键值对</returns>
        public static NameValueCollection GetNVCFromEnumValue(Type enumType)
        {
            NameValueCollection nvc = new NameValueCollection();
            Type typeDescription = typeof(DescriptionAttribute);
            System.Reflection.FieldInfo[] fields = enumType.GetFields();
            string strText = string.Empty;
            string strValue = string.Empty;
            foreach (FieldInfo field in fields)
            {
                if (field.FieldType.IsEnum)
                {
                    strValue = ((int)enumType.InvokeMember(field.Name, BindingFlags.GetField, null, null, null)).ToString();
                    object[] arr = field.GetCustomAttributes(typeDescription, true);
                    if (arr.Length > 0)
                    {
                        DescriptionAttribute aa = (DescriptionAttribute)arr[0];
                        strText = aa.Description;
                    }
                    else
                    {
                        strText = field.Name;
                    }
                    nvc.Add(strValue, strText);
                }
            }
            return nvc;
        }
        ///<summary>
        /// 返回 Dic<枚举项，描述>
        ///</summary>
        ///<param name="enumType"></param>
        ///<returns>Dic<枚举项，描述></returns>
        public static Dictionary<string, string> GetEnumDic(Type enumType)
                {
            Dictionary<string, string> dic = new Dictionary<string, string>();
            FieldInfo[] fieldinfos = enumType.GetFields();
            foreach (FieldInfo field in fieldinfos)
            {
                if (field.FieldType.IsEnum)
                {
                    Object[] objs = field.GetCustomAttributes(typeof(DescriptionAttribute), false);

                    dic.Add(field.Name, ((DescriptionAttribute)objs[0]).Description);
                }

            }

            return dic;
        }
        public static T getJobjectValue<T>(JObject jobj, string keyname)
        {
            if (jobjectHaveKey(jobj, keyname))
            {
                return jobj.Value<T>(keyname);
            }
            return default(T);
        }
        public static bool jobjectHaveKey(JObject jobj ,string keyname)
        {
            foreach (var p in jobj.Properties())
            {
                if (p.Name == keyname)
                    return true;
            }
            return false;
        }
        /// <summary>
        /// 比较程序集版本号的大小
        /// 版本号必须是x.xx.xxxx.x
        /// 并且两个版本的格式必须相同，有相同的分隔符数量。
        /// </summary>
        /// <param name="version1"></param>
        /// <param name="version2"></param>
        /// <returns>
        /// version1>version2  为1
        /// version1=version2  为0
        /// version1<version2  为-1
        /// 
        /// </returns>
        public static int CompareVersion(string version1, string version2)
       {
           string[] arrayVer1 = version1.Split('.');
           string[] arrayVer2 = version2.Split('.');
           for (int i = 0; i < arrayVer1.Length; i++)
           {
               int ver1 = int.Parse(arrayVer1[i]);
               int ver2 = int.Parse(arrayVer2[i]);
               if (ver1 > ver2)
                   return 1;
               else
                   if (ver1 < ver2)
                       return -1;
                   
           }
           return 0;
       }
        /// <summary>
        /// 整型转为utc时间
        /// </summary>
        /// <param name="utc"></param>
        /// <returns></returns>
       public static DateTime ConvertIntDatetime(Int64 utc)
       {
           System.DateTime startTime = TimeZone.CurrentTimeZone.ToLocalTime(new System.DateTime(1970, 1, 1));
           startTime = startTime.AddSeconds(utc);
          // startTime = startTime.AddHours(8);//转化为北京时间(北京时间=UTC时间+8小时 )
           return startTime;
       }
       /// <summary>
       /// UTC时间转为整型
       /// </summary>
       /// <param name="time"></param>
       /// <returns></returns>
       public static Int64 ConvertDateTimeInt(System.DateTime time)
       {
           Int64 intResult = 0;
           System.DateTime startTime = TimeZone.CurrentTimeZone.ToLocalTime(new System.DateTime(1970, 1, 1));
           intResult = Convert.ToInt64((time - startTime).TotalSeconds );
           return intResult;
       }
        public static string getExceptionMessage(Exception ex)
        {
            string message = ex.Message;
            Exception e = ex;
            while (e.InnerException != null)
            {
                message = message + System.Environment.NewLine + e.InnerException.Message;
                e = e.InnerException;
            }
            return message;
        }

        public static byte[] StreamToBytes(Stream stream)
       {
           byte[] bytes = new byte[stream.Length];
           stream.Seek(0, SeekOrigin.Begin); 
           stream.Read(bytes, 0, bytes.Length);

           // 设置当前流的位置为流的开始 
           stream.Seek(0, SeekOrigin.Begin);
           return bytes;
       }
           /// <summary>
        /// C#将IP地址转为长整形
        /// </summary>
        /// <param name="ip"></param>
        /// <returns></returns>
        public static long IpToNumber(string ip)
        {
            string[] arr = ip.Split('.');
            return 256 * 256 * 256 * long.Parse(arr[0]) + 256 * 256 * long.Parse(arr[1]) + 256 * long.Parse(arr[2]) + long.Parse(arr[3]);
        }
        /// <summary>
        /// C#判断IP地址是否为私有/内网ip地址
        /// </summary>
        /// <param name="ip"></param>
        /// <returns></returns>
        public static bool IsPrivateIp(string ip)
        {
            long ABegin = IpToNumber("10.0.0.0"), AEnd = IpToNumber("10.255.255.255"),//A类私有IP地址
             BBegin = IpToNumber("172.16.0.0"), BEnd = IpToNumber("172.31.255.255"),//'B类私有IP地址
             CBegin = IpToNumber("192.168.0.0"), CEnd = IpToNumber("192.168.255.255"),//'C类私有IP地址
             IpNum = IpToNumber(ip);
            return (ABegin <= IpNum && IpNum <= AEnd) || (BBegin <= IpNum && IpNum <= BEnd) || (CBegin <= IpNum && IpNum <= CEnd);
        }

        public static DateTime SetDateTime(DateTime dt,string strtime)
       {
           string tmpdt = dt.ToString("yyyyMMdd");
           tmpdt = tmpdt + strtime;
           DateTime newdt;
           IFormatProvider ifp = new CultureInfo("zh-CN", true);
           DateTime.TryParseExact(tmpdt, "yyyyMMddHHmmss", ifp, DateTimeStyles.None, out newdt);
           return newdt;
       }

        ///   <summary>
        ///   给一个字符串进行MD5加密
        ///   </summary>
        ///   <param   name="strText">待加密字符串</param>
        ///   <returns>加密后的字符串</returns>
        public static string MD5Encrypt(string strText)
        {
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] result = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(strText));
            return System.Text.Encoding.UTF8.GetString(result);
        }
        public static string ToHexString(IEnumerable<byte> value)
        {
            StringBuilder sb = new StringBuilder();
            foreach (byte b in value)
                sb.AppendFormat("{0:x2}", b);
            return sb.ToString();
        }
        public static byte[] HexToBytes(string value)
        {
            if (value == null || value.Length == 0)
                return new byte[0];
            if (value.Length % 2 == 1)
                throw new FormatException();
            byte[] result = new byte[value.Length / 2];
            for (int i = 0; i < result.Length; i++)
                result[i] = byte.Parse(value.Substring(i * 2, 2), NumberStyles.AllowHexSpecifier);
            return result;
        }


        /// <summary>
        /// 计算参数签名
        /// </summary>
        /// <param name="params">请求参数集，所有参数必须已转换为字符串类型</param>
        /// <param name="secret">签名密钥</param>
        /// <returns>签名</returns>
        public static string getSignature(IDictionary<string, string> parameters, string secret)
        {
            // 先将参数以其参数名的字典序升序进行排序
            IDictionary<string, string> sortedParams = new SortedDictionary<string, string>(parameters);
            IEnumerator<KeyValuePair<string, string>> iterator = sortedParams.GetEnumerator();

            // 遍历排序后的字典，将所有参数按"key=value"格式拼接在一起
            StringBuilder basestring = new StringBuilder();
            while (iterator.MoveNext())
            {
                string key = iterator.Current.Key;
                string value = iterator.Current.Value;
                if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
                {
                    basestring.Append(key).Append("=").Append(value);
                }
            }
            basestring.Append(secret);

            // 使用MD5对待签名串求签
            MD5 md5 = MD5.Create();
            byte[] bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(basestring.ToString()));

            // 将MD5输出的二进制结果转换为小写的十六进制
            StringBuilder result = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                string hex = bytes[i].ToString("x");
                if (hex.Length == 1)
                {
                    result.Append("0");
                }
                result.Append(hex);
            }

            return result.ToString();
        }


   }
   public class logintoken
   {
       public string UID;
       public string loginID;
       public string roleKeyList;
       public string tokenID;
        /// <summary>
        /// 归属组织id和组织type，！分割多个归属组织，$分割同一个组织的id和typekey
        /// </summary>
        public string OrgList;/* */
       public logintoken(string struid,string str_loginid,string strrolelist)
       {
           UID = struid;
           loginID = str_loginid;
           roleKeyList = strrolelist;
         
       }
        public logintoken()
        {
            UID = "-1";
            loginID = "~Anonymous";
            roleKeyList = "$";
            tokenID = "-1";
            OrgList = "!$";
        }
        public logintoken(string strDesToken)
        {
            string[] strarray = strDesToken.Split(':');
            UID = strarray[0];
            roleKeyList = strarray[1];
            OrgList = strarray[2];
            if (strarray.Length == 5)
                tokenID = strarray[4];
        }
   
   


   }
   public class CustomPrincipal : IPrincipal
   {

       private CustomIdentity identity;

       public CustomPrincipal(CustomIdentity identity)
       {
           this.identity = identity;
       }

        public CustomPrincipal(IPrincipal principal)
        {
            this.identity = principal.Identity as CustomIdentity;
        }
       public IIdentity Identity
       {
           get
           {
               return identity;
           }
       }

       public bool IsInRole(string role)
       {
           return identity.IsInRole(role);
       }
   }

   public class CustomIdentity : IIdentity
   {
       private logintoken _token;
       private string ApiLoginIP;
        public string Uid {
           get
           {
               if (_token != null)
                   return _token.UID;
               else
                   return "";
       }}
       public bool IsInRole(string inroleid)
       {
           return _token.roleKeyList.Split('$').Contains(inroleid);
       
       }
       public CustomIdentity(string strLoginToken )
       {
           this._token = new logintoken(strLoginToken);
          
        
       }
        public CustomIdentity()
        {
            this._token = new logintoken();
        }
        public CustomIdentity(logintoken token)
       {

           this._token = token;
       }
        public string AuthenticationType
       {
           get { return "Custom"; }
       }

       public bool IsAuthenticated
       {
           get { return true; }
       }

       public string Name
       {
           get
           {
               if (static_sysdata.clienttype == enum_Apptype.webclient)
                   return _token.loginID;
               else
                   return this._token.UID;
           }
       }
       public string ApiIP
       {
           get
           {
               return this.ApiLoginIP;
           }
           set
           {
               this.ApiLoginIP = value;
           }
       }
      
      
     
      
   }


  
  public static class PredicateExtensionses
  {
      public static Expression<Func<T, bool>> True<T>() { return f => true; }

      public static Expression<Func<T, bool>> False<T>() { return f => false; }

      public static Expression<Func<T, bool>> Or<T>(this Expression<Func<T, bool>> exp_flow, Expression<Func<T, bool>> expression2)
      {

          var invokedExpression = System.Linq.Expressions.Expression.Invoke(expression2, exp_flow.Parameters.Cast<System.Linq.Expressions.Expression>());

          return System.Linq.Expressions.Expression.Lambda<Func<T, bool>>(System.Linq.Expressions.Expression.Or(exp_flow.Body, invokedExpression), exp_flow.Parameters);

      }
      public static Expression<Func<T, bool>> And<T>(this Expression<Func<T, bool>> exp_flow, Expression<Func<T, bool>> expression2)
      {

          var invokedExpression = System.Linq.Expressions.Expression.Invoke(expression2, exp_flow.Parameters.Cast<System.Linq.Expressions.Expression>());

          return System.Linq.Expressions.Expression.Lambda<Func<T, bool>>(System.Linq.Expressions.Expression.And(exp_flow.Body, invokedExpression), exp_flow.Parameters);

      }
  }

  public class LevenshteinDistance
  {
      private static LevenshteinDistance _instance = null;
      public static LevenshteinDistance Instance
      {
          get
          {
              if (_instance == null)
              {
                  return new LevenshteinDistance();
              }
              return _instance;
          }
      }

      /// <summary>
      /// 取最小的一位数
      /// </summary>
      /// <param name=”first”></param>
      /// <param name=”second”></param>
      /// <param name=”third”></param>
      /// <returns></returns>
      public int LowerOfThree(int first, int second, int third)
      {
          int min = Math.Min(first, second);
          return Math.Min(min, third);
      }
      /// <summary>
      /// www.it165.net
      /// </summary>
      /// <param name="str1"></param>
      /// <param name="str2"></param>
      /// <returns></returns>
      public int Levenshtein_Distance(string str1, string str2)
      {
          int[,] Matrix;
          int n = str1.Length;
          int m = str2.Length;

          int temp = 0;
          char ch1;
          char ch2;
          int i = 0;
          int j = 0;
          if (n == 0)
          {
              return m;
          }
          if (m == 0)
          {

              return n;
          }
          Matrix = new int[n + 1, m + 1];

          for (i = 0; i <= n; i++)
          {
              //初始化第一列
              Matrix[i, 0] = i;
          }

          for (j = 0; j <= m; j++)
          {
              //初始化第一行
              Matrix[0, j] = j;
          }

          for (i = 1; i <= n; i++)
          {
              ch1 = str1[i - 1];
              for (j = 1; j <= m; j++)
              {
                  ch2 = str2[j - 1];
                  if (ch1.Equals(ch2))
                  {
                      temp = 0;
                  }
                  else
                  {
                      temp = 1;
                  }
                  Matrix[i, j] = LowerOfThree(Matrix[i - 1, j] + 1, Matrix[i, j - 1] + 1, Matrix[i - 1, j - 1] + temp);

              }
          }

          for (i = 0; i <= n; i++)
          {
              for (j = 0; j <= m; j++)
              {
                  Console.Write(" {0} ", Matrix[i, j]);
              }
              Console.WriteLine("");
          }
          return Matrix[n, m];
      }

      /// <summary>
      /// 计算字符串相似度
      /// </summary>
      /// <param name=”str1″></param>
      /// <param name=”str2″></param>
      /// <returns></returns>
      public decimal LevenshteinDistancePercent(string str1, string str2)
      {
          int val = Levenshtein_Distance(str1, str2);
          return 1 - (decimal)val / Math.Max(str1.Length, str2.Length);
      }
  }
  
}
