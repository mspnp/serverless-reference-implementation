using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using System;
using Serverless.Serialization;
using Serverless.Serialization.Models;
using Microsoft.Extensions.Configuration;

[assembly: FunctionsStartup(typeof(DroneTelemetryFunctionApp.Startup))]

namespace DroneTelemetryFunctionApp
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            var config = builder.Services.BuildServiceProvider().GetService<IConfiguration>();

            builder.Services.AddTransient<ITelemetrySerializer<DroneState>, TelemetrySerializer<DroneState>>();
            builder.Services.AddTransient<ITelemetryProcessor, TelemetryProcessor>();
            builder.Services.AddTransient<IStateChangeProcessor>(ctx =>
            {
                var client = ctx.GetService<IDocumentClient>();
                return new StateChangeProcessor(
                    client, 
                    config.GetValue<string>("COSMOSDB_DATABASE_NAME"), 
                    config.GetValue<string>("COSMOSDB_DATABASE_COL"));
            });

            var cosmosDBEndpoint = config.GetValue<string>("CosmosDBEndpoint");
            var cosmosDBKey = config.GetValue<string>("CosmosDBKey");
            builder.Services.AddSingleton<IDocumentClient>(new DocumentClient(new Uri(cosmosDBEndpoint), cosmosDBKey));

            var key = TelemetryConfiguration.Active.InstrumentationKey = config.GetValue<string>("APPINSIGHTS_INSTRUMENTATIONKEY");
            builder.Services.AddSingleton<TelemetryClient>(new TelemetryClient() { InstrumentationKey = key });
        }
    }
}