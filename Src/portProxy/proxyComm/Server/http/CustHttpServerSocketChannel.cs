using DotNetty.Common.Internal.Logging;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Proxy.Comm.http
{
 public class CustHttpServerSocketChannel: TcpServerSocketChannel
    {
        static readonly IInternalLogger Logger = InternalLoggerFactory.GetInstance<TcpServerSocketChannel>();

        static readonly CustChannelMetadata CHANNELMata=new CustChannelMetadata(false);
        public CustChannelMetadata ChannelMata => CHANNELMata;
        public bool ReadPending;

        public CustHttpServerSocketChannel()
           : this(new Socket(SocketType.Stream, ProtocolType.Tcp))
        {
        }

        /// <summary>
        ///     Create a new instance
        /// </summary>
        public CustHttpServerSocketChannel(AddressFamily addressFamily)
            : this(new Socket(addressFamily, SocketType.Stream, ProtocolType.Tcp))
        {
        }

        /// <summary>
        ///     Create a new instance using the given <see cref="Socket"/>.
        /// </summary>
        public CustHttpServerSocketChannel(Socket socket)
            : base( socket)
        {
          
        }
        public override Task ConnectAsync(EndPoint remoteAddress, EndPoint localAddress)
        {
            return base.ConnectAsync(remoteAddress, localAddress);
        }
        protected override IChannelUnsafe NewUnsafe() => new CustHttpServerSocketChannelUnsafe(this);
        sealed class CustHttpServerSocketChannelUnsafe : AbstractSocketUnsafe
        {
            public CustHttpServerSocketChannelUnsafe(TcpServerSocketChannel channel)
                : base(channel)
            {
            }

            new CustHttpServerSocketChannel Channel => (CustHttpServerSocketChannel)this.channel;

            public override void FinishRead(SocketChannelAsyncOperation operation)
            {
                Contract.Assert(this.channel.EventLoop.InEventLoop);

                CustHttpServerSocketChannel ch = this.Channel;
                if ((ch.ResetState(StateFlags.ReadScheduled) & StateFlags.Active) == 0)
                {
                    return; // read was signaled as a result of channel closure
                }
                IChannelConfiguration config = ch.Configuration;
                IChannelPipeline pipeline = ch.Pipeline;
                IRecvByteBufAllocatorHandle allocHandle = this.Channel.Unsafe.RecvBufAllocHandle;
                allocHandle.Reset(config);

                bool closed = false;
                Exception exception = null;

                try
                {
                    Socket connectedSocket = null;
                    try
                    {
                        connectedSocket = operation.AcceptSocket;
                        operation.AcceptSocket = null;
                        operation.Validate();

                        var message = this.PrepareChannel(connectedSocket);

                        connectedSocket = null;
                        
                        ch.ReadPending = false;
                        pipeline.FireChannelRead(message);
                        allocHandle.IncMessagesRead(1);

                        if (!config.AutoRead && !ch.ReadPending)
                        {
                            // ChannelConfig.setAutoRead(false) was called in the meantime.
                            // Completed Accept has to be processed though.
                            return;
                        }

                        while (allocHandle.ContinueReading())
                        {
                            connectedSocket = ch.Socket.Accept();
                            message = this.PrepareChannel(connectedSocket);

                            connectedSocket = null;
                            ch.ReadPending = false;
                            pipeline.FireChannelRead(message);
                            allocHandle.IncMessagesRead(1);
                        }
                    }
                    catch (SocketException ex) when (ex.SocketErrorCode == SocketError.OperationAborted || ex.SocketErrorCode == SocketError.InvalidArgument)
                    {
                        closed = true;
                    }
                    catch (SocketException ex) when (ex.SocketErrorCode == SocketError.WouldBlock)
                    {
                    }
                    catch (SocketException ex)
                    {
                        // socket exceptions here are internal to channel's operation and should not go through the pipeline
                        // especially as they have no effect on overall channel's operation
                        Logger.Info("Exception on accept.", ex);
                    }
                    catch (ObjectDisposedException)
                    {
                        closed = true;
                    }
                    catch (Exception ex)
                    {
                        exception = ex;
                    }

                    allocHandle.ReadComplete();
                    pipeline.FireChannelReadComplete();

                    if (exception != null)
                    {
                        // ServerChannel should not be closed even on SocketException because it can often continue
                        // accepting incoming connections. (e.g. too many open files)

                        pipeline.FireExceptionCaught(exception);
                    }

                    if (closed && ch.Open)
                    {
                        this.CloseSafe();
                    }
                }
                finally
                {
                    // Check if there is a readPending which was not processed yet.
                    if (!closed && (ch.ReadPending || config.AutoRead))
                    {
                        ch.DoBeginRead();
                    }
                }
            }

            TcpSocketChannel PrepareChannel(Socket socket)
            {
                try
                {
                    return new CustHttpSocketChannel(this.channel, socket, true);
                }
                catch (Exception ex)
                {
                    Logger.Warn("Failed to create a new channel from accepted socket.", ex);
                    try
                    {
                        socket.Dispose();
                    }
                    catch (Exception ex2)
                    {
                        Logger.Warn("Failed to close a socket cleanly.", ex2);
                    }
                    throw;
                }
            }
        }
    }
}
