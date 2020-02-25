// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace msgp.common.dotnetty
{
    using System;
    using System.Net;

    public class ClientSettings
    {
        public static bool IsSsl
        {
            get
            {
                string ssl = dotneetyCommonHelper.Configuration["ssl"];
                return !string.IsNullOrEmpty(ssl) && bool.Parse(ssl);
            }
        }

        public static IPAddress Host => IPAddress.Parse(dotneetyCommonHelper.Configuration["host"]);

        public static int Port => int.Parse(dotneetyCommonHelper.Configuration["port"]);

        public static int Size => int.Parse(dotneetyCommonHelper.Configuration["size"]);
        public static string ProcessDirectory
        {
            get
            {
#if NETSTANDARD1_3
                return AppContext.BaseDirectory;
#else
                return AppDomain.CurrentDomain.BaseDirectory;
#endif
            }
        }
        public static bool UseLibuv
        {
            get
            {
                string libuv = dotneetyCommonHelper.Configuration["libuv"];
                return !string.IsNullOrEmpty(libuv) && bool.Parse(libuv);
            }
        }
    }
}