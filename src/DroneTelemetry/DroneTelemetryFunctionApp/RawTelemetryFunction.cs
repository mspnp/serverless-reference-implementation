using Microsoft.ApplicationInsights;
using Azure.Messaging.EventHubs;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace DroneTelemetryFunctionApp
{
    public class RawTelemetryFunction
    {
        private readonly ITelemetryProcessor telemetryProcessor;
        
        private readonly TelemetryClient telemetryClient;

        public RawTelemetryFunction(ITelemetryProcessor telemetryProcessor, TelemetryClient telemetryClient)
        {
            this.telemetryProcessor = telemetryProcessor;
            this.telemetryClient = telemetryClient;
        }

        [FunctionName("RawTelemetryFunction")]
        [StorageAccount("DeadLetterStorage")]
        public async Task RunAsync(
            [EventHubTrigger("%EventHubName%", Connection = "EventHubConnection", ConsumerGroup = "%EventHubConsumerGroup%")] EventData[] messages,
            [Queue("deadletterqueue")] IAsyncCollector<DeadLetterMessage> deadLetterMessages,
            [CosmosDB(
                databaseName: "%COSMOSDB_DATABASE_NAME%",
                collectionName: "%COSMOSDB_DATABASE_COL%",
                ConnectionStringSetting = "COSMOSDB_CONNECTION_STRING")]
                IAsyncCollector<DeviceState> devices,
            ILogger logger)
        {
            telemetryClient.GetMetric("EventHubMessageBatchSize").TrackValue(messages.Length);

            foreach (var message in messages)
            {
                DeviceState deviceState = null;

                try
                {
                    deviceState = telemetryProcessor.Deserialize(message.Body.ToArray(), logger);

                    try
                    {
                        await devices.AddAsync(deviceState);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error updating status document", deviceState);
                        await deadLetterMessages.AddAsync(new DeadLetterMessage { Exception = ex, EventData = message, DeviceState = deviceState });
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error deserializing message", message.PartitionKey, message.SequenceNumber);
                    await deadLetterMessages.AddAsync(new DeadLetterMessage { Exception = ex, EventData = message });
                }
            }
        }
    }
}
