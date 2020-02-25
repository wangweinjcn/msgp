using MessagePack;
using msgp.mc.model;
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Collections.Concurrent;

namespace msgp.mc.server.data
{
    public class dataShard
    {
        /// <summary>
        /// 检查过期时间间隔，单位秒
        /// </summary>
        private int checkExpireSecond = 50;
       
        /// <summary>
        /// 
        /// </summary>
        public long memoryValue{  get; private set; }
        /// <summary>
        /// 
        /// </summary>
        public long itemCount{  get; private set; }
        /// <summary>
        /// 按key值对象
        /// </summary>
        private  ConcurrentDictionary<string, baseMcObject> _dataDic;
        /// <summary>
        /// 按数据类型分组对象
        /// </summary>
        private ConcurrentDictionary<string, List<baseMcObject>> _typeListDic;
        /// <summary>
        /// 对象过期时间分组字典，时间结构(yyyymmddhhMM),到分钟为单位
        /// </summary>
        private ConcurrentDictionary<string, List<baseMcObject>> _expireListDic;

        private bool checking = false;
        private object _lock = new object();
        private void checkExpire(Object state, System.Timers.ElapsedEventArgs e)
        {
            if (checking)
                return;
            lock (_lock)
            {
                if (checking)
                    return;
                checking = true;
            }
            try
            {
                var nowstr = long.Parse(DateTime.Now.ToString("yyyyMMddHHmm"));
                foreach (var obj in _expireListDic)
                {
                    var tmp = long.Parse(obj.Key);
                    if (tmp > nowstr)
                        continue;
                    var list1 = obj.Value;
                    foreach (var onedata in list1)
                    {
                        baseMcObject outdata;
                        _dataDic.Remove(onedata.key, out outdata);
                        var dtenum = outdata.getDataType().ToString();

                        var list = _typeListDic[dtenum];
                        list.Remove(onedata);

                    }
                    List<baseMcObject> outd;
                    _expireListDic.Remove(obj.Key, out outd);

                }
            }
            catch (Exception ex)
            {
                
            }
            finally
            {
                checking = false;
            }
        }
        System.Timers.Timer checkExpireTimer;

       

        /// <summary>
        /// 
        /// </summary>

        public dataShard()
        {

            _dataDic = new ConcurrentDictionary<string, baseMcObject>();
            _expireListDic = new ConcurrentDictionary<string, List<baseMcObject>>();
            _typeListDic = new ConcurrentDictionary<string, List<baseMcObject>>();

            checkExpireTimer = new System.Timers.Timer(this.checkExpireSecond*1000);
            checkExpireTimer.Elapsed += new System.Timers.ElapsedEventHandler(checkExpire);
            checkExpireTimer.AutoReset = true;
            checkExpireTimer.Start();

        }
        public bool delete(string key)
        {
            if (!_dataDic.ContainsKey(key))
                return false;
            baseMcObject outdata;
            _dataDic.Remove(key, out outdata);
            var dtenum = outdata.getDataType().ToString();
           
            var list = _typeListDic[dtenum];
            list.Remove(outdata);
            string expstr = outdata.expirets.ToString("yyyyMMddHHmm");
            if (_expireListDic.ContainsKey(expstr))
            {
                list = _expireListDic[expstr];
                list.Remove(outdata);
            }
            return true;
        }
        public baseMcObject get(string key)
        {
            baseMcObject res = null;
            if (_dataDic.ContainsKey(key))
            {
                res = _dataDic[key];
                if (res.expirets < DateTime.Now)
                    res = null;
            }
            return res;
        }
        public bool addLock(string key,DateTime expirets)
        {
            if (string.IsNullOrEmpty(key))
                return false;
            if (_dataDic.ContainsKey(key))
                return false;
            baseMcObject data = new baseMcObject();
            data.key = key;
            data.isock = true;
            data.expirets =expirets;
            return this.add(data);

        }
        public bool add(baseMcObject data)
        {
            if (string.IsNullOrEmpty(data.key))
                return false;
            if (data == null)
                return false;
          
            baseMcObject oldobj=null;
            if (_dataDic.ContainsKey(data.key))
                oldobj = _dataDic[data.key];

            var dType = data.getDataType().ToString();
            if (oldobj != null)
            {
                delete(oldobj.key);
               
            }
            _dataDic.AddOrUpdate(data.key, data,(Key, value) => value);
            if (!_typeListDic.ContainsKey(dType))
                _typeListDic.AddOrUpdate(dType, new List<baseMcObject>(), (Key, value) => value);
            var list = _typeListDic[dType];
            list.Add(data);
            string expstr = data.expirets.ToString("yyyyMMddHHmm");
            if (!_expireListDic.ContainsKey(expstr))
            {
               _expireListDic.AddOrUpdate(expstr, new List<baseMcObject>(), (Key, value) => value);
            }
            list = _expireListDic[expstr];
            list.Add(data);
            return true;
        }

        public ConcurrentDictionary<string, List<baseMcObject>>  gettypeListDic()
        {
            return this._typeListDic;

        }
    }
    internal class saveModel
    {
        public datatypeEnum dtname;
        public byte[] mpackBin;
    }
}
