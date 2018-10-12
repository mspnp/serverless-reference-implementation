using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Azure.EventHubs;

namespace DroneTelemetryFunctionApp
{
    public class DeadLetterMessage
    {
        public Exception Exception { get; set; }
        public EventData EventData { get; set; }
        public DeviceState DeviceState { get; set; }
    }
}
