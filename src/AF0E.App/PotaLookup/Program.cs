using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PotaLookup;
using Serilog;

//args here are not normal. no file name in args[0]
if (args.Length == 0)
{
    Console.WriteLine($"Usage: {System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name} <YYYY-DD-MM[Thh:mm:ss]> [YYYY-DD-MM]\n\r       end date is optional and includes the whole day, can't use time there :(");
    return 1;
}
if (!DateTime.TryParse(args[0], out DateTime _))
{
    Console.WriteLine($"Invalid start date: {args[0]}");
    return 1;
}
if (args.Length == 2 && !DateTime.TryParse(args[1], out DateTime _))
{
    Console.WriteLine($"Invalid end date: {args[1]}");
    return 1;
}

var host = new HostBuilder()
    .ConfigureAppConfiguration((ctx, cfg) =>
    {
        ctx.HostingEnvironment.EnvironmentName = Environment.GetEnvironmentVariable("NETCOREAPP_ENVIRONMENT") ?? "production";

        //cfg.SetBasePath(Directory.GetCurrentDirectory()) // this will give current working directory, not the .exe file location
        cfg.SetBasePath(AppContext.BaseDirectory) // .exe file location, alternative is AppDomain.CurrentDomain.BaseDirectory
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
                .WriteTo.File(Path.Combine(AppContext.BaseDirectory, "QrzLookup-.log"), rollingInterval: RollingInterval.Month)
                .CreateLogger());

    })
    .Build();

await host.RunAsync();
return 0;
