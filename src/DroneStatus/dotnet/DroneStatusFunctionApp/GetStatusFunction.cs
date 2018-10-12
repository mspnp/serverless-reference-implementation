using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace DroneStatusFunctionApp
{
    public static class GetStatusFunction
    {
        public const string GetDeviceStatusRoleName = "GetStatus";

        [FunctionName("GetStatusFunction")]
        public static Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequest req, [CosmosDB(
                databaseName: "%COSMOSDB_DATABASE_NAME%",
                collectionName: "%COSMOSDB_DATABASE_COL%",
                ConnectionStringSetting = "COSMOSDB_CONNECTION_STRING",
                Id = "{Query.deviceId}",
                PartitionKey = "{Query.deviceId}")] dynamic deviceStatus, ILogger log)
        {
            log.LogInformation("Processing GetStatus request.");

            return req.HandleIfAuthorizedForRoles(new[] { GetDeviceStatusRoleName },
                async () =>
                {
                    string deviceId = req.Query["deviceId"];
                    if (deviceId == null)
                    {
                        return new BadRequestObjectResult("Missing DeviceId");
                    }

                    return await Task.FromResult<IActionResult>(deviceStatus != null
                         ? (ActionResult)new OkObjectResult(deviceStatus)
                         : new NotFoundResult());
                },
                log);
        }

        public static IDeviceStatusRepository Repository { get; set; }
    }
}
