using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json.Linq;
namespace Ace
{
    public class Result<T> : Result, IJsonSerialize
    {
        public Result()
        {
        }
        public Result(ResultStatus status)
            : base(status)
        {
        }
        public Result(ResultStatus status, T data)
            : base(status)
        {
            this.Data = data;
        }
        public string toJsonString()
        {
            JObject jobj = new JObject();
            if (this.Data == null)
            {
                
                jobj.Add("Status", (int)this.Status);

                jobj.Add("Data", null);
                return jobj.ToString();
            }          
            
               
                jobj.Add("Status", (int)this.Status);

                jobj.Add("Data", JObject.FromObject( this.Data));
                return jobj.ToString();
            
      }

        public T Data { get; set; }
    }
}
