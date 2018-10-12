using System;
using System.IO;
using ProtoBuf;
using ProtoBuf.Meta;

namespace Serverless.Serialization.Protobuf
{
    public class ProtobufPayloadSerializer<T> : IPayloadSerializer<T>
    {
        public ProtobufPayloadSerializer()
        {
            RuntimeTypeModel.Default.AllowParseableTypes = true;
            Serializer.PrepareSerializer<T>();
        }
        public ArraySegment<byte> Serialize(dynamic objectGraph, T payloadType)
        {
            using (var ms = new MemoryStream())
            {
                Serializer.Serialize(ms, payloadType);
                return new ArraySegment<byte>(ms.ToArray());
            }
        }

        public dynamic Deserialize(byte[] byteStream)
        {
            var memoryStream = new MemoryStream(byteStream); 
            return Serializer.Deserialize<T>(memoryStream);
        }
    }
}