// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Proxy.Comm
{
    using System;
    using DotNetty.Common.Internal.Logging;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging.Console;
    using DotNetty.Buffers;
    public static class commSetting
    {
        public static  int MAX_FRAME_LENGTH = 1 * 1024;//最大数据长度
        public static int LENGTH_FIELD_OFFSET = 0; //长度域的偏移值
        public static int LENGTH_FIELD_LENGTH = 4; //长度域的长度
        public static  int LENGTH_ADJUSTMENT = 0;  //不包括长度域
        public static  int INITIAL_BYTES_TO_STRIP = 4;//抛弃长度域的长度


        public static int maxPackageSize = 8196;
        public static IByteBuffer[] httpDelimiter()
        {
            return new[]
           {
                Unpooled.WrappedBuffer(new[] { (byte)'\r', (byte)'\n',(byte)'0',(byte)'\r',(byte)'\n' }),
                Unpooled.WrappedBuffer(new[] { (byte)'\n',(byte)'0',(byte)'\n' }),
            };
        }

        public static IByteBuffer[] rpcDelimiter()
        {
            return new[]
           {
                Unpooled.WrappedBuffer(new[] { (byte)'\r', (byte)'\n',(byte)'0',(byte)'\r',(byte)'\n',(byte)'\r',(byte)'\n'  }),
                Unpooled.WrappedBuffer(new[] { (byte)'\n',(byte)'0',(byte)'\n',(byte)'\n' }),
            };
        }
        static commSetting()
        {
            Configuration = new ConfigurationBuilder()
                .SetBasePath(ProcessDirectory)
                .AddJsonFile("appsettings.json")
                .Build();
        }

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

        public static IConfigurationRoot Configuration { get; }

        public static void SetConsoleLogger() => InternalLoggerFactory.DefaultFactory.AddProvider(new ConsoleLoggerProvider((s, level) => true, false));
    }
}