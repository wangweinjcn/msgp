using System;
using System.Collections.Generic;
using System.Text;

namespace msgp.mc.model
{
    public interface iDataService
    {
        T getData<T>(string key);
        bool addData<T>(string key, T value);
        bool addData<T>(string key, T value,TimeSpan expirets);
        bool addDataForLock<T>(string key, T value,TimeSpan expirets);
        bool removeData(string key);
        bool updateData<T>(string key, T value);
        bool updateData<T>(string key, T value,TimeSpan expirets);

        bool addLock(string key,TimeSpan expirets);
        string connect(string username, string passwd,string dbname);
        
        
    }
}
