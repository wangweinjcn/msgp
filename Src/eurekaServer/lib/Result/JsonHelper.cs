using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ace
{
    public static class JsonHelper
    {
        public static string Serialize(this object obj)
        {
            JsonSerializerSettings jsSettings = new JsonSerializerSettings();
            jsSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            return JsonConvert.SerializeObject(obj);
        }
        public static T Deserialize<T>(this string json)
        {
            return json == null ? default(T) : JsonConvert.DeserializeObject<T>(json);
        }
        public static List<T> DeserializeToList<T>(this string json)
        {
            return json == null ? default(List<T>) : JsonConvert.DeserializeObject<List<T>>(json);
        }
    }
}
