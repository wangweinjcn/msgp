﻿using System;

namespace msgp.common.Serializing
{
    public interface IJsonSerializer
    {
        string Serialize(object obj);
        object Deserialize(string value, Type type);
        T Deserialize<T>(string value) where T : class;
    }
}
