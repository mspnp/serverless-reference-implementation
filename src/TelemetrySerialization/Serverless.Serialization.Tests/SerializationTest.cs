using Serverless.Serialization.Models;
using Xunit;

namespace Serverless.Serialization.Tests
{
    public class SerializationTest
    {
        private readonly ITelemetrySerializer<DroneState> _serializer = new TelemetrySerializer<DroneState>();

        [Fact]
        public void Serialized_Deserialized_DroneState_Are_Identical()
        {
            DroneState droneState = new DroneState(){
                DeviceId = "some_random_device_id",
                FlightMode = DroneFlightMode.Inflight,
                Battery = 0.99,
                Position = (44.5, 44.6, 500),
                Health = (true, false, false),
                IsKeyFrame = true
            };

            var byteArraySeg = _serializer.Serialize(droneState);
            DroneState restored = _serializer.Deserialize(byteArraySeg.Array);
            Assert.Equal(droneState.DeviceId, restored.DeviceId);
            Assert.Equal(droneState.FlightMode, restored.FlightMode);
            Assert.Equal(droneState.Battery, restored.Battery);  
            Assert.Equal(droneState.IsKeyFrame, restored.IsKeyFrame);
            Assert.Equal(droneState.Position, restored.Position);
            Assert.Equal(droneState.Health, restored.Health);
        }
    }
}
