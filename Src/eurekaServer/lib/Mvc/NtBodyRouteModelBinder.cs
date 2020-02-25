   #if (NETCORE || NETSTANDARD2_0 )
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ace.Web.Mvc
{
    public class NtBodyModelBinderProvider : IModelBinderProvider
    {
        private readonly IList<IInputFormatter> formatters;
        private readonly IHttpRequestStreamReaderFactory readerFactory;
        private BodyModelBinderProvider defaultProvider;

        public NtBodyModelBinderProvider(IList<IInputFormatter> formatters, IHttpRequestStreamReaderFactory readerFactory)
        {
            this.formatters = formatters;
            this.readerFactory = readerFactory;
            defaultProvider = new BodyModelBinderProvider(formatters, readerFactory);
        }

        public IModelBinder GetBinder(ModelBinderProviderContext context)
        {
            IModelBinder modelBinder = defaultProvider.GetBinder(context);

            // default provider returns null when there is error.So for not null setting our binder
            if (modelBinder != null)
            {
                modelBinder = new NtBodyModelBinder(this.formatters, this.readerFactory);
            }

            return modelBinder;
        }
    }

    public class NtBodyModelBinder : IModelBinder
    {
        private BodyModelBinder defaultBinder;

        public NtBodyModelBinder(IList<IInputFormatter> formatters, IHttpRequestStreamReaderFactory readerFactory) // : base(formatters, readerFactory)
        {
            defaultBinder = new BodyModelBinder(formatters, readerFactory);
        }

        public async Task BindModelAsync(ModelBindingContext bindingContext)
        {
            bindingContext.HttpContext.Request.EnableRewind();
            object baseModel = null;
            object hydratedModel = null;
            var boundProperties = new List<KeyValuePair<string, string>>();
            var modelBinderId = string.Empty;
            var valueProviders = new List<KeyValuePair<string, IValueProvider>>();

            bindingContext.HttpContext.Request.Body.Position = 0;
            await defaultBinder.BindModelAsync(bindingContext);
            hydratedModel = bindingContext.Result.Model;

            if (hydratedModel == null)
            {

                if (hydratedModel == null)
                {
                    try
                    {
                        if (!bindingContext.HttpContext.Items.ContainsKey("bodyParms"))
                        {
                            bindingContext.HttpContext.Request.Body.Position = 0;
                            Stream stream = bindingContext.HttpContext.Request.Body;
                            byte[] buffer = new byte[bindingContext.HttpContext.Request.ContentLength.Value];
                            stream.Read(buffer, 0, buffer.Length);
                            string temp = Encoding.UTF8.GetString(buffer);
                            bindingContext.HttpContext.Items.Add("bodyParms", temp);
                        }
                        var jobj = JObject.Parse(bindingContext.HttpContext.Items["bodyParms"].ToString());
                        foreach (var p in jobj.Properties())
                        {
                            if (string.Equals(p.Name, bindingContext.FieldName, StringComparison.CurrentCultureIgnoreCase))
                            {

                                hydratedModel = Convert.ChangeType(jobj[p.Name].ToString(), bindingContext.ModelType);
                                break;
                            }
                        }
                    }
                    catch (Exception exp)
                    {
                        bindingContext.Result = ModelBindingResult.Failed();
                        return;
                    }


                }
                bindingContext.Result = ModelBindingResult.Success(hydratedModel);
            }
        }
    }
    public class BodyAndRouteBindingSource : BindingSource
    {
        public static readonly BindingSource BodyAndRoute = new BodyAndRouteBindingSource(
            "BodyAndRoute",
            "BodyAndRoute",
            true,
            true
            );

        public BodyAndRouteBindingSource(string id, string displayName, bool isGreedy, bool isFromRequest) : base(id, displayName, isGreedy, isFromRequest)
        {
        }

        public override bool CanAcceptDataFrom(BindingSource bindingSource)
        {
            return bindingSource == Body || bindingSource == this;
        }
       

    }
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property, AllowMultiple = true, Inherited = true)]
    public class FromBodyAndRouteAttribute : Attribute, IBindingSourceMetadata
    {
        public BindingSource BindingSource => BodyAndRouteBindingSource.BodyAndRoute;
    }
    public class BodyAndRouteModelBinder : IModelBinder
    {
        private readonly IModelBinder _bodyBinder;
        private readonly IModelBinder _complexBinder;

        public BodyAndRouteModelBinder(IModelBinder bodyBinder, IModelBinder complexBinder)
        {
            _bodyBinder = bodyBinder;
            _complexBinder = complexBinder;
        }
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
        public async Task BindModelAsync(ModelBindingContext bindingContext)
        {
           
            await _bodyBinder.BindModelAsync(bindingContext);

            if (bindingContext.Result.IsModelSet)
            {
                bindingContext.Model = bindingContext.Result.Model;
            }
           
            {
                try
                {
                    if(!IsSimple(bindingContext.ModelType))
                       await _complexBinder.BindModelAsync(bindingContext);
                }
                catch (Exception ex)
                {

                }
            }
        }
    }
    public class BodyAndRouteModelBinderProvider : IModelBinderProvider
    {
        private NtBodyModelBinderProvider _bodyModelBinderProvider;
        private ComplexTypeModelBinderProvider _complexTypeModelBinderProvider;

        public BodyAndRouteModelBinderProvider(NtBodyModelBinderProvider bodyModelBinderProvider, ComplexTypeModelBinderProvider complexTypeModelBinderProvider)
        {
            _bodyModelBinderProvider = bodyModelBinderProvider;
            _complexTypeModelBinderProvider = complexTypeModelBinderProvider;
        }

        public IModelBinder GetBinder(ModelBinderProviderContext context)
        {
            var bodyBinder = _bodyModelBinderProvider.GetBinder(context);
            var complexBinder = _complexTypeModelBinderProvider.GetBinder(context);

            if (context.BindingInfo.BindingSource != null
                && context.BindingInfo.BindingSource.CanAcceptDataFrom(BodyAndRouteBindingSource.BodyAndRoute))
            {
                return new BodyAndRouteModelBinder(bodyBinder, complexBinder);
            }
            else
            {
                return null;
            }
        }
    }
    public static class BodyAndRouteModelBinderProviderSetup
    {
        public static void InsertBodyAndRouteBinding(this IList<IModelBinderProvider> providers)
        {
            var bodyProvider = providers.Single(provider => provider.GetType() == typeof(NtBodyModelBinderProvider)) as NtBodyModelBinderProvider;
            var complexProvider = providers.Single(provider => provider.GetType() == typeof(ComplexTypeModelBinderProvider)) as ComplexTypeModelBinderProvider;
            
            var bodyAndRouteProvider = new BodyAndRouteModelBinderProvider(bodyProvider, complexProvider);

            providers.Insert(0, bodyAndRouteProvider);
        }
    }


}
#endif