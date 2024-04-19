using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serverless.Serialization.Models;
using Serverless.Serialization;
using DroneTelemetryFunctionApp;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        services.AddTransient<ITelemetrySerializer<DroneState>, TelemetrySerializer<DroneState>>();
        services.AddTransient<ITelemetryProcessor, TelemetryProcessor>();
    })
    .Build();

await host.RunAsync();
