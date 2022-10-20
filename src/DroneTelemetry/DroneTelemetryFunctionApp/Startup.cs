using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Serverless.Serialization;
using Serverless.Serialization.Models;

[assembly: FunctionsStartup(typeof(DroneTelemetryFunctionApp.Startup))]

namespace DroneTelemetryFunctionApp
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddTransient<ITelemetrySerializer<DroneState>, TelemetrySerializer<DroneState>>();
            builder.Services.AddTransient<ITelemetryProcessor, TelemetryProcessor>();
        }
    }
}