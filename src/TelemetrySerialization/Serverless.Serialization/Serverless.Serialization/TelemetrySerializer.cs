using Serverless.Serialization.Protobuf;
using System;

namespace Serverless.Serialization
{
    public class TelemetrySerializer<T> : ITelemetrySerializer<T>
    {
        IPayloadSerializer<T> _serializer = new ProtobufPayloadSerializer<T>();

        public ArraySegment<byte> Serialize(T message)
        {
            return _serializer.Serialize(message, message);
        }

        T ITelemetrySerializer<T>.Deserialize(byte[] message)
        {
            T restored = (T)_serializer.Deserialize(message);

            // Here you can set the defaults if null is undesirable for nullable fields
            return restored;
        }
    }
}
