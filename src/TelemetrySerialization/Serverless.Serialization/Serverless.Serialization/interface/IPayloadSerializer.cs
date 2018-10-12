using System;

namespace Serverless.Serialization
{
    public interface IPayloadSerializer<in T>
    {
        ArraySegment<byte> Serialize(object objectGraph, T payloadType);
        object Deserialize(byte[] byteStream);
    }
}
