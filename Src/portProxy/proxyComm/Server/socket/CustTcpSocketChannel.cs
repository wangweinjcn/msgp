// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Proxy.Comm.socket
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading.Tasks;
    using DotNetty.Buffers;
    using DotNetty.Common.Concurrency;
    using DotNetty.Transport.Bootstrapping;
    using DotNetty.Transport.Channels;
    using DotNetty.Transport.Channels.Sockets;
    using Proxy.Comm.model;


    /// <summary>
    ///     <see cref="ISocketChannel" /> which uses Socket-based implementation.
    /// </summary>
    public class CustTcpSocketChannel : TcpSocketChannel
    {
        public bool ReadPendingCust { get; set; }
        readonly ISocketChannelConfiguration config;
        CustChannelMetadata CHANNELMata = new CustChannelMetadata(false);
        public CustChannelMetadata ChannelMata => CHANNELMata;

        public outMapPort outMapPort;
        internal mapPortGroup mapPortG;
        /// <summary>
        /// 
        /// </summary>
        internal Dictionary<string, Bootstrap> bsp_dic;
        /// <summary>
        /// 服务端channel对应的客户端链接
        /// 键值为channel中的tag值
        /// </summary>
        internal Dictionary<string, IChannel> allclientchannel;
        /// <summary>
        /// 客户端计数，host+port为键值
        /// </summary>
        internal Dictionary<string, int> allclientCounter;
        /// <summary>Create a new instance</summary>
        public CustTcpSocketChannel()
            : this(new Socket(SocketType.Stream, ProtocolType.Tcp))
        {
        }

        /// <summary>Create a new instance</summary>
        public CustTcpSocketChannel(AddressFamily addressFamily)
            : this(new Socket(addressFamily, SocketType.Stream, ProtocolType.Tcp))
        {
        }

        /// <summary>Create a new instance using the given <see cref="ISocketChannel" />.</summary>
        public CustTcpSocketChannel(Socket socket)
            : this(null, socket)
        {
        }

        /// <summary>Create a new instance</summary>
        /// <param name="parent">
        ///     the <see cref="IChannel" /> which created this instance or <c>null</c> if it was created by the
        ///     user
        /// </param>
        /// <param name="socket">the <see cref="ISocketChannel" /> which will be used</param>
        public CustTcpSocketChannel(IChannel parent, Socket socket)
            : this(parent, socket,false)
        {

        }
        internal CustTcpSocketChannel(IChannel parent, Socket socket, bool connected)
            : base(parent, socket)
        {
            bsp_dic = new Dictionary<string, Bootstrap>();
            allclientchannel = new Dictionary<string, IChannel>();
            allclientCounter = new Dictionary<string, int>();
            this.config = new CustTcpSocketChannelConfig(this, socket);
            if (connected)
            {
                this.OnConnected();
            }
        }
        void OnConnected()
        {
            this.SetState(StateFlags.Active);

            // preserve local and remote addresses for later availability even if Socket fails
            this.CacheLocalAddress();
            this.CacheRemoteAddress();
        }
        public void cleanData()
        {
            foreach (var obj in this.allclientchannel.Values)
            {
                if (obj != null)
                    obj.CloseSafe();
            }
            this.allclientchannel.Clear();
            this.allclientCounter.Clear();
            this.bsp_dic.Clear();

        }
        sealed class CustTcpSocketChannelConfig : DefaultSocketChannelConfiguration
        {
            volatile int maxBytesPerGatheringWrite = int.MaxValue;

            public CustTcpSocketChannelConfig(TcpSocketChannel channel, Socket javaSocket)
                : base(channel, javaSocket)
            {
                this.CalculateMaxBytesPerGatheringWrite();
            }

            public int GetMaxBytesPerGatheringWrite() => this.maxBytesPerGatheringWrite;

            public override int SendBufferSize
            {
                get => base.SendBufferSize;
                set
                {
                    base.SendBufferSize = value;
                    this.CalculateMaxBytesPerGatheringWrite();
                }
            }

            void CalculateMaxBytesPerGatheringWrite()
            {
                // Multiply by 2 to give some extra space in case the OS can process write data faster than we can provide.
                int newSendBufferSize = this.SendBufferSize << 1;
                if (newSendBufferSize > 0)
                {
                    this.maxBytesPerGatheringWrite = newSendBufferSize;
                }
            }

            protected override void AutoReadCleared() => ((CustTcpSocketChannel)this.Channel).ClearReadPending();
        }

    }
}