// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Proxy.Comm.http
{
    using System.Text;
    using DotNetty.Buffers;
    using DotNetty.Codecs.Http;
    using DotNetty.Common.Utilities;
    using DotNetty.Transport.Channels;
    using System;
    using DotNetty.Common;
    using DotNetty.Transport.Channels.Embedded;
    using Proxy.Comm.http;
    using System.Collections.Generic;
    using DotNetty.Transport.Bootstrapping;

    using DotNetty.Transport.Channels.Sockets;
    using System.Net;
    using FrmLib.Extend;

    sealed class HttpProxyServerHandler : ChannelHandlerAdapter
    {
        ServerHttpClientHandler pclientHandle;
        MultithreadEventLoopGroup group;
        public HttpProxyServerHandler()
        { 
            group = new MultithreadEventLoopGroup();

        }

      
        private string getclientKey(string clientId, string appkey)
        {
            return string.Format("{0}-{1}", clientId, appkey);
        }
        /// <summary>
        /// 获取客户端连接，如果没有新增
        /// </summary>
        /// <param name="context"></param>
        /// <param name="appkey"></param>
        /// <returns></returns>
        IChannel obtainClientChannel(IChannelHandlerContext context,string appkey)
        {
            if (string.IsNullOrEmpty(appkey))
                return null;
            var ctssc = context.Channel as CustHttpSocketChannel;
            string ClientId;
            if (ctssc != null)
            {
                ClientId = ctssc.ChannelMata.tags["channelKey"].ToString();
                Console.WriteLine("obtainClientChannel CustHttpServerSocketChannel:{0}", ClientId);

            }
            else
            {
                return null;
            }
            var clientkey = getclientKey(ClientId, appkey);
            if (ctssc.allclientchannel.ContainsKey(clientkey))
            {
                if (ctssc.allclientchannel[clientkey].Active)
                    return ctssc.allclientchannel[clientkey];
                else
                {
                    ctssc.removeOneClientChannel(clientkey);
                }
            }
            if (!localRunServer.Instance.ownServer.httpGroupContainKey(appkey))
            {
                
                return null;
            }
            ctssc.mapPortG = localRunServer.Instance.ownServer.getHttpGroupByKey(appkey);
            var mapto = ctssc.mapPortG.selectOutPortMaped();
            if (mapto == null)
                return null;
            
          
            try
            {

                pclientHandle = new ServerHttpClientHandler(context);
                var bsp = new Bootstrap();
                bsp
                    .Group(group)
                    .Channel<CustHttpSocketChannel>()
                    .Option(ChannelOption.TcpNodelay, true)
                     .Option(ChannelOption.SoKeepalive,true)
                     .Option(ChannelOption.ConnectTimeout, TimeSpan.FromSeconds(1))
                    .Handler(new ActionChannelInitializer<ISocketChannel>(c =>
                    {
                        IChannelPipeline pipeline = c.Pipeline;
                        pipeline.AddLast(new HttpRequestEncoder());
                        pipeline.AddLast(pclientHandle);
                    }));
                bsp.RemoteAddress(new IPEndPoint(IPAddress.Parse(mapto.host), int.Parse(mapto.port)));
              
                IChannel clientChannel = AsyncHelpers.RunSync<IChannel>(() => bsp.ConnectAsync());
                var ctsc = clientChannel as CustHttpSocketChannel;
                var meta = ctsc.ChannelMata as CustChannelMetadata;
                meta.tags.AddOrUpdate("channelKey",clientkey,  (key, value) => value);
                meta.tags.AddOrUpdate("serverChannelContext", context, (key, value) => value);
                
                ctsc.outMapPort = mapto;
                ctsc.outMapPort.addCount();
                ctssc.allclientchannel.Add(clientkey, clientChannel);
                ctssc.allclientCounter.Add(clientkey, 1);
                ctssc.bsp_dic.Add(clientkey, bsp);
                return clientChannel;
            }
            catch (Exception e)
            {
                throw new Exception("cann't connect server;" + e.Message);
            }

            finally
            {

            }
        }
        public override void ChannelRegistered(IChannelHandlerContext context)
        {
          
        }

        /// <summary>
        /// 客户端连接过来
        /// </summary>
        /// <param name="context"></param>
        public override void HandlerAdded(IChannelHandlerContext context)
        {
            string ClientId = Guid.NewGuid().ToString();
            var type = context.Channel.GetType();
            var ctssc = context.Channel as CustHttpSocketChannel;
            if (ctssc != null)
            {
                if (ctssc.ChannelMata.tags.ContainsKey("channelKey"))
                {
                    object tmp;
                    ctssc.ChannelMata.tags.TryGetValue("channelKey", out tmp);
                    Console.WriteLine("HandlerAdded  have CustHttpSocketChannel:{0} @httpProxyServerHandle", tmp);
                }
                else
                {
                    Console.WriteLine("HandlerAdded  CustHttpSocketChannel:{0} @httpProxyServerHandle", ClientId);
                    ctssc.ChannelMata.tags.TryAdd("channelKey", ClientId);
                }
            }
            else
            {
                throw new Exception("channel errror");
            }

            base.HandlerAdded(context);
        }
        public override void ChannelInactive(IChannelHandlerContext context)
        {
            var ctssc = context.Channel as CustHttpSocketChannel;
            if (ctssc != null)
            {

                if (ctssc.ChannelMata.tags.ContainsKey("channelKey"))
                {

                    var ClientId  = ctssc.ChannelMata.tags["channelKey"].ToString();
                    Console.WriteLine(" ChannelInactive :{0}  @httpProxyServerHandle", ClientId);

                }
            }

        }

                private void removeOneClient(IChannelHandlerContext context,string appkey)
        {
            string ClientId = "noId";
            var ctssc = context.Channel as CustHttpSocketChannel;
            if (ctssc != null)
            {

                if (ctssc.ChannelMata.tags.ContainsKey("channelKey"))
                {

                    ClientId = ctssc.ChannelMata.tags["channelKey"].ToString();
                    Console.WriteLine(" httpProxyServerHandler:{0}  forward send  msg", ClientId);
                    var key = getclientKey(ClientId, appkey);
                    
                    if (ctssc.bsp_dic.ContainsKey(key))
                        ctssc.bsp_dic.Remove(key);
                    if (ctssc.allclientchannel.ContainsKey(key))
                        ctssc.allclientchannel.Remove(key);
                    if (ctssc.allclientCounter.ContainsKey(key))
                        ctssc.allclientCounter.Remove(key);

                }
            }
            else
            {
                throw new Exception("removeOneClient errror");
            }

        }
        public override void ChannelUnregistered(IChannelHandlerContext context)
        {
            var ctssc = context.Channel as CustHttpSocketChannel;
            ctssc.cleanData();
        }
        public override void ChannelActive(IChannelHandlerContext context)
        {
            
        }
        public override void ChannelRead(IChannelHandlerContext ctx, object message)
        {
            if (message is IFullHttpRequest request)
            {
                try
                {
                    this.Process(ctx, request);
                }
                finally
                {
                    ReferenceCountUtil.Release(message);
                }
            }
            else
            {
                ctx.FireChannelRead(message);
            }
        }
        private string getAppKeyFromUri(string uri)
        {
            var tmp = uri.Trim();
            var pos1 = tmp.IndexOf('/');
            if (pos1 < 0)
                return null;
            var pos2 = tmp.IndexOf('/', pos1 + 1);
            if (pos2 < 0 || pos2 < pos1 + 1)
                return null;
            return tmp.Substring(pos1 + 1, pos2 - pos1-1);
        }
        IByteBuffer encodeFullHttpRequest(IChannelHandlerContext ctx,string appkey, IFullHttpRequest request)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(request.Method.ToString());
            sb.Append(" ");
            int pos = request.Uri.IndexOf("/" + appkey);
            var tmp = request.Uri.Substring(pos + appkey.Length + 1);
            sb.Append(tmp);
            sb.Append(" ");
            sb.Append(request.ProtocolVersion.ToString());
            sb.AppendLine(" ");
            sb.AppendLine(" ");
            var cl = HttpUtil.GetContentLength(request, -1L);
            if (cl == -1 &&request.Content.ReadableBytes>0 )
            {
                request.Headers.Add(HttpHeaderNames.ContentLength, request.Content.ReadableBytes);
            }
            foreach (var obj in request.Headers)
            {
                sb.AppendLine(string.Format("{0}:{1}", obj.Key, obj.Value));
            }
            sb.AppendLine(" ");
            sb.AppendLine(" ");

            var bbs = ctx.Allocator.Buffer();
            bbs.WriteString(sb.ToString(), System.Text.Encoding.UTF8);
            bbs.WriteBytes(request.Content);

            return bbs;
        }
        void Process(IChannelHandlerContext ctx, IFullHttpRequest request)
        {         
            var ctssc = ctx.Channel as CustHttpSocketChannel;
            string ClientId;
            string uri = request.Uri;
            var appkey = getAppKeyFromUri(request.Uri);
            var clientchannel = obtainClientChannel(ctx, appkey) as CustHttpSocketChannel;
            if (clientchannel == null)
            {
                var response = new DefaultFullHttpResponse(DotNetty.Codecs.Http.HttpVersion.Http11, HttpResponseStatus.NotFound, Unpooled.Empty, false);
                ctx.WriteAndFlushAsync(response);
                ctx.CloseAsync();
                return;
            }

            int pos = request.Uri.IndexOf("/" + appkey);
            var tmp = request.Uri.Substring(pos + appkey.Length + 1);

            DefaultFullHttpRequest forwardrequest = new DefaultFullHttpRequest(request.ProtocolVersion,request.Method ,
                  tmp,request.Content);
            var bytecount = request.Content.ReadableBytes + request.Method.ToString().Length + tmp.Length+request.ProtocolVersion.ToString().Length;
            foreach (var obj in request.Headers)
            {
                bytecount = bytecount + obj.Key.Count+3 + obj.Value.Count;
                forwardrequest.Headers.Set(obj.Key, obj.Value);
            }
          
            if (!clientchannel.ChannelMata.tags.ContainsKey("readStart"))
                clientchannel.ChannelMata.tags.AddOrUpdate("readStart", DateTime.Now, (key, value) => value);

            (clientchannel as CustHttpSocketChannel).outMapPort.addRecvBytes(bytecount);

            clientchannel.WriteAndFlushAsync(forwardrequest);

            
        }
        void write404Response(IChannelHandlerContext ctx)
        {
            var response = new DefaultFullHttpResponse(DotNetty.Codecs.Http.HttpVersion.Http11, HttpResponseStatus.NotFound, null, false);
            HttpHeaders headers = response.Headers;
            ctx.WriteAsync(response);
        }
        
        void WriteResponse(IChannelHandlerContext ctx, IByteBuffer buf, ICharSequence contentType, ICharSequence contentLength)
        {
            // Build the response object.
            var response = new DefaultFullHttpResponse(DotNetty.Codecs.Http.HttpVersion.Http11, HttpResponseStatus.NotFound, buf, false);
            HttpHeaders headers = response.Headers;


            // Close the non-keep-alive connection after the write operation is done.
            ctx.WriteAsync(response);
        }

        public override void ExceptionCaught(IChannelHandlerContext context, Exception exception) => context.CloseAsync();

        public override void ChannelReadComplete(IChannelHandlerContext context) => context.Flush();
    }
}
