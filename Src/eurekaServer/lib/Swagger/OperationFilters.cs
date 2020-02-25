   #if (NETCORE || NETSTANDARD2_0 )
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FrmLib.Swagger
{
    public class Consumes : IOperationFilter
    {
        bool IsSimple(Type type)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                // nullable type, check if the nested type is simple.
                return IsSimple(type.GetGenericArguments()[0]);
            }
            return type.IsPrimitive
              || type.IsEnum
              || type.Equals(typeof(string))
              || type.Equals(typeof(decimal));
        }
        public void Apply(Operation operation, OperationFilterContext context)
        {
            string method = context.ApiDescription.HttpMethod;
            var paramid = operation.Parameters.Where(a => a.Name.ToLower() == "id"
              && a.In == "path").FirstOrDefault();
            if (paramid != null)
            {
                var idcount = context.ApiDescription.ActionDescriptor
                     .Parameters.Where(a => a.Name.ToLower() == "id").Count();
                var action = (Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor)context.ApiDescription.ActionDescriptor;

                if (idcount < 1)
                {
                    operation.Parameters.Remove(paramid);
                    
                }
                else
                {
                    var paramid2 = operation.Parameters.Where(a => a.Name.ToLower() == "id"
               && a.In == "query").FirstOrDefault();
                    if (paramid2 != null)
                    {
                        operation.Parameters.Remove(paramid2);
                    }
                }

            }
            if (method.ToLower() == "get")
                return;
            var xlist = context.ApiDescription.ActionAttributes();
            var x= (from a in xlist where a.GetType()==typeof(SwaggerConsumesAttribute) select a).FirstOrDefault();
            var attribute = x as SwaggerConsumesAttribute;
           if (attribute == null )
            {
                var x2 = context.ApiDescription.ParameterDescriptions
                        .FirstOrDefault();
                if (x2 != null)
                {
                 var  x3=  x2.Source.DisplayName;
                    Console.WriteLine("{0},{1}",x2.Name,x3);
                    if (x3.ToLower() == "body" || x3.ToLower()=="bodyandroute")
                    {
                        operation.Consumes.Clear();
                        operation.Consumes.Add("application/json");
                        if (x3.ToLower() == "bodyandroute")
                        {
                            IList<IParameter> parameters = new List<IParameter>();
                            var rpath = context.ApiDescription.RelativePath.ToLower();
                            if (rpath.Contains("/{id}") )
                            {
                                parameters.Add(new NonBodyParameter() { In = "path", Name = "Id", Required = true,Description="路由参数，优先bady中的同名参数" });
                            }
                            BodyParameter bp = new BodyParameter();
                            string paramdesc = "";
                            foreach (var pa in operation.Parameters)
                            {
                                if (pa.Name == "id")
                                {
                                    if((from a in parameters where a.Name=="Id" select a).Count()>0)
                                        continue;
                                    else
                                        paramdesc= string.Format("{0},{1}:{2}", paramdesc, pa.Description, pa.Name);
                                }
                              
                                var obj = pa;
                                var param = (from a in context.ApiDescription.ActionDescriptor.Parameters
                                             where a.Name == pa.Name
                                             select a).FirstOrDefault();
                                if (param != null)
                                {
                                    if (IsSimple(param.ParameterType))
                                    {
                                        paramdesc = string.Format("{0},描述:{1}-名称:{2}[类型:{3}]", paramdesc, obj.Description,param.Name,param.ParameterType.Name);
                                    }
                                    else
                                    {
                                        paramdesc = obj.Description + paramdesc;
                                        bp.Name = obj.Name;                                       
                                        bp.Schema = new Schema() { Ref = "#/definitions/"+param.ParameterType.Name.ToString() };
                                        bp.Required = true;
                                    }
                                }

                            }
                            bp.In = "body";
                            bp.Description = paramdesc.TrimStart(',');

                            parameters.Add(bp);
                            operation.Parameters.Clear();
                            foreach(var obj in parameters)
                                 operation.Parameters.Add(obj);
                        }
                     }
                    else
                    {
                        operation.Consumes.Clear();
                        operation.Consumes.Add("application/x-www-form-urlencoded");
                        foreach (var pa in operation.Parameters)
                        {
                            if (pa.Name == "id")
                                continue;
                            if (pa.In == "header")
                                continue;
                            pa.In = "formData";
                        }
                    }
                }
                return;
            }
            operation.Consumes.Clear();
            operation.Consumes = attribute.ContentTypes.ToList();
             }
        
    }

}
#endif