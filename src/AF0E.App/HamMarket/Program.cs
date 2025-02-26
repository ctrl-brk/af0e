using FluentValidation;
using HamMarket;
using HamMarket.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog;

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

        // Register FluentValidation validator
        svc.AddSingleton<IValidator<AppSettings>, SettingsValidator>();
        // Validate the configuration during startup
        svc.AddSingleton<IValidateOptions<AppSettings>>(provider =>
        {
            var validator = provider.GetRequiredService<IValidator<AppSettings>>();
            return new ValidationOptions<AppSettings>(validator);
        });
    })
    .ConfigureLogging((ctx, cfg) =>
    {
        cfg.ClearProviders()
            .AddConfiguration(ctx.Configuration.GetSection("Logging"))
            .AddSerilog(new LoggerConfiguration()
                .ReadFrom.Configuration(ctx.Configuration)
                .WriteTo.Console()
                .WriteTo.File(Path.Combine(AppContext.BaseDirectory, "HamMarket-.log"), rollingInterval: RollingInterval.Month)
                .CreateLogger());
    })
    .UseConsoleLifetime()
    .Build();

await host.RunAsync();
return 0;
