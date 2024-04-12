using Azure.Messaging.EventHubs;

namespace DroneTelemetryFunctionApp
{
    public class DeadLetterMessage
    {
        public string? Issue { get; set; }
        public byte[]? MessageBody { get; set; }
        public DeviceState? DeviceState { get; set; }
    }
}
