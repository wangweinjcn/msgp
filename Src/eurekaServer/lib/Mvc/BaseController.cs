
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Ace.Attributes;
using System.Net;

namespace Ace.Web.Mvc
{

    public abstract class BaseController : Controller
    {
     
        static readonly Type TypeOfCurrent = typeof(BaseController);
        static readonly Type TypeOfDisposableAttribute = typeof(DisposableAttribute);

        

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            this.DisposeMembers();
        }
        /// <summary>
        /// 扫描对象内所有带有 DisposableAttribute 标记并实现了 IDisposable 接口的属性和字段，执行其 Dispose() 方法
        /// </summary>
        void DisposeMembers()
        {
            Type type = this.GetType();

            List<PropertyInfo> properties = new List<PropertyInfo>();
            List<FieldInfo> fields = new List<FieldInfo>();

            Type searchType = type;

            while (true)
            {
                properties.AddRange(searchType.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly).Where(a =>
                {
                    if (typeof(IDisposable).IsAssignableFrom(a.PropertyType))
                    {
                        return a.CustomAttributes.Any(c => c.AttributeType == TypeOfDisposableAttribute);
                    }

                    return false;
                }));

                fields.AddRange(searchType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly).Where(a =>
                {
                    if (typeof(IDisposable).IsAssignableFrom(a.FieldType))
                    {
                        return a.CustomAttributes.Any(c => c.AttributeType == TypeOfDisposableAttribute);
                    }

                    return false;
                }));

                if (searchType == TypeOfCurrent)
                    break;
                else
                    searchType = searchType.GetTypeInfo().BaseType;
            }

            foreach (var pro in properties)
            {
                IDisposable val = pro.GetValue(this) as IDisposable;
                if (val != null)
                    val.Dispose();
            }

            foreach (var field in fields)
            {
                IDisposable val = field.GetValue(this) as IDisposable;
                if (val != null)
                    val.Dispose();
            }
        }


        protected virtual  ContentResult JsonContent(string json, HttpStatusCode hscode = HttpStatusCode.OK)
        {

            if (Response.Headers.ContainsKey("Content-Type"))
                Response.Headers["Content-Type"] = "application/json";
            else
                Response.Headers.Add("Content-Type", "application/json");

            return ContentWithStatus(json,hscode);
        }
        protected virtual ContentResult noContentStatus(HttpStatusCode hscode)
        {
            return ContentWithStatus("", hscode);
        }
        protected ContentResult ContentWithStatus(string str, HttpStatusCode hscode = HttpStatusCode.OK)
        {
            Response.StatusCode = (int)hscode;
            return Content(str);
        }
            protected ContentResult xmlResult(string xml,HttpStatusCode hscode=HttpStatusCode.OK)
        {
            Response.StatusCode = (int)hscode;
            if (Response.Headers.ContainsKey("Content-Type"))
                Response.Headers["Content-Type"] = "application/xml";
            else
                Response.Headers.Add("Content-Type", "application/xml");
            return Content(xml);

        }
        protected ContentResult SuccessData(object data = null)
        {
            Result<object> result = Result.CreateResult<object>(ResultStatus.OK, data);
            return this.JsonContent(result.toJsonString());
        }
        protected ContentResult SuccessMsg(string msg = null)
        {
            Result result = new Result(ResultStatus.OK, msg);
            return this.JsonContent(result.toJsonString());
        }
        protected ContentResult AddSuccessData(object data, string msg = "添加成功")
        {
            Result<object> result = Result.CreateResult<object>(ResultStatus.OK, data);
            result.Msg = msg;
            return this.JsonContent(result.toJsonString());
        }
        protected ContentResult AddSuccessMsg(string msg = "添加成功")
        {
            return this.SuccessMsg(msg);
        }
        protected ContentResult UpdateSuccessMsg(string msg = "更新成功")
        {
            return this.SuccessMsg(msg);
        }
        protected ContentResult DeleteSuccessMsg(string msg = "删除成功")
        {
            return this.SuccessMsg(msg);
        }
        protected ContentResult FailedMsg(string msg = null)
        {
            Result retResult = new Result(ResultStatus.Failed, msg);
            return this.JsonContent(retResult.toJsonString());
        }
        protected ContentResult FailedMsg(ResultStatus status, string msg)
        {
            Result retResult = new Result(status, msg);
            return this.JsonContent(retResult.toJsonString());
        }
        protected ContentResult FailedMsg(int statusCode, string msg)
        {
            Result retResult = new Result(statusCode, msg);
            return this.JsonContent(retResult.toJsonString());
        }
    
    }
       
}

