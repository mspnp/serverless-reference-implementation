using Azure.Messaging.EventHubs;

namespace DroneTelemetryFunctionApp
{
    public class DeadLetterMessage
    {
        public string? Exception { get; set; }
        public byte[]? EventData { get; set; }
        public DeviceState? DeviceState { get; set; }
    }
}
