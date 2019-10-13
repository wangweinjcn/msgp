using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace msgp.mc.server.data
{
    public class dataService
    {

       public string baseDataPath { get; private set; }
        
        public int shardCount { get; private set; }
 

        /// <summary>
        /// 
        /// </summary>
        public int saveTimeSpan { get; private set; }

        public string saveFileNamePrix { get; private set; }
        /// <summary>
        /// 数据库对象字典，键值为{database}
        /// </summary>
        private ConcurrentDictionary<string, dataBase> _dataBasesDic;
        public dataService()
        {

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

    }
}
