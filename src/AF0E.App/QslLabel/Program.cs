using Microsoft.Extensions.Configuration;

namespace QslLabel;

internal static class Program
{
    public static AppSettings Settings { get; private set; } = null!;

    [STAThread]
    private static void Main()
    {
        var env = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "production";

        var builder = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{env}.json", true);

        var cfg = builder.Build();
        Settings = cfg.GetSection("AppSettings").Get<AppSettings>()!;

        ApplicationConfiguration.Initialize();

#pragma warning disable CA2000
        Application.Run(new MainForm());
    }
}
