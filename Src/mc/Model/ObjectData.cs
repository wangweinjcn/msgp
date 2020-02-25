using MessagePack;
using System;
using System.Collections;

namespace msgp.mc.model
{
    public enum datatypeEnum
    {
      
        system_int = 0,
        system_int32 = 10,
        system_int16 = 20,
        system_int64 = 30,
        system_long = 40,
        system_int_array= 50,
        system_int32_array= 60,
        system_int16_array= 70,
        system_int64_array= 80,
        system_long_array = 90,
        system_string = 100,
        system_bool = 110,
        system_byte = 120,
        system_float = 130,
        system_double = 140,
        system_decimal = 150,
        system_decimal_array= 160,
        system_double_array= 170,
        system_float_array= 180,
        system_byte_array= 190,
        system_bool_array= 200,
        system_string_array= 210,
        system_collections_arraylist = 220,
        system_collections_hashtable = 230,

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
        public  static object  toObject()
        {

            return null;

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
    


    public class testobject<T>
    {
        public string fttype;
        public int f1 = 0;
        public float f2 = 1.0F;
        public string f3 = "test";
        public T ft;
        public testobject()
            {
            if (!typeof(T).IsValueType)
            {
                object[] paramObject = new object[] { };
                ft = (T)Activator.CreateInstance(typeof(T), paramObject);
               
            }
            fttype = typeof(string).ToString();
        }

    }
    
    public class testobject2
    {
        public string f1="tobject2";
        public string f2="test object2";
    }
}
