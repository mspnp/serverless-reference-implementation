using Microsoft.Extensions.Logging;
using Serverless.Serialization;
using Serverless.Serialization.Models;

namespace DroneTelemetryFunctionApp
{
     public class TelemetryProcessor : ITelemetryProcessor
    {
        private readonly ITelemetrySerializer<DroneState> serializer;

        public TelemetryProcessor(ITelemetrySerializer<DroneState> serializer)
        {
            this.serializer = serializer;
        }

        public DeviceState Deserialize(byte[] payload, ILogger log)
        {
            DroneState restored = serializer.Deserialize(payload);

            log.LogInformation("Deserialize message for device ID {DeviceId}", restored.DeviceId);

            var deviceState = new DeviceState();
            deviceState.DeviceId = restored.DeviceId;

            if (restored.Battery != null)
            {
                deviceState.Battery = restored.Battery;
            }
            if (restored.FlightMode != null)
            {
                deviceState.FlightMode = (int)restored.FlightMode;
            }
            if (restored.Position != null)
            {
                deviceState.Latitude = restored.Position.Value.Latitude;
                deviceState.Longitude = restored.Position.Value.Longitude;
                deviceState.Altitude = restored.Position.Value.Altitude;
            }
            if (restored.Health != null)
            {
                deviceState.AccelerometerOK = restored.Health.Value.AccelerometerOK;
                deviceState.GyrometerOK = restored.Health.Value.GyrometerOK;
                deviceState.MagnetometerOK = restored.Health.Value.MagnetometerOK;
            }
            return deviceState;
        }
    }
}
