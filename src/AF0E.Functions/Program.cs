using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    /*
    .ConfigureAppConfiguration(config =>
    {
        config
            /*
            .AddJsonFile("appsettings.json", true, true)
            .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            #1#
            .Build();
    })
    */

    .ConfigureServices(services =>
    {
        services.AddHttpClient();
        /*
        services.AddDurableTaskClient(builder =>
        {
            // Configure options for this builder. Can be omitted if no options customization is needed.
            builder.Configure(opt => { });
            builder.UseGrpc(); // multiple overloads available for providing gRPC information

            // AddDurableTaskClient allows for multiple named clients by passing in a name as the first argument.
            // When using a non-default named client, you will need to make this call below to have the
            // DurableTaskClient added directly to the DI container. Otherwise IDurableTaskClientProvider must be used
            // to retrieve DurableTaskClients by name from the DI container. In this case, we are using the default
            // name, so the line below is NOT required as it was already called for us.
            builder.RegisterDirectly();
        });
        */
    })

    /*
    .UseSerilog((context, config) =>
        config.ReadFrom.Configuration(context.Configuration))
        */

    .Build();

host.Run();
