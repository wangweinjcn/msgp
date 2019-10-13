using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace msgp.common.Configurations
{
    public class proopertiesConf
    {
        public static void Load(string file)
        {
            _confContent = new Hashtable();
            string content = null;
            try
            {
                content = File.ReadAllText(file, System.Text.Encoding.UTF8);
            }
            catch (Exception e)
            {
                return ;
            }
            string[] rows = content.Split(System.Environment.NewLine);
            string[] kv = null;
            foreach (string c in rows)
            {
                if (c.Trim().Length == 0)
                    continue;
                kv = c.Split('=');
                if (kv.Length == 1)
                {
                    _confContent[kv[0].Trim()] = "";
                }
                else if (kv.Length == 2)
                {
                    _confContent[kv[0].Trim()] = kv[1].Trim();
                }
            }
            return ;
        }

        private static Hashtable _confContent;

        public string getValue(string key)
        {
            if (_confContent.ContainsKey(key.Trim()))
                return _confContent[key].ToString();
            return null;
        }
        public void setValue(string key, string value)
        {
            if (_confContent.ContainsKey(key.Trim()))
                _confContent[key] = value;
        }
        public static bool Save(string file, Hashtable ht)
        {
            if (ht == null || ht.Count == 0)
                return false;
            StringBuilder sb = new StringBuilder(ht.Count * 12);
            foreach (string k in ht.Keys)
            {
                sb.Append(k).Append('=').Append(ht[k]).Append(System.Environment.NewLine);
            }
            try
            {
                File.WriteAllText(file, sb.ToString(), System.Text.Encoding.UTF8);
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        public static string test()
        {
            string file = @"D:\cfg.txt";
            if (!File.Exists(file))
                File.WriteAllText(file, "");

             proopertiesConf.Load(file);
            var ht = _confContent;
            if (ht != null)
            {
                ht["Time" + ht.Count] = System.DateTime.Now.ToString();
                proopertiesConf.Save(file, ht);

                StringBuilder sb = new StringBuilder(ht.Count * 12);
                foreach (string k in ht.Keys)
                {
                    sb.Append(k).Append('=').Append(ht[k]).Append(System.Environment.NewLine);
                }
                return sb.ToString();
            }
            return "none";
        }

       


    }

}
