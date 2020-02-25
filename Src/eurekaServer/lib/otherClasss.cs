

using FrmLib.Swagger;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;

using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FrmLib.web
{
    public class complexPorertyRemoveFilter : IOperationFilter
    {
        public void Apply(Swashbuckle.AspNetCore.Swagger.Operation operation, OperationFilterContext context)
        {
            var allremoveParams = operation.Parameters.Where(a => a.Name.Contains(".")
               ).ToList();
            foreach (var obj in allremoveParams)
                operation.Parameters.Remove(obj);
        }
    }
    public class ConcurrentstackDic<T>
    {
        /// <summary>
        /// 数据包裹
        /// </summary>
        internal class dataPack
        {
            public T data;
            public object props;
            public int count;
            public dataPack(T value, object p)
            {
                data = value;
                props = p;
                count = 0;
            }

        }
        private int maxLength;

        private ConcurrentStack<string> keyStack;
        private ConcurrentDictionary<string, dataPack> dict;
        /// <summary>
        /// 启动时间
        /// </summary>
        public DateTime startdt { private set; get; }
        /// <summary>
        /// 请求个数
        /// </summary>
        public long getCount { private set; get; }
        /// <summary>
        /// 换出个数
        /// </summary>
        public long removeCount { private set; get; }
        /// <summary>
        /// 缓存对象个数
        /// </summary>
        public long cacheObjectCount { get { return dict.Keys.Count(); } }
        /// <summary>
        /// 缓存数据大小
        /// </summary>
        public long cacheSizeMemory { private set; get; }
        /// <summary>
        /// 初始化次数
        /// </summary>
        public long reInitCount { private set; get; }
        public ConcurrentstackDic(int maxCount)
        {
            maxLength = maxCount;
            keyStack = new ConcurrentStack<string>();
            dict = new ConcurrentDictionary<string, dataPack>();
            getCount = 0;
            removeCount = 0;
            cacheSizeMemory = 0;
            reInitCount = 0;
            startdt = DateTime.Now;
        }
        public bool addOne(string key, T data)
        {
            return addOne(key, data, null);
        }
        public bool addOne(string key, T data, Object props)
        {
            if (dict.Keys.Count > maxLength)
            {
                try
                {
                    string removekey;
                    if (!keyStack.TryPop(out removekey))
                        return false;
                    else
                    {
                        if (!dict.ContainsKey(removekey))
                        {
                            throw new Exception("dict not contain removekey");
                        }
                        else
                        {
                            dataPack value;
                            if (!dict.TryRemove(removekey, out value))
                                throw new Exception("dict remove key error");
                            removeCount++;
                            if (typeof(T) == typeof(byte[]))
                            {
                                cacheSizeMemory = cacheSizeMemory - (value.data as byte[]).Length;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    keyStack = new ConcurrentStack<string>();
                    dict = new ConcurrentDictionary<string, dataPack>();
                    getCount = 0;
                    removeCount = 0;
                    cacheSizeMemory = 0;
                    reInitCount++;
                }
            }
            keyStack.Push(key);
            dict.AddOrUpdate(key, new dataPack(data, props), (a, b) => b);
            if (typeof(T) == typeof(byte[]))
            {
                cacheSizeMemory = cacheSizeMemory + (data as byte[]).Length;
            }

            return true;
        }
        public bool ContainsKey(string key)
        {
            return dict.ContainsKey(key);
        }
        public T getValue(string key)
        {
            if (dict.ContainsKey(key))
            {
                getCount++;
                var dp = dict[key];
                dp.count++;
                return dp.data;
            }
            else
                return default(T);
        }
        public object getProps(string key)
        {
            if (dict.ContainsKey(key))
            {
                return dict[key].props;
            }
            else
                return null;
        }
        public void setLength(int maxCount)
        {
            maxLength = maxCount;
        }
        public int getlength()
        {
            return dict.Keys.Count();
        }
    }
    public struct classViewData
    {
        /// <summary>
        /// 类的字段及默认值
        /// </summary>
        public JObject jsonvalue { get; set; }
        /// <summary>
        /// 字段显示信息
        /// </summary>
        public JObject Disp { get; set; }
        /// <summary>
        /// 字段验证规则
        /// </summary>
        public JObject Rules { get; set; }
        /// <summary>
        /// 不允许编辑字段列表，字符串数组
        /// </summary>
        public JArray DisableFields { get; set; }
    }
    public class SwaggerExcludeFilter : ISchemaFilter
    {
        #region ISchemaFilter Members

        public void Apply(Schema schema, SchemaFilterContext context)
        {
            if (schema?.Properties == null)
                return;
            //var t1 = context.SystemType;
            //Console.WriteLine(t1);
            //if (context.SystemType == typeof(Application.Model.Base.Department))
            //    Console.WriteLine("");
            //var ps = context.SystemType.GetProperties();
            //List<PropertyInfo> pslist = new List<PropertyInfo>();
            //foreach (var onep in ps)
            //{
            //    var xt = onep.GetCustomAttribute<BindNeverAttribute>(true);
            //    if (xt != null)
            //        pslist.Add(onep);
            //}
            var excludedProperties = context.SystemType.GetProperties()
                                         .Where(t =>
                                                t.GetCustomAttribute<BindNeverAttribute>(true)
                                                != null);

            foreach (var excludedProperty in excludedProperties)
            {
                var propertyToRemove =
                schema.Properties.Keys.SingleOrDefault(
                    x => x.ToLower() == excludedProperty.Name.ToLower());

                if (propertyToRemove != null)
                {
                    schema.Properties.Remove(propertyToRemove);
                }
            }
        }

        #endregion
    }

    //添加通用参数，若in='header'则添加到header中,默认query参数
    public class AssignOperationVendorExtensions : IOperationFilter
    {
        public void Apply(Swashbuckle.AspNetCore.Swagger.Operation operation, OperationFilterContext context)
        {
            operation.Parameters = operation.Parameters ?? new List<IParameter>();

            //AllowAnonymousAttribute 允许匿名访问特性标记
            var isAnonymous = operation != null && context != null
                && context.ApiDescription.ControllerAttributes()
                .Any(e => e.GetType() == typeof(AllowAnonymousAttribute))
                || context.ApiDescription.ActionAttributes()
                .Any(e => e.GetType() == typeof(AllowAnonymousAttribute));
            if (!isAnonymous)
            {
                //in query header 
                operation.Parameters.Add(new NonBodyParameter()
                {
                    Name = "Authorization",
                    In = "header",
                    Description = "身份验证票据",
                    Required = false,
                    Type = "string"
                });

            }
        }
    }
    public class AssignSiteKeyExtensions : IOperationFilter
    {
        public void Apply(Swashbuckle.AspNetCore.Swagger.Operation operation, OperationFilterContext context)
        {
            operation.Parameters.Add(new NonBodyParameter()
            {
                Name = "siteKey",
                In = "header",
                Description = "站点Key",
                Required = true,
                Type = "string"
            });
            operation.Parameters.Add(new NonBodyParameter()
            {
                Name = "isOss",
                In = "header",
                Description = "是否是oss系统",
                Required = false,
                Type = "bool"
            });


        }
    }
    //添加标签
    public class ApplyTagDescriptions : IDocumentFilter
    {
        public void Apply(SwaggerDocument swaggerDoc, DocumentFilterContext context)
        {
            swaggerDoc.Tags = baseStartup.swaggerDocTags;
        }
    }
    /// <summary>
    /// 老版本的，
    /// </summary>
    [Obsolete]
    public class SwaggerAddEnumDescriptions : IDocumentFilter
    {
        public void Apply(SwaggerDocument swaggerDoc, DocumentFilterContext context)
        {
            // add enum descriptions to result models
            foreach (KeyValuePair<string, Schema> schemaDictionaryItem in swaggerDoc.Definitions)
            {
                Schema schema = schemaDictionaryItem.Value;
                foreach (KeyValuePair<string, Schema> propertyDictionaryItem in schema.Properties)
                {
                    Schema property = propertyDictionaryItem.Value;
                    IList<object> propertyEnums = property.Enum;
                    if (propertyEnums != null && propertyEnums.Count > 0)
                    {
                        property.Description += DescribeEnum(propertyEnums);
                    }
                }
            }

            // add enum descriptions to input parameters
            if (swaggerDoc.Paths.Count > 0)
            {
                foreach (PathItem pathItem in swaggerDoc.Paths.Values)
                {
                    DescribeEnumParameters(pathItem.Parameters);

                    // head, patch, options, delete left out
                    List<Operation> possibleParameterisedOperations = new List<Operation> { pathItem.Get, pathItem.Post, pathItem.Put };
                    possibleParameterisedOperations.FindAll(x => x != null).ForEach(x => DescribeEnumParameters(x.Parameters));
                }
            }
        }

        private void DescribeEnumParameters(IList<IParameter> parameters)
        {
            //if (parameters != null)
            //{
            //    foreach (IParameter param in parameters)
            //    {
            //        IList<object> paramEnums = param.GetType();
            //        if (paramEnums != null && paramEnums.Count > 0)
            //        {
            //            param.Description += DescribeEnum(paramEnums);
            //        }
            //    }
            //}
        }

        private string DescribeEnum(IList<object> enums)
        {
            List<string> enumDescriptions = new List<string>();
            foreach (object enumOption in enums)
            {
                enumDescriptions.Add(string.Format("{0} = {1}", (int)enumOption, Enum.GetName(enumOption.GetType(), enumOption)));
            }
            return string.Join(", ", enumDescriptions.ToArray());
        }

    }
    /// <summary>
    /// Add enum value descriptions to Swagger
    /// </summary>
    public class EnumDocumentFilter : IDocumentFilter
    {
        /// <inheritdoc />
        public void Apply(SwaggerDocument swaggerDoc, DocumentFilterContext context)
        {
            // add enum descriptions to result models
            foreach (var schemaDictionaryItem in swaggerDoc.Definitions)
            {
                var schema = schemaDictionaryItem.Value;
                foreach (var propertyDictionaryItem in schema.Properties)
                {
                    var property = propertyDictionaryItem.Value;
                    var propertyEnums = property.Enum;
                    if (propertyEnums != null && propertyEnums.Count > 0)
                    {
                        property.Description += DescribeEnum(propertyEnums);
                    }
                }
            }

            if (swaggerDoc.Paths.Count <= 0) return;

            // add enum descriptions to input parameters
            foreach (var pathItem in swaggerDoc.Paths.Values)
            {
                DescribeEnumParameters(pathItem.Parameters);

                // head, patch, options, delete left out
                var possibleParameterisedOperations = new List<Operation> { pathItem.Get, pathItem.Post, pathItem.Put };
                possibleParameterisedOperations.FindAll(x => x != null)
                    .ForEach(x => DescribeEnumParameters(x.Parameters));
            }
        }

        private static void DescribeEnumParameters(IList<IParameter> parameters)
        {
            if (parameters == null) return;

            foreach (var param in parameters)
            {
                if (param is NonBodyParameter nbParam && nbParam.Enum?.Any() == true)
                {
                    param.Description += DescribeEnum(nbParam.Enum);
                }
                else if (param.Extensions.ContainsKey("enum") && param.Extensions["enum"] is IList<object> paramEnums &&
                  paramEnums.Count > 0)
                {
                    param.Description += DescribeEnum(paramEnums);
                }
            }
        }

        private static string DescribeEnum(IEnumerable<object> enums)
        {
            var enumDescriptions = new List<string>();
            Type type = null;
            foreach (var enumOption in enums)
            {
                if (type == null)
                    type = enumOption.GetType();
               
                //enumDescriptions.Add($"{Convert.ChangeType(enumOption, type.GetEnumUnderlyingType())} = {Enum.GetName(type, enumOption)}");
                 enumDescriptions.Add($"{Convert.ChangeType(enumOption, type.GetEnumUnderlyingType())} = {getEnumOptionDesc(enumOption,type)}");
            }

            return $"{Environment.NewLine}{string.Join(Environment.NewLine, enumDescriptions)}";
        }
        private static string getEnumOptionDesc(object enumOption,Type t)
        {
            var name = Enum.GetName(t, enumOption);
            Type typeDescription = typeof(DescriptionAttribute);
            System.Reflection.FieldInfo[] fields = t.GetFields();
            var field = (from x in fields where x.Name == name select x).FirstOrDefault();
            string strText = null;
            if (field !=null && field.FieldType.IsEnum)
            {
               
                object[] arr = field.GetCustomAttributes(typeDescription, true);
                if (arr.Length > 0)
                {
                    DescriptionAttribute aa = (DescriptionAttribute)arr[0];
                    strText = aa.Description;
                }
                else
                {
                    strText = name;
                }
               
            }
            return strText;
        }
    }

}

