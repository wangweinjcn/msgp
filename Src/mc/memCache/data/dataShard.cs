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
        /// 文件名
        /// </summary>
        public string filename {  get; private set; }
        /// <summary>
        /// 分片的ID，key值得md5转整形，然后按分片总数取余数
        /// </summary>
        public int shardNO{  get; private set; }
        /// <summary>
        /// 
        /// </summary>
        public int saveTimeSpan{  get; private set; }
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
        private ConcurrentDictionary<Type, List<baseMcObject>> _typeListDic;
        /// <summary>
        /// 对象过期时间分组字典，时间结构(yyyymmddhhMM),到分钟为单位
        /// </summary>
        private ConcurrentDictionary<string, List<baseMcObject>> _expireListDic;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fn">文件名</param>
        /// <param name="no">分片号</param>
        /// <param name="savets">保存间隔，单位秒</param>
        public dataShard(string fn,int no,int savets)
        {
            this.filename = fn;
            this.shardNO = no;
            this.saveTimeSpan = savets;
            _dataDic = new ConcurrentDictionary<string, baseMcObject>();
            _expireListDic = new ConcurrentDictionary<string, List<baseMcObject>>();
            _typeListDic = new ConcurrentDictionary<Type, List<baseMcObject>>();
        }
        public void delete(string key)
        {
            if (!_dataDic.ContainsKey(key))
                return;
            baseMcObject outdata;
            _dataDic.Remove(key, out outdata);
            var dtenum = outdata.getDataType();
           
            var list = _typeListDic[dtenum];
            list.Remove(outdata);
            string expstr = outdata.expirets.ToString("yyyyMMddHHmm");
            if (_expireListDic.ContainsKey(expstr))
            {
                list = _expireListDic[expstr];
                list.Remove(outdata);
            }
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
        public void add(string key, baseMcObject data)
        {
            if (string.IsNullOrEmpty(key))
                return;
            if (data == null)
                return;
            if (key != data.key)
                return;
            baseMcObject oldobj=null;
            if (_dataDic.ContainsKey(key))
                oldobj = _dataDic[key];
           
             datatypeEnum dtenum = data.GetDatatypeEnum();
            if (oldobj != null)
            {
                delete(oldobj.key);
               
            }
            _dataDic.AddOrUpdate(key, data,(Key, value) => value);
            if (!_typeListDic.ContainsKey(dtenum))
                _typeListDic.AddOrUpdate(dtenum, new List<baseMcObject>(), (Key, value) => value);
            var list = _typeListDic[dtenum];
            list.Add(data);
            string expstr = data.expirets.ToString("yyyyMMddHHmm");
            if (!_expireListDic.ContainsKey(expstr))
            {
               _expireListDic.AddOrUpdate(expstr, new List<baseMcObject>(), (Key, value) => value);
            }
            list = _expireListDic[expstr];
            list.Add(data);
            //switch (dtenum)
            //{
            //    case (datatypeEnum.boolMcObject):
            //        break;
            //    case (datatypeEnum.decimalMcObject):
            //        break;
            //    case (datatypeEnum.doubleMcObject):
            //        break;
            //    case (datatypeEnum.dtMcObject):
            //        break;
            //    case (datatypeEnum.dynamicMcObject):
            //        break;
            //    case (datatypeEnum.intMcObject):
            //        break;
            //    case (datatypeEnum.listMcObject):
            //        break;
            //    case (datatypeEnum.mapMcObject):
            //        break;
            //    case (datatypeEnum.stringMcObject):
            //        break;
            //}
        }

        public void saveToFile()
        {
            Dictionary<Type, byte[]> saveDic = new Dictionary<Type, byte[]>();
            foreach (var obj in _typeListDic)
            {
                byte[] datalist = MessagePackSerializer.Serialize(obj.Value,MessagePack.Resolvers.ContractlessStandardResolver.Instance);
                saveDic.Add(obj.Key, datalist);
            }
            var allbin = MessagePackSerializer.Serialize(saveDic,MessagePack.Resolvers.ContractlessStandardResolver.Instance);
            File.WriteAllBytes(filename, allbin);

        }
    }
    internal class saveModel
    {
        public datatypeEnum dtname;
        public byte[] mpackBin;
    }
}
