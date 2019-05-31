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
    public class RawTelemetryFunction
    {
        private readonly TelemetryProcessor telemetryProcessor;
        private readonly StateChangeProcessor stateChangeProcessor;

        private readonly static DocumentClient client;
        private readonly static string CosmosDBDatabase = GetEnvironmentVariable("COSMOSDB_DATABASE_NAME");
        private readonly static string CosmosDBCollection = GetEnvironmentVariable("COSMOSDB_DATABASE_COL");

        public RawTelemetryFunction(ITelemetrySerializer<DroneState> telemetrySerializer)
        {
            telemetryProcessor = new TelemetryProcessor(telemetrySerializer);
            stateChangeProcessor = new StateChangeProcessor(client, CosmosDBDatabase, CosmosDBCollection);
        }

        static RawTelemetryFunction()
        {
            var cosmosDBEndpoint = GetEnvironmentVariable("CosmosDBEndpoint");
            var cosmosDBKey = GetEnvironmentVariable("CosmosDBKey");
            client = new DocumentClient(new Uri(cosmosDBEndpoint), cosmosDBKey);
        }

        private static string GetEnvironmentVariable(string name)
        {
            return Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
        }

        private static string key = TelemetryConfiguration.Active.InstrumentationKey =
                                            GetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY");

        private static TelemetryClient telemetryClient =
            new TelemetryClient() { InstrumentationKey = key };


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
