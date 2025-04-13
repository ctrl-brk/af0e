using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Azure.Functions.Worker.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        // Define application-level resource
        var resourceBuilder = ResourceBuilder.CreateDefault()
            .AddService("DXAggregator");

        // Add OpenTelemetry tracing
        services.AddOpenTelemetry()
            .UseFunctionsWorkerDefaults()
            .WithTracing(builder =>
            {
                builder
                    .SetResourceBuilder(resourceBuilder)
                    .AddAspNetCoreInstrumentation() // Track incoming HTTP requests
                    .AddHttpClientInstrumentation() // Track outbound HTTP requests
                    .AddSource("DXAggregator") // Custom ActivitySource
                    .AddAzureMonitorTraceExporter(options =>
                    {
                        options.ConnectionString = Environment.GetEnvironmentVariable("AppInsightsConnectionString");
                    });
            });

        services.AddLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddOpenTelemetry(options =>
            {
                options.IncludeFormattedMessage = true;
                options.IncludeScopes = true;
                options.ParseStateValues = true;

                options.AddAzureMonitorLogExporter(o =>
                {
                    o.ConnectionString = Environment.GetEnvironmentVariable("AppInsightsConnectionString");
                });
            });
        });

        services.AddHttpClient();
    })

    /*
    .UseSerilog((context, config) =>
        config.ReadFrom.Configuration(context.Configuration))
        */

    .Build();

host.Run();
