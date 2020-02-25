using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using msgp.mc.model;
namespace msgp.mc.server.data
{
    public class cacheService
    {
        private static object lockObj = new object();

        private static cacheService _instance;
        internal static void initInstance(int maxDb,string _baseDataPath,int _shardCount,int saveFileSec)
        {
             _instance = new cacheService(maxDb,_baseDataPath,_shardCount,saveFileSec);
            _instance.loadData();
        }
        public static cacheService getInstance()
        {
            if (_instance == null)
            {
                lock (lockObj)
                {
                    if (_instance == null)
                        throw new NotImplementedException();
                }
            }
            return _instance;
        }
       public string baseDataPath { get; private set; }
        
        public int shardCount { get; private set; }
 
        /// <summary>
        /// 最大数据库数量
        /// </summary>
        public int maxDbcount { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public int saveTimeSpan { get; private set; }

        /// <summary>
        /// 数据库对象字典，键值为{database}
        /// </summary>
        private ConcurrentDictionary<string, dataBase> _dataBasesDic;
        private cacheService(int maxDb,string _baseDataPath,int _shardCount,int saveFileSec)
        {
            this.baseDataPath = _baseDataPath;
            this.maxDbcount = maxDb;
            this.shardCount = _shardCount;
            this.saveTimeSpan = saveFileSec;
        }
        public static int getShardId(string key, int shardCount)
        {
            var tmp = key.ToMD5();
            var value = long.Parse(tmp, System.Globalization.NumberStyles.HexNumber);
                int mod = (int)(value % shardCount);
            return mod;
        }
        public void loadData()
        {
            //两级目录，数据库目录，和数据目录下的分片数据文件
            DirectoryInfo TheFolder = new DirectoryInfo(baseDataPath);
            if (!TheFolder.Exists)
                return;
            var subfolders = TheFolder.GetDirectories();

            foreach (var subfolder in subfolders)
            {
                
            }
        }

        
        public string connect(string username, string passwd, string dbname)
        {
            throw new NotImplementedException();
        }
    }
}
