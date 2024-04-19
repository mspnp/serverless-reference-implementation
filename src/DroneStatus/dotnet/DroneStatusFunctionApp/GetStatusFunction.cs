using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace DroneStatusFunctionApp
{
    public class GetStatusFunction
    {
        public const string GetDeviceStatusRoleName = "GetStatus";

        private readonly ILogger<GetStatusFunction> _logger;

        public GetStatusFunction(ILogger<GetStatusFunction> logger)
        {
            _logger = logger;
        }

        [Function("GetStatusFunction")]
        public IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req,
        [CosmosDBInput(
           databaseName: "%COSMOSDB_DATABASE_NAME%",
           containerName:"%COSMOSDB_DATABASE_COL%",
           Connection  = "COSMOSDB_CONNECTION_STRING",
           Id = "{Query.deviceId}",
           PartitionKey = "{Query.deviceId}")] DeviceState? deviceStatus)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            var principal = ClaimsPrincipalParser.Parse(req);
            if (principal == null || !principal.IsAuthorizedByRoles([GetDeviceStatusRoleName], _logger))
            {
                return new UnauthorizedResult();
            }

            string? deviceId = req.Query["deviceId"];
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
