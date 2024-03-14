using HamMarket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureAppConfiguration((ctx, cfg) =>
    {
        ctx.HostingEnvironment.EnvironmentName = Environment.GetEnvironmentVariable("NETCOREAPP_ENVIRONMENT") ?? "production";

        cfg.AddJsonFile("appsettings.json", false)
            .AddJsonFile($"appsettings.{ctx.HostingEnvironment.EnvironmentName}.json", true)
            .AddEnvironmentVariables();
    })
    .ConfigureServices((ctx, svc) =>
    {
        svc.Configure<ConsoleLifetimeOptions>(opt => opt.SuppressStatusMessages = true)
            .Configure<HostOptions>(opt => opt.ShutdownTimeout = TimeSpan.FromSeconds(10))
            .Configure<AppSettings>(ctx.Configuration.GetSection("AppSettings"))
            .AddSingleton<IHostedService, HostedService>()
            .AddSingleton<IQthHandler, QthHandler>()
            .AddSingleton<IEhamHandler, EhamHandler>();
    })
    .ConfigureLogging((ctx, cfg) =>
    {
        cfg.ClearProviders()
            .AddConfiguration(ctx.Configuration.GetSection("Logging"))
            .AddConsole()
            .AddDebug();
    })
    .UseConsoleLifetime()
    .Build();

await host.RunAsync();
return 0;
