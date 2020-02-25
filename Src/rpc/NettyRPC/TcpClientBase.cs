using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Linq;
using System.Threading;
using System.Net.Security;
using System.Security.Authentication;
using NettyRPC.Tasks;

namespace NettyRPC
{
    /// <summary>
    /// 表示Tcp客户端抽象类
    /// </summary>   
    public abstract class TcpClientBase : IWrapper, IDisposable
    {
        /// <summary>
        /// 会话对象
        /// </summary>
        private ISession session { get; set; }

        /// <summary>
        /// 获取远程终结点
        /// </summary>
        public EndPoint RemoteEndPoint
        {
            get
            {
                return this.session.RemoteEndPoint;
            }
        }

        /// <summary>
        /// 获取是否已连接到远程端
        /// </summary>
        public bool IsConnected
        {
            get
            {
                return this.session.IsConnected;
            }
        }

        /// <summary>
        /// 获取用户附加数据
        /// </summary>
        public ITag Tag
        {
            get
            {
                return this.session.Tag;
            }
        }

        /// <summary>
        /// 获取或设置断线自动重连的时间间隔 
        /// 设置为TimeSpan.Zero表示不自动重连
        /// </summary>
        public TimeSpan ReconnectPeriod { get; set; }

        /// <summary>
        /// 获取或设置心跳包时间间隔
        /// 设置为TimeSpan.Zero表示不发心跳包
        /// </summary>
        public TimeSpan KeepAlivePeriod { get; set; }

        /// <summary>
        /// Tcp客户端抽象类
        /// </summary>
        public TcpClientBase()
        {
            
        }

        /// <summary>
        /// SSL支持的Tcp客户端抽象类
        /// </summary>
        /// <param name="targetHost">目标主机</param>
        /// <exception cref="ArgumentNullException"></exception>
        public TcpClientBase(string targetHost)
            : this(targetHost, null)
        {
        }

        /// <summary>
        /// SSL支持的Tcp客户端抽象类
        /// </summary>  
        /// <param name="targetHost">目标主机</param>
        /// <param name="certificateValidationCallback">远程证书验证回调</param>
        /// <exception cref="ArgumentNullException"></exception>
        public TcpClientBase(string targetHost, RemoteCertificateValidationCallback certificateValidationCallback)
        {

        }

       
        /// <summary>
        /// 连接到远程端
        /// </summary>
        /// <param name="host">域名或ip地址</param>
        /// <param name="port">远程端口</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="AuthenticationException"></exception>
        /// <returns></returns>
        public Task<SocketError> ConnectAsync(string host, int port)
        {
            return this.ConnectAsync(new DnsEndPoint(host, port));
        }

        /// <summary>
        /// 连接到远程终端       
        /// </summary>
        /// <param name="ip">远程ip</param>
        /// <param name="port">远程端口</param>
        /// <exception cref="AuthenticationException"></exception>
        /// <returns></returns>
        public Task<SocketError> ConnectAsync(IPAddress ip, int port)
        {
            return this.ConnectAsync(new IPEndPoint(ip, port));
        }

        /// <summary>
        /// 连接到远程终端 
        /// </summary>
        /// <param name="remoteEndPoint">远程ip和端口</param> 
        /// <exception cref="AuthenticationException"></exception>
        /// <returns></returns>
        public virtual async Task<SocketError> ConnectAsync(EndPoint remoteEndPoint)
        {
            var error = await this.ConnectInternalAsync(remoteEndPoint);
            if (error == SocketError.Success)
            {
               
            }

            this.OnConnected(error);
            return error;
        }

        /// <summary>
        /// 连接到远程终端 
        /// </summary>
        /// <param name="remoteEndPoint">远程ip和端口</param> 
        /// <returns></returns>
        private async Task<SocketError> ConnectInternalAsync(EndPoint remoteEndPoint)
        {
            if (remoteEndPoint == null)
            {
                throw new ArgumentNullException();
            }

            if (this.IsConnected == true)
            {
                return SocketError.IsConnected;
            }

            var addressFamily = AddressFamily.InterNetwork;
            if (remoteEndPoint.AddressFamily != AddressFamily.Unspecified)
            {
                addressFamily = remoteEndPoint.AddressFamily;
            }
            var socket = new Socket(addressFamily, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                await Task.Factory.FromAsync(socket.BeginConnect, socket.EndConnect, remoteEndPoint, null);
              
                return SocketError.Success;
            }
            catch (SocketException ex)
            {
                socket.Dispose();
                return ex.SocketErrorCode;
            }
        }


        /// <summary>
        /// 连接到远程端
        /// </summary>
        /// <param name="host">域名或ip地址</param>
        /// <param name="port">远程端口</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="AuthenticationException"></exception>
        /// <returns></returns>
        public SocketError Connect(string host, int port)
        {
            return this.Connect(new DnsEndPoint(host, port));
        }

        /// <summary>
        /// 连接到远程终端       
        /// </summary>
        /// <param name="ip">远程ip</param>
        /// <param name="port">远程端口</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="AuthenticationException"></exception>
        /// <returns></returns>
        public SocketError Connect(IPAddress ip, int port)
        {
            return this.Connect(new IPEndPoint(ip, port));
        }

        /// <summary>
        /// 连接到远程端
        /// </summary>
        /// <param name="remoteEndPoint">远程端</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="AuthenticationException"></exception>
        /// <returns></returns>
        public virtual SocketError Connect(EndPoint remoteEndPoint)
        {
            var error = this.ConnectInternal(remoteEndPoint);
            if (error == SocketError.Success)
            {
              
            }

            this.OnConnected(error);
            return error;
        }

        /// <summary>
        /// 连接到远程端
        /// </summary>
        /// <param name="remoteEndPoint">远程端</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <returns></returns>
        private SocketError ConnectInternal(EndPoint remoteEndPoint)
        {
            if (remoteEndPoint == null)
            {
                throw new ArgumentNullException();
            }

            if (this.IsConnected == true)
            {
                return SocketError.IsConnected;
            }

            var addressFamily = AddressFamily.InterNetwork;
            if (remoteEndPoint.AddressFamily != AddressFamily.Unspecified)
            {
                addressFamily = remoteEndPoint.AddressFamily;
            }
            var socket = new Socket(addressFamily, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                socket.Connect(remoteEndPoint);
              
                return SocketError.Success;
            }
            catch (SocketException ex)
            {
                socket.Dispose();
                return ex.SocketErrorCode;
            }
        }

       

       

        /// <summary>
        /// 与服务器连接之后，将触发此方法
        /// </summary>
        /// <param name="error">连接状态码</param>
        protected virtual void OnConnected(SocketError error)
        {
        }

        /// <summary>
        /// 当与服务器断开连接后，将触发此方法
        /// </summary>       
        protected virtual void OnDisconnected()
        {
        }

        /// <summary>
        /// 当接收到远程端的数据时，将触发此方法   
        /// </summary>       
        /// <param name="streamReader">接收到的数据读取器</param>
        /// <returns></returns>
        protected abstract Task OnReceiveAsync(ISessionStreamReader streamReader);


        /// <summary>
        /// 同步发送数据
        /// </summary>
        /// <param name="buffer">数据</param>  
        /// <exception cref="ArgumentNullException"></exception>        
        /// <exception cref="SocketException"></exception>
        /// <returns></returns>
        public virtual int Send(byte[] buffer)
        {
            return this.session.Send(buffer);
        }

        /// <summary>
        /// 异步发送数据
        /// </summary>
        /// <param name="byteRange">数据范围</param>  
        /// <exception cref="ArgumentNullException"></exception>        
        /// <exception cref="SocketException"></exception>
        /// <returns></returns>
        public virtual int Send(ArraySegment<byte> byteRange)
        {
            return this.session.Send(byteRange);
        }

      

        /// <summary>
        /// 还原到包装前
        /// </summary>
        /// <returns></returns>
        public ISession UnWrap()
        {
            return this.session;
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public virtual void Dispose()
        {
            this.session.Dispose();
        }



        /// <summary>
        /// 循环尝试间隔地重连
        /// </summary>
        private async void LoopReconnectAsync()
        {
            if (this.ReconnectPeriod <= TimeSpan.Zero)
            {
                return;
            }

            var state = await this.ReConnectAsync().ConfigureAwait(false);
            if (state == true)
            {
                return;
            }

            await Task.Delay(this.ReconnectPeriod).ConfigureAwait(false);
            this.LoopReconnectAsync();
        }

        /// <summary>
        /// 尝试重连
        /// </summary>
        /// <returns></returns>
        private async Task<bool> ReConnectAsync()
        {
            try
            {
                var state = await this.ConnectAsync(this.RemoteEndPoint);
                return state == SocketError.Success;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}