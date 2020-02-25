// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Proxy.Comm
{
    using System.Net;

    public class ClientSettings
    {

        public static bool IsSsl
        {
            get
            {
                string ssl = commSetting.Configuration["ssl"];
                return !string.IsNullOrEmpty(ssl) && bool.Parse(ssl);
            }
        }
        public static IPAddress Host => IPAddress.Parse(commSetting.Configuration["host"]);

        public static int Port => int.Parse(commSetting.Configuration["port"]);

        public static int Size => int.Parse(commSetting.Configuration["size"]);

        public static bool UseLibuv
        {
            get
            {
                string libuv = commSetting.Configuration["libuv"];
                return !string.IsNullOrEmpty(libuv) && bool.Parse(libuv);
            }
        }
    }
}