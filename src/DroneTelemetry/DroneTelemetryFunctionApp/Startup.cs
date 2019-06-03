using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using Serverless.Serialization;
using Serverless.Serialization.Models;

[assembly: FunctionsStartup(typeof(DroneTelemetryFunctionApp.Startup))]

namespace DroneTelemetryFunctionApp
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddOptions<StateChangeProcessorOptions>()
                .Configure<IConfiguration>((configSection, configuration) =>
                {
                    configuration.Bind(configSection);
                });

            builder.Services.AddTransient<ITelemetrySerializer<DroneState>, TelemetrySerializer<DroneState>>();
            builder.Services.AddTransient<ITelemetryProcessor, TelemetryProcessor>();
            builder.Services.AddTransient<IStateChangeProcessor, StateChangeProcessor>();

            builder.Services.AddSingleton<IDocumentClient>(ctx => {
                var config = ctx.GetService<IConfiguration>();
                var cosmosDBEndpoint = config.GetValue<string>("CosmosDBEndpoint");
                var cosmosDBKey = config.GetValue<string>("CosmosDBKey");
                return new DocumentClient(new Uri(cosmosDBEndpoint), cosmosDBKey);
            });
        }
    }
}