using Azure.Messaging.EventHubs;
using Azure.Storage.Queues;
using Microsoft.ApplicationInsights;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

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
        public async Task RunAsync([EventHubTrigger("%EventHubName%", Connection = "EventHubConnection")] EventData[] messages,
            FunctionContext context)
        {
            _telemetryClient.GetMetric("EventHubMessageBatchSize").TrackValue(messages.Length);
            DeviceState? deviceState = null;
            // Create a new CosmosClient
            var cosmosClient = new CosmosClient(Environment.GetEnvironmentVariable("COSMOSDB_CONNECTION_STRING"));

            // Get a reference to the database and the container
            var database = cosmosClient.GetDatabase(Environment.GetEnvironmentVariable("COSMOSDB_DATABASE_NAME"));
            var container = database.GetContainer(Environment.GetEnvironmentVariable("COSMOSDB_DATABASE_COL"));

            // Create a new QueueClient
            var queueClient = new QueueClient(Environment.GetEnvironmentVariable("DeadLetterStorage"), "deadletterqueue");
            await queueClient.CreateIfNotExistsAsync();

            foreach (var message in messages)
            {
                try
                {
                    deviceState = _telemetryProcessor.Deserialize(message.Body.ToArray(), _logger);
                    try
                    {
                        // Add the device state to Cosmos DB
                        await container.UpsertItemAsync(deviceState, new PartitionKey(deviceState.DeviceId));
                    }
                    catch (Exception ex)
                    {
                         _logger.LogError(ex, "Error saving on database", message.PartitionKey, message.SequenceNumber);
                        var deadLetterMessage = new DeadLetterMessage { Issue = ex.Message, MessageBody = message.Body.ToArray(), DeviceState = deviceState };
                        // Convert the dead letter message to a string
                        var deadLetterMessageString = JsonConvert.SerializeObject(deadLetterMessage);

                        // Send the message to the queue
                        await queueClient.SendMessageAsync(deadLetterMessageString);
                    }

                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deserializing message", message.PartitionKey, message.SequenceNumber);
                    var deadLetterMessage = new DeadLetterMessage { Issue = ex.Message, MessageBody = message.Body.ToArray(), DeviceState = deviceState };
                    // Convert the dead letter message to a string
                    var deadLetterMessageString = JsonConvert.SerializeObject(deadLetterMessage);

                    // Send the message to the queue
                    await queueClient.SendMessageAsync(deadLetterMessageString);
                }
            }
        }
    }
}
