using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddHttpClient();
    })

    /*
    .UseSerilog((context, config) =>
        config.ReadFrom.Configuration(context.Configuration))
        */

    .Build();

host.Run();
