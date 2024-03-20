using Microsoft.Extensions.Configuration;

namespace N1MMLookup;

internal static class Program
{
    public static AppSettings Settings { get; private set; } = null!;

    [STAThread]
    static void Main()
    {
        var env = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "production";

        var builder = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{env}.json", true);

        var cfg = builder.Build();
        Settings = cfg.GetSection("AppSettings").Get<AppSettings>()!;

        // To customize application configuration such as set high DPI settings or default font, see https://aka.ms/applicationconfiguration.
        ApplicationConfiguration.Initialize();
#pragma warning disable CA2000
        Application.Run(new AppForm());
    }
}
