using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.Documents;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Serverless.Serialization;
using Serverless.Serialization.Models;
using System;
using System.Threading.Tasks;

namespace DroneTelemetryFunctionApp
{
    public class RawTelemetryFunction
    {
        private readonly ITelemetryProcessor telemetryProcessor;
        private readonly IStateChangeProcessor stateChangeProcessor;

        private readonly TelemetryClient telemetryClient;

        public RawTelemetryFunction(ITelemetryProcessor telemetryProcessor, IStateChangeProcessor stateChangeProcessor, TelemetryClient telemetryClient)
        {
            this.telemetryProcessor = telemetryProcessor;
            this.stateChangeProcessor = stateChangeProcessor;
            this.telemetryClient = telemetryClient;
        }

        [FunctionName("RawTelemetryFunction")]
        [StorageAccount("DeadLetterStorage")]
        public async Task RunAsync(
            [EventHubTrigger("%EventHubName%", Connection = "EventHubConnection", ConsumerGroup ="%EventHubConsumerGroup%")]EventData[] messages,
            [Queue("deadletterqueue")] IAsyncCollector<DeadLetterMessage> deadLetterMessages,
            ILogger logger)
        {
            telemetryClient.GetMetric("EventHubMessageBatchSize").TrackValue(messages.Length);

            foreach (var message in messages)
            {
                DeviceState deviceState = null;

                try
                {
                    deviceState = telemetryProcessor.Deserialize(message.Body.Array, logger);

                    try
                    {
                        await stateChangeProcessor.UpdateState(deviceState, logger);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error updating status document", deviceState);
                        await deadLetterMessages.AddAsync(new DeadLetterMessage { Exception = ex, EventData = message, DeviceState = deviceState });
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error deserializing message", message.SystemProperties.PartitionKey, message.SystemProperties.SequenceNumber);
                    await deadLetterMessages.AddAsync(new DeadLetterMessage { Exception = ex, EventData = message });
                }
            }
        }
    }
}
