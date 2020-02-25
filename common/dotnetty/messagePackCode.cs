using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Transport.Channels;

using System;
using System.Collections.Generic;
using System.Text;

namespace msgp.common.dotnetty
{
    class messagePackCode
    {
    }
    public class CommonEncoder<T> : MessageToByteEncoder<T>
    {
        protected override void Encode(IChannelHandlerContext context, T message, IByteBuffer output)
        {
            //序列化类
            byte[] messageBytes = MessagePack.MessagePackSerializer.Serialize(message);
            IByteBuffer initialMessage = Unpooled.Buffer(messageBytes.Length);
            initialMessage.WriteBytes(messageBytes);

            output.WriteBytes(initialMessage);
        }
    }
    public class CommonDecoder<T> : ByteToMessageDecoder
    {
        protected override void Decode(IChannelHandlerContext context, IByteBuffer input, List<object> output)
        {
            byte[] array = new byte[input.ReadableBytes];
            input.GetBytes(input.ReaderIndex, array, 0, input.ReadableBytes);
            input.Clear();
            var temp = MessagePack.MessagePackSerializer.Deserialize<T>(array);
            output.Add(temp);
        }
    }
}
