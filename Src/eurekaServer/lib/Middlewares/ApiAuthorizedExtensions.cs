#if NETCORE
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;

namespace Ace.Web.Mvc.Middlewares
{
    public static class ApiAuthorizedExtensions
    {
        public static IApplicationBuilder UseApiAuthorized(this IApplicationBuilder builder, Action<IRouteBuilder> configureRoutes)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }
            var routes = new RouteBuilder(builder)
            {
                DefaultHandler = builder.ApplicationServices.GetRequiredService<MvcRouteHandler>(),
            };
            configureRoutes(routes);
            routes.Routes.Insert(0, AttributeRouting.CreateAttributeMegaRoute(builder.ApplicationServices));
            var router = routes.Build();
            return builder.UseMiddleware<ApiAuthorizedMiddleware>(router);
        }

        public static IApplicationBuilder UseApiAuthorized(this IApplicationBuilder builder, ApiAuthorizedOptions options, Action<IRouteBuilder> configureRoutes)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }
            var routes = new RouteBuilder(builder)
            {
                DefaultHandler = builder.ApplicationServices.GetRequiredService<MvcRouteHandler>(),
            };
            configureRoutes(routes);
            routes.Routes.Insert(0, AttributeRouting.CreateAttributeMegaRoute(builder.ApplicationServices));
            var router = routes.Build();
            return builder.UseMiddleware<ApiAuthorizedMiddleware>(Options.Create(options),router);
        }
    }
}
#endif