using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace QslDueWatcher;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var environment = Environment.GetEnvironmentVariable("NETCOREAPP_ENVIRONMENT")
                          ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
                          ?? Environments.Production;

        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            Args = args,
            EnvironmentName = environment,
            ContentRootPath = AppContext.BaseDirectory
        });

        builder.Configuration.AddEnvironmentVariables();
        builder.Services.Configure<ConsoleLifetimeOptions>(options => options.SuppressStatusMessages = true);
        builder.Services.Configure<HostOptions>(options => options.ShutdownTimeout = TimeSpan.FromSeconds(30));

        builder.Services.AddOptions<AppSettings>()
            .Bind(builder.Configuration.GetSection("AppSettings"))
            .Validate(ValidateSettings, "AppSettings contains missing or invalid values")
            .ValidateOnStart();

        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddSingleton<IQslDueRepository, QslDueRepository>();
        builder.Services.AddSingleton<IReminderStateStore, ReminderStateStore>();
        builder.Services.AddSingleton<IReminderEmailFormatter, ReminderEmailFormatter>();
        builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();
        builder.Services.AddHostedService<QslDueWorker>();

        builder.Logging.ClearProviders();
        builder.Services.AddSerilog((_, configuration) => configuration
            .ReadFrom.Configuration(builder.Configuration)
            .WriteTo.Console()
            .WriteTo.File(Path.Combine(AppContext.BaseDirectory, "QslDueWatcher-.log"), rollingInterval: RollingInterval.Month));

        await builder.Build().RunAsync();
        return Environment.ExitCode;
    }

    private static bool ValidateSettings(AppSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.ConnectionString) ||
            string.IsNullOrWhiteSpace(settings.StateFile) ||
            string.IsNullOrWhiteSpace(settings.Email.From) ||
            string.IsNullOrWhiteSpace(settings.Email.To) ||
            string.IsNullOrWhiteSpace(settings.Email.Subject) ||
            string.IsNullOrWhiteSpace(settings.Email.Smtp.Server) ||
            settings.Email.Smtp.Port is < 1 or > 65535)
            return false;

        try
        {
            _ = TimeZoneInfo.FindSystemTimeZoneById(settings.TimeZoneId);
            return true;
        }
        catch (TimeZoneNotFoundException)
        {
            return false;
        }
        catch (InvalidTimeZoneException)
        {
            return false;
        }
    }
}


