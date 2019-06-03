using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Linq;
using System.Threading.Tasks;


namespace DroneTelemetryFunctionApp
{
    public class StateChangeProcessor : IStateChangeProcessor
    {
        private IDocumentClient client;
        private readonly string cosmosDBDatabase;
        private readonly string cosmosDBCollection;

        public StateChangeProcessor(IDocumentClient client, IOptions<StateChangeProcessorOptions> options)
        {
            this.client = client;
            this.cosmosDBDatabase = options.Value.COSMOSDB_DATABASE_NAME;
            this.cosmosDBCollection = options.Value.COSMOSDB_DATABASE_COL;
        }

        public async Task<ResourceResponse<Document>> UpdateState(DeviceState source, ILogger log)
        {
            log.LogInformation("Processing change message for device ID {DeviceId}", source.DeviceId);

            DeviceState target = null;

            try
            {
                var response = await client.ReadDocumentAsync(UriFactory.CreateDocumentUri(cosmosDBDatabase, cosmosDBCollection, source.DeviceId),
                                                              new RequestOptions { PartitionKey = new PartitionKey(source.DeviceId) });

                target = (DeviceState)(dynamic)response.Resource;

                // Merge properties
                target.Battery = source.Battery ?? target.Battery;
                target.FlightMode = source.FlightMode ?? target.FlightMode;
                target.Latitude = source.Latitude ?? target.Latitude;
                target.Longitude = source.Longitude ?? target.Longitude;
                target.Altitude = source.Altitude ?? target.Altitude;
                target.AccelerometerOK = source.AccelerometerOK ?? target.AccelerometerOK;
                target.GyrometerOK = source.GyrometerOK ?? target.GyrometerOK;
                target.MagnetometerOK = source.MagnetometerOK ?? target.MagnetometerOK;
            }
            catch (DocumentClientException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    target = source;
                }
            }

            var collectionLink = UriFactory.CreateDocumentCollectionUri(cosmosDBDatabase, cosmosDBCollection);
            return await client.UpsertDocumentAsync(collectionLink, target);
        }
    }
}
