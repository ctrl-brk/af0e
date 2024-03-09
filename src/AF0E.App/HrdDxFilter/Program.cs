using HrdDxFilter;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

var host = new HostBuilder()
    .ConfigureAppConfiguration((ctx, cfg) =>
    {
        ctx.HostingEnvironment.EnvironmentName = Environment.GetEnvironmentVariable("NETCOREAPP_ENVIRONMENT") ?? "production";

        cfg.SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", false)
            .AddJsonFile($"appsettings.{ctx.HostingEnvironment.EnvironmentName}.json", true)
            .AddEnvironmentVariables();
    })
    .ConfigureServices((ctx, svc) =>
    {
        svc.Configure<ConsoleLifetimeOptions>(opt => opt.SuppressStatusMessages = true)
            .Configure<HostOptions>(opt => opt.ShutdownTimeout = TimeSpan.FromSeconds(10))
            .Configure<AppSettings>(ctx.Configuration.GetSection("AppSettings"))
            .AddSingleton<IHostedService, HostedService>();
    })

    .ConfigureLogging((ctx, cfg) =>
    {
        cfg.ClearProviders()
            .AddConfiguration(ctx.Configuration.GetSection("Logging"))
            .AddSerilog(new LoggerConfiguration()
                .ReadFrom.Configuration(ctx.Configuration)
                .WriteTo.Console()
                .WriteTo.File("HrdDxFilter-.log", rollingInterval: RollingInterval.Month)
                .CreateLogger());

    })
    .Build();

await host.RunAsync();
return 0;
