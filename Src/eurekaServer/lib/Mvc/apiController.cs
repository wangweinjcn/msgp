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


namespace Ace.Web.Mvc
{
#if (NETCORE || NETSTANDARD2_0)
    public abstract class apiController : BaseController
    {
    }
#endif

}
