using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Threading.Tasks;

namespace DroneStatusFunctionApp
{
    public static class GetStatusFunction
    {
        public const string GetDeviceStatusRoleName = "GetStatus";

        [FunctionName("GetStatusFunction")]
        public static IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)]HttpRequest req, 
            [CosmosDB(
                databaseName: "%COSMOSDB_DATABASE_NAME%",
                collectionName: "%COSMOSDB_DATABASE_COL%",
                ConnectionStringSetting = "COSMOSDB_CONNECTION_STRING",
                Id = "{Query.deviceId}",
                PartitionKey = "{Query.deviceId}")] dynamic deviceStatus, 
            ClaimsPrincipal principal,
            ILogger log)
        {
            log.LogInformation("Processing GetStatus request.");

            if (!principal.IsAuthorizedByRoles(new[] { GetDeviceStatusRoleName }, log))
            {
                return new UnauthorizedResult();
            }

            string deviceId = req.Query["deviceId"];
            if (deviceId == null)
            {
                return new BadRequestObjectResult("Missing DeviceId");
            }

            if (deviceStatus == null)
            {
                return new NotFoundResult();
            }
            else
            {
                return new OkObjectResult(deviceStatus);
            }
        }
    }
}
