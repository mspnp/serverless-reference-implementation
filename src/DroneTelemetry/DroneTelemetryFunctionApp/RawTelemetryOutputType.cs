//https://learn.microsoft.com/azure/azure-functions/dotnet-isolated-process-guide?tabs=windows#multiple-output-bindings
using Microsoft.Azure.Functions.Worker;

namespace DroneTelemetryFunctionApp
{
    public class RawTelemetryOutputType
    {
        [QueueOutput("deadletterqueue")]
        public DeadLetterMessage? DeadLetterMessage { get; set; }

        [CosmosDBOutput("%COSMOSDB_DATABASE_NAME%", "%COSMOSDB_DATABASE_COL%", Connection = "COSMOSDB_CONNECTION_STRING", CreateIfNotExists = true)]
        public Object? DeviceState { get; set; }
    }
}
