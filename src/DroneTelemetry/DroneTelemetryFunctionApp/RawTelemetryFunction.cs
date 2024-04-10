using Azure.Messaging.EventHubs;
using Microsoft.ApplicationInsights;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace DroneTelemetryFunctionApp
{
    public class RawTelemetryFunction
    {
        private readonly ILogger<RawTelemetryFunction> _logger;
        private readonly ITelemetryProcessor _telemetryProcessor;
        private readonly TelemetryClient _telemetryClient;

        public RawTelemetryFunction(ILogger<RawTelemetryFunction> logger, ITelemetryProcessor telemetryProcessor, TelemetryClient telemetryClient)
        {
            _logger = logger;
            _telemetryProcessor = telemetryProcessor;
            _telemetryClient = telemetryClient;
        }

        [Function(nameof(RawTelemetryFunction))]
        public RawTelemetryOutputType Run([EventHubTrigger("%EventHubName%", Connection = "EventHubConnection")] EventData[] messages,
            FunctionContext context)
        {
            _telemetryClient.GetMetric("EventHubMessageBatchSize").TrackValue(messages.Length);

            var rawTelemetryOutputType = new RawTelemetryOutputType();
            foreach (var message in messages)
            {
                try
                {
                    var state = _telemetryProcessor.Deserialize(message.Body.ToArray(), _logger);
                    rawTelemetryOutputType.DeviceState = new { id = state.DeviceId, state.Battery, state.FlightMode, state.Latitude, state.Longitude, state.Altitude, state.GyrometerOK, state.AccelerometerOK, state.MagnetometerOK };
                    return rawTelemetryOutputType;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deserializing message", message.PartitionKey, message.SequenceNumber);
                    rawTelemetryOutputType.DeadLetterMessage = new DeadLetterMessage { Exception = ex, EventData = message };
                }
            }

            return rawTelemetryOutputType;
        }
    }
}
