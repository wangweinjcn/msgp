using System;
using System.Collections.Generic;
using System.Text;
using DotNetty.Buffers;
using MessagePack;
namespace msgp.mc.model
{

    public enum commandEnum
    {
        //连接
        connect=0,
        //增加
        add=10,
        //
        update=20,
        //
        delete=30,
        //锁定
        addlock = 40,
        //
        read = 100,
        //
        unknown =-1,

    }

    public enum commandStatus
    {
        //命令生成
        upload=0,
        //执行中
        executing=1,
        //执行成功
        ok =2,
        //执行识别
        fail =3,
        //
        unknown=-1
    }

    [MessagePackObject]
    public class cmdMessageLogUnit
    {
        [Key(0)]
        public commandEnum cmd;
        [Key(1)]
        public string cmdNum;
        [Key(2)]
        public Dictionary<string, string> commandParams;
        [Key(3)]
        public DateTime execTime;
    }
    [MessagePackObject]
   public class cmdMessage
    {
        [IgnoreMember]
        public static string cmdEndFlag="";
        /// <summary>
        /// 命令
        /// </summary>
        [Key(0)]
        public commandEnum cmd;
        /// <summary>
        /// 命令序号
        /// </summary>
        [Key(1)]
        public string cmdNum;
        /// <summary>
        /// 命令执行状态
        /// </summary>
        [Key(2)]
        public commandStatus exeStatus;
      
        /// <summary>
        /// 数据(mcObject的序列化数据
        /// </summary>
        [Key(3)]
        public byte[] data;
        /// <summary>
        /// 命令执行信息
        /// </summary>
        [Key(4)]
        public string msg;
        /// <summary>
        /// 数据类型
        /// </summary>
        [Key(6)]
        public datatypeEnum datatype;

        public cmdMessage()
        {
            
            msg = null;
            data = null;
            exeStatus = commandStatus.unknown;
            cmdNum = Guid.NewGuid().ToString();
            cmd = commandEnum.unknown;
        }
        public void setData<T>(mcObject<T> _data)
        {
            this.data = MessagePack.MessagePackSerializer.Serialize(_data,MessagePack.Resolvers.ContractlessStandardResolver.Instance);
        }
        public mcObject<T> getData<T>()
        {
           return  MessagePack.MessagePackSerializer.Deserialize<mcObject<T>>(this.data,MessagePack.Resolvers.ContractlessStandardResolver.Instance);
        }
        public static bool Parse(IByteBuffer streamReader, out cmdMessage packet)
        {
            packet = null;
            const int packetMinSize = 32;
            var allpakagesize = streamReader.ReadableBytes;
            if (streamReader.ReadableBytes < packetMinSize )
            {
                return false;
            }


            var totalBytes = streamReader.ReadInt();//读取数据总长度字段
            if (totalBytes < packetMinSize)
            {
                return false;
            }
          

            // 数据包未接收完整
            if (allpakagesize < totalBytes)
            {
                return false;
            }
            byte[] objbytes = new byte[totalBytes - 32];
            streamReader.ReadBytes( objbytes, 0, totalBytes - 32);
            packet = MessagePack.MessagePackSerializer.Deserialize<cmdMessage>(objbytes);
            return true;

        }
        public IByteBuffer ToByteBuffer()
        {
            var messagebytes =MessagePack.MessagePackSerializer.Serialize(this);            
           
            Int32 TotalBytes = messagebytes.Length+32;
            var bb = Unpooled.Buffer(TotalBytes);
          
            bb.WriteInt(TotalBytes);
            bb.WriteBytes(messagebytes);
          
            return bb;
        }
    }
}
