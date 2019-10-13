using System;
using System.ComponentModel;

namespace msgp.common.Utilities
{
    /// <summary>A class provides utility methods.
    /// </summary>
    public static class TypeUtils
    {
        /// <summary>Convert the given object to a given strong type.
        /// </summary>
        public static T ConvertType<T>(object value)
        {
            if (value == null)
            {
                return default(T);
            }

            var typeConverter1 = TypeDescriptor.GetConverter(typeof(T));
            if (typeConverter1.CanConvertFrom(value.GetType()))
            {
                return (T)typeConverter1.ConvertFrom(value);
            }

            var typeConverter2 = TypeDescriptor.GetConverter(value.GetType());
            if (typeConverter2.CanConvertTo(typeof(T)))
            {
                return (T)typeConverter2.ConvertTo(value, typeof(T));
            }

            return (T)Convert.ChangeType(value, typeof(T));
        }
        /// <summary>
        /// 16进制字符串转长整形
        /// </summary>
        /// <param name="str">16进制字符串</param>
        /// <returns></returns>
        public static long hexStrToLong(string str)
        {
            long num = long.Parse(str, System.Globalization.NumberStyles.HexNumber);
            return num;
        }
    }
}