using System;
using System.Runtime.Serialization;
using UnityEngine;

[Serializable]
public class StringStringDictionary : SerializableDictionary<string, string>
{
    public StringStringDictionary()
    {
    }

    protected StringStringDictionary(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}

[Serializable]
public class ObjectColorDictionary : SerializableDictionary<UnityEngine.Object, Color>
{
    public ObjectColorDictionary()
    {
    }

    protected ObjectColorDictionary(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}

[Serializable]
public class ColorArrayStorage : SerializableDictionary.Storage<Color[]>
{
    public ColorArrayStorage()
    {
    }

    protected ColorArrayStorage(SerializationInfo info, StreamingContext context)
    {
    }
}

[Serializable]
public class StringColorArrayDictionary : SerializableDictionary<string, Color[], ColorArrayStorage>
{
    public StringColorArrayDictionary()
    {
    }

    protected StringColorArrayDictionary(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}