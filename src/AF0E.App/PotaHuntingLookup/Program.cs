using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PotaHuntingLookup;
using Serilog;

if (!ValidateArguments(args))
    return 1;

var host = new HostBuilder()
    .ConfigureAppConfiguration((ctx, cfg) =>
    {
        ctx.HostingEnvironment.EnvironmentName = Environment.GetEnvironmentVariable("NETCOREAPP_ENVIRONMENT") ?? "production";

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
                .WriteTo.File(Path.Combine(AppContext.BaseDirectory, "PotaHuntingLookup-.log"), rollingInterval: RollingInterval.Month)
                .CreateLogger());

    })
    .Build();

await host.RunAsync();
return 0;

static bool ValidateArguments(string[] args)
{
    // args are not normal here. no file name in args[0]
    if (args.Length is < 2 or > 3 || (args.Length == 3 && !args[2].Equals("matchBand", StringComparison.OrdinalIgnoreCase)))
    {
        DisplayUsageInstructions();
        return false;
    }

    if (!DateTime.TryParse(args[0], out DateTime _))
    {
        DisplayUsageInstructions();
        Console.WriteLine($"Invalid start date: {args[0]}");
        return false;
    }

    if (!DateTime.TryParse(args[1], out DateTime _))
    {
        DisplayUsageInstructions();
        Console.WriteLine($"Invalid end date: {args[1]}");
        return false;
    }

    return true;

    static void DisplayUsageInstructions()
    {
        Console.WriteLine($"Usage: {System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name} <YYYY-DD-MM> <YYYY-DD-MM> [matchBand]\n\r");
    }
}
