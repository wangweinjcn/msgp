using MessagePack;
using System;
using System.Collections;

namespace msgp.mc.model
{
    public enum datatypeEnum
    {
        dtMcObject=0,
        boolMcObject=10,
        intMcObject=20,
        doubleMcObject=30,
        decimalMcObject=40,
        stringMcObject=50,
        listMcObject=60,
        mapMcObject=70,
        dynamicMcObject=80,
            unknown=-1
        
    }
    public class mcObject<T>:baseMcObject
    {
        public T data;
        public mcObject()
        {
            if (!typeof(T).IsValueType)
            {
                object[] paramObject = new object[] { };
                data = (T)Activator.CreateInstance(typeof(T), paramObject);
            }
        }
        
        public override Type getDataType()
        {
            return typeof(T);

        }
    }
   
    public  class baseMcObject
    {
        [Key(0)]
        public string key;
        [Key(1)]
        public DateTime createdt;
        [Key(2)]
        public DateTime expirets;
        [Key(4)]
        public bool isock;
        [IgnoreMember]
        public long visistCount;

        public virtual Type getDataType()
        {
            return typeof(object);
        }
       
    }
    

     [MessagePackObject]
    public class testobject<T>
    { [Key(0)]
        public int f1 = 0;
         [Key(1)]
        public float f2 = 1.0F;
         [Key(2)]
        public string f3 = "test";
         [Key(3)]
        public T ft;
        public testobject()
            {
            if (!typeof(T).IsValueType)
            {
                object[] paramObject = new object[] { };
                ft = (T)Activator.CreateInstance(typeof(T), paramObject);
            }
        }

    }
      [MessagePackObject]
    public class testobject2
    {
        [Key(0)]
        public string f1="tobject2";
        [Key(1)]
        public string f2="test object2";
    }
}
