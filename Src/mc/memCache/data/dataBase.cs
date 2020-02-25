using MessagePack;
using msgp.mc.model;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace msgp.mc.server.data
{
    public class dataBase:iDataService
    {
        public string baseDataPath { get; private set; }

        public int shardCount { get; private set; }
        /// <summary>
        /// 数据库名称
        /// </summary>
        public string databaseName { get; private set; }
        /// <summary>
        /// 数据保存时间，单位秒
        /// </summary>
        public int saveTimeSpan { get; private set; }

        public string saveFileNamePrix { get; private set; }
        /// <summary>
        /// 数据分片对象字典，键值为 shardNo
        /// </summary>
        private ConcurrentDictionary<int, dataShard> _dataShardsDic;
        /// <summary>
        /// 文件保存定时器
        /// </summary>
         System.Timers.Timer saveFileTimer;

        private string savefilename { get {return  Path.Combine(baseDataPath, this.databaseName, ".data"); } }
        private void initDataBase()
        {
            if (!File.Exists(savefilename))
                return;
            byte[] allbyte = File.ReadAllBytes(savefilename);
            if (allbyte != null && allbyte.Length > 0)
            {
                var savebinDic = MessagePackSerializer.Deserialize<Dictionary<string, byte[]>>(allbyte);
                foreach (var obj in savebinDic)
                {
                   IList<baseMcObject> lists;
                    var typename = obj.Key;
                    Type ttype = dataBase.typen(typename);
                    Type mcoType = typeof(mcObject<>).MakeGenericType(ttype);
                    switch (obj.Key.ToLower())
                    {
                        
                        case("system.int"):
                             lists = MessagePackSerializer.Deserialize<List<mcObject<int>>>(obj.Value) as IList<baseMcObject>;
                            break;
                        case ("system.int32"):
                            lists = MessagePackSerializer.Deserialize<List<mcObject<Int32>>>(obj.Value) as IList<baseMcObject>;
                            break;
                        case ("system.int16"):
                            lists = MessagePackSerializer.Deserialize<List<mcObject<Int16>>>(obj.Value) as IList<baseMcObject>;
                            break;
                        case ("system.int64"):

                        case ("system.long"):
                             lists = MessagePackSerializer.Deserialize<List<mcObject<Int64>>>(obj.Value) as IList<baseMcObject>;
                            break;
                        case ("system.int[]"):
                             lists = MessagePackSerializer.Deserialize<List<mcObject<int[]>>>(obj.Value) as IList<baseMcObject>;
                            break;
                        case ("system.int32[]"):
                             lists = MessagePackSerializer.Deserialize<List<mcObject<Int32[]>>>(obj.Value) as IList<baseMcObject>;
                            break;
                        case ("system.int16[]"):
                             lists = MessagePackSerializer.Deserialize<List<mcObject<Int16[]>>>(obj.Value) as IList<baseMcObject>;
                            break;
                        case ("system.int64[]"):
                        case ("system.long[]"):
                             lists = MessagePackSerializer.Deserialize<List<mcObject<Int64>>>(obj.Value) as IList<baseMcObject>;
                            break;
                        case ("system.string"):
                             lists = MessagePackSerializer.Deserialize<List<mcObject<string>>>(obj.Value) as IList<baseMcObject>;
                            break;
                        case ("system.bool"):
                             lists = MessagePackSerializer.Deserialize<List<mcObject<bool>>>(obj.Value) as IList<baseMcObject>;
                            break;
                        case ("system.byte"):
                             lists = MessagePackSerializer.Deserialize<List<mcObject<byte>>>(obj.Value) as IList<baseMcObject>;
                            break;
                        case ("system.float"):
                             lists = MessagePackSerializer.Deserialize<List<mcObject<float>>>(obj.Value) as IList<baseMcObject>;
                            break;
                        case ("system.double"):
                             lists = MessagePackSerializer.Deserialize<List<mcObject<double>>>(obj.Value) as IList<baseMcObject>;
                            break;
                        case ("system.decimal"):
                             lists = MessagePackSerializer.Deserialize<List<mcObject<decimal>>>(obj.Value) as IList<baseMcObject>;
                            break;
                        case("system.decimal[]"):
                             lists = MessagePackSerializer.Deserialize<List<mcObject<decimal[]>>>(obj.Value) as IList<baseMcObject>;
                            break;
                        case ("system.double[]"):
                             lists = MessagePackSerializer.Deserialize<List<mcObject<double[]>>>(obj.Value) as IList<baseMcObject>;
                            break;
                        case ("system.float[]"):
                             lists = MessagePackSerializer.Deserialize<List<mcObject<float[]>>>(obj.Value) as IList<baseMcObject>;
                            break;
                        case ("system.byte[]"):
                             lists = MessagePackSerializer.Deserialize<List<mcObject<byte[]>>>(obj.Value) as IList<baseMcObject>;
                            break;
                        case ("system.bool[]"):
                             lists = MessagePackSerializer.Deserialize<List<mcObject<bool[]>>>(obj.Value) as IList<baseMcObject>;
                            break;
                        case ("system.string[]"):
                             lists = MessagePackSerializer.Deserialize<List<mcObject<string[]>>>(obj.Value) as IList<baseMcObject>;
                            break;

                        case ("system.collections.arraylist"):
                             lists = MessagePackSerializer.Deserialize<List<mcObject<ArrayList>>>(obj.Value) as IList<baseMcObject>;
                            break;
                             case ("system.collections.hashtable"):
                             lists = MessagePackSerializer.Deserialize<List<mcObject<Hashtable>>>(obj.Value) as IList<baseMcObject>;
                            break;
                        default:
                           
                            
                                Type listMcoType = typeof(List<>).MakeGenericType(mcoType);
                            lists = getObject(obj.Value, listMcoType) as IList<baseMcObject>;
                            break;

                    }
                    foreach (var mobj in lists)
                    {
                       
                        var ds = this.getDataShard(mobj.key);
                        ds.add(mobj);
                            }
                }
            }

        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="_baseDataPath">数据基础目录</param>
        /// <param name="_shardCount">分片数量</param>
        /// <param name="saveFileSec">保存文件间隔</param>
        /// <param name="dbname"></param>
        public dataBase(string dbname, string _baseDataPath,int _shardCount,int saveFileSec)
        {
            this.databaseName = dbname;
            this.baseDataPath = _baseDataPath;
            this.shardCount = _shardCount;
            this.saveTimeSpan = saveFileSec;
            initDataBase();
            saveFileTimer = new System.Timers.Timer(this.saveTimeSpan * 1000);
            saveFileTimer.Elapsed += new System.Timers.ElapsedEventHandler(saveToFile);
            saveFileTimer.AutoReset = true;
            saveFileTimer.Start();
        }
        private void saveToFile(Object state, System.Timers.ElapsedEventArgs e)
        {
            Dictionary<string, byte[]> saveDic = new Dictionary<string, byte[]>();
            Dictionary<string, List<baseMcObject>> alltypeListDic = new Dictionary<string, List<baseMcObject>>();
            foreach (var oneshard in this._dataShardsDic.Values)
            {
                var _typeListDic = oneshard.gettypeListDic();

                foreach (var obj in _typeListDic)
                {
                    List<baseMcObject> valuelist;
                    if (!alltypeListDic.ContainsKey(obj.Key))
                    {
                        valuelist = new List<baseMcObject>();
                        alltypeListDic.Add(obj.Key, valuelist);
                    }
                    else
                        valuelist = alltypeListDic[obj.Key];

                    valuelist.AddRange(obj.Value);
                }
            }

            foreach (var obj in alltypeListDic)
            {
                byte[] datalist = MessagePackSerializer.Serialize(obj.Value, MessagePack.Resolvers.ContractlessStandardResolver.Instance);

                saveDic.Add(obj.Key, datalist);
            }
           
            var allbin = MessagePackSerializer.Serialize(saveDic, MessagePack.Resolvers.ContractlessStandardResolver.Instance);
            File.WriteAllBytes(this.savefilename, allbin);
        }
        public dataShard getDataShard(string key)
        {
            var mod = cacheService.getShardId(key, this.shardCount);
            if (!this._dataShardsDic.ContainsKey(mod))
            {
                lock (this._dataShardsDic)
                {
                    if (!this._dataShardsDic.ContainsKey(mod))
                        this._dataShardsDic.AddOrUpdate(mod, new dataShard(),(Key, value) => value);
                }
            }
            return _dataShardsDic[mod];
            
        }
        public object getObject(byte[] bin, Type gtype)
        {
            IEnumerable<Assembly> allAssemblies = Assembly.GetEntryAssembly().GetReferencedAssemblies().Select(Assembly.Load);

            Assembly mockAssembly = (from x in allAssemblies where x.FullName.Contains("MessagePack") select x).FirstOrDefault();
            var MethodType = mockAssembly.GetType("MessagePack.MessagePackSerializer");
            var GenericMethod = MethodType.GetMethod("Deserialize", BindingFlags.Static | BindingFlags.Public, null, new Type[] { typeof(byte[]), typeof(IFormatterResolver) }, null);
            var genMethod = GenericMethod.MakeGenericMethod(gtype);
            var obj = genMethod.Invoke(genMethod, new object[] { bin, MessagePack.Resolvers.ContractlessStandardResolver.Instance });
            return obj;
        }
        public static Type typen(string typeName)
        {
            Type type = null;
            Assembly[] assemblyArray =Assembly.GetEntryAssembly().GetReferencedAssemblies().Select(Assembly.Load).ToArray();
            int assemblyArrayLength = assemblyArray.Length;
            for (int i = 0; i < assemblyArrayLength; ++i)
            {
                type = assemblyArray[i].GetType(typeName);
                if (type != null)
                {
                    return type;
                }
            }

            for (int i = 0; (i < assemblyArrayLength); ++i)
            {
                Type[] typeArray = assemblyArray[i].GetTypes();
                int typeArrayLength = typeArray.Length;
                for (int j = 0; j < typeArrayLength; ++j)
                {
                    if (typeArray[j].Name.Equals(typeName))
                    {
                        return typeArray[j];
                    }
                }
            }
            return type;
        }

        public T getData<T>(string key)
        {
            throw new NotImplementedException();
        }

        public bool addData<T>(string key, T value)
        {
            throw new NotImplementedException();
        }

        public bool addData<T>(string key, T value, TimeSpan expirets)
        {
            throw new NotImplementedException();
        }

        public bool addDataForLock<T>(string key, T value, TimeSpan expirets)
        {
            throw new NotImplementedException();
        }

        public bool removeData(string key)
        {
            throw new NotImplementedException();
        }

        public bool updateData<T>(string key, T value)
        {
            throw new NotImplementedException();
        }

        public bool updateData<T>(string key, T value, TimeSpan expirets)
        {
            throw new NotImplementedException();
        }

        public bool addLock(string key, TimeSpan expirets)
        {
            throw new NotImplementedException();
        }

        public string connect(string username, string passwd, string dbname)
        {
            throw new NotImplementedException();
        }
    }

}
