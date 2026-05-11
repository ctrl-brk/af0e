using System.Collections.ObjectModel;
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable CollectionNeverUpdated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable PropertyCanBeMadeInitOnly.Global

namespace AF0E.Services.DxCluster.Configuration;

public sealed class DxClusterOptions
{
    public TimeSpan InactivityTimeout { get; set; } = TimeSpan.FromMinutes(10);
    public TimeSpan ReconnectDelay { get; set; } = TimeSpan.FromSeconds(15);
    public TimeSpan MonitorInterval { get; set; } = TimeSpan.FromSeconds(15);
    public TimeSpan SpotMaxAge { get; set; } = TimeSpan.FromMinutes(30);
    public TimeSpan LoginDelay { get; set; } = TimeSpan.FromSeconds(1);
    public int MaxSpots { get; set; } = 250;
    public Collection<DxClusterFilterOptions> Filters { get; } = [];
    public Collection<DxClusterServerOptions> Servers { get; } = [];
}

public sealed class DxClusterFilterOptions
{
    public string Name { get; set; } = string.Empty;
    public string? CallsignPatterns { get; set; }
    public Collection<string>? Modes { get; init; }
    public Collection<DxClusterFrequencyWindowOptions>? FrequencyWindows { get; init; }
}

public sealed class DxClusterFrequencyWindowOptions
{
    public decimal? MinFrequencyKhz { get; set; }
    public decimal? MaxFrequencyKhz { get; set; }
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
