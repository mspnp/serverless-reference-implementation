using Microsoft.Extensions.Logging;

namespace DroneTelemetryFunctionApp
{
    public interface ITelemetryProcessor
    {
        DeviceState Deserialize(byte[] payload, ILogger log);
    }
}