using Azure.Messaging.EventHubs;
using System;

namespace DroneTelemetryFunctionApp
{
    public class DeadLetterMessage
    {
        public Exception Exception { get; set; }
        public EventData EventData { get; set; }
        public DeviceState DeviceState { get; set; }
    }
}
