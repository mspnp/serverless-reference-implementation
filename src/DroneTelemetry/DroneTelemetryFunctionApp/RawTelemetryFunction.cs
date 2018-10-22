using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Serverless.Serialization;
using Serverless.Serialization.Models;
using System;
using System.Threading.Tasks;

namespace DroneTelemetryFunctionApp
{
    public static class RawTelemetryFunction
    {
        private static readonly TelemetryProcessor telemetryProcessor;
        private static readonly StateChangeProcessor stateChangeProcessor;

        static RawTelemetryFunction()
        {
            telemetryProcessor = new TelemetryProcessor(new TelemetrySerializer<DroneState>());
            var client = new DocumentClient(new Uri(CosmosDBEndpoint), CosmosDBKey);
            stateChangeProcessor = new StateChangeProcessor(client, CosmosDBDatabase, CosmosDBCollection);
        }

        private static string GetEnvironmentVariable(string name)
        {
            return Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
        }

        private static readonly string CosmosDBEndpoint = GetEnvironmentVariable("CosmosDBEndpoint");
        private static readonly string CosmosDBKey = GetEnvironmentVariable("CosmosDBKey");
        private static readonly string CosmosDBDatabase = GetEnvironmentVariable("COSMOSDB_DATABASE_NAME");
        private static readonly string CosmosDBCollection = GetEnvironmentVariable("COSMOSDB_DATABASE_COL");

        private static string key = TelemetryConfiguration.Active.InstrumentationKey =
                                            GetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY");

        private static TelemetryClient telemetryClient =
            new TelemetryClient() { InstrumentationKey = key };


        [FunctionName("RawTelemetryFunction")]
        [StorageAccount("DeadLetterStorage")]
        public static async Task RunAsync(
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
