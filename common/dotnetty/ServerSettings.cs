// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace msgp.common.dotnetty
{
    public static class ServerSettings
    {
        public static bool IsSsl
        {
            get
            {
                string ssl = dotneetyCommonHelper.Configuration["ssl"];
                return !string.IsNullOrEmpty(ssl) && bool.Parse(ssl);
            }
        }

        public static int Port => int.Parse(dotneetyCommonHelper.Configuration["port"]);
        public static int httpPort => int.Parse(dotneetyCommonHelper.Configuration["httpport"]);

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