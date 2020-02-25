   #if (NETCORE || NETSTANDARD2_0 )
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Ace.Web.Mvc
{
    internal interface IRouteMatcher
    {
        RouteValueDictionary Match(string routeTemplate, string requestPath, IQueryCollection query);
    }
}
#endif