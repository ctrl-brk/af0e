using System.Collections.ObjectModel;

namespace AF0E.Services.DxCluster.Configuration;

public sealed class DxClusterOptions
{
    public TimeSpan InactivityTimeout { get; set; } = TimeSpan.FromMinutes(10);
    public TimeSpan ReconnectDelay { get; set; } = TimeSpan.FromSeconds(15);
    public TimeSpan MonitorInterval { get; set; } = TimeSpan.FromSeconds(15);
    public TimeSpan SpotMaxAge { get; set; } = TimeSpan.FromMinutes(30);
    public TimeSpan LoginDelay { get; set; } = TimeSpan.FromSeconds(1);
    public int MaxSpots { get; set; } = 250;
    public Collection<DxClusterServerOptions> Servers { get; } = [];
}

public sealed class DxClusterServerOptions
{
    public string Name { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 23;
    public string? Login { get; set; }
    public string? Password { get; set; }
    public string? PostLoginCommand { get; set; }
    public bool Enabled { get; set; }
}
