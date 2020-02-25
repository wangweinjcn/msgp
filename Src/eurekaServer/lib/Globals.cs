   #if (NETCORE || NETSTANDARD2_0 )
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ace
{
    public static class Globals
    {
        public static IServiceProvider ServiceProvider { get; set; }
        public static IConfigurationRoot Configuration { get; set; }
    }
}
#endif