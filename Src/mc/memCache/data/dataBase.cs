using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace msgp.mc.server.data
{
    public class dataBase
    {
        public string baseDataPath { get; private set; }

        public int shardCount { get; private set; }


        /// <summary>
        /// 
        /// </summary>
        public int saveTimeSpan { get; private set; }

        public string saveFileNamePrix { get; private set; }
        /// <summary>
        /// 数据分片对象字典，键值为 shardNo
        /// </summary>
        private ConcurrentDictionary<int, dataShard> _dataShardsDic;

        private void initDataBase(string database, DirectoryInfo baseDbDir)
        {
            foreach (FileInfo NextFile in baseDbDir.GetFiles())
            {
                if (NextFile.Name == "0-0-11.grid")
                    continue;
                // 获取文件完整路径
                string heatmappath = NextFile.FullName;

            }

        }
    }
}
