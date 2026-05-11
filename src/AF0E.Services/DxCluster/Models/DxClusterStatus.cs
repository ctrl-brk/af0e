// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace AF0E.Services.DxCluster.Models;

public sealed record DxClusterStatus
{
    public bool Configured { get; init; }
    public bool Running { get; init; }
    public DateTimeOffset? LastAccessUtc { get; init; }
    public DateTimeOffset? LastStartUtc { get; init; }
    public DateTimeOffset? LastStopUtc { get; init; }
    public int CachedSpotCount { get; init; }
    public TimeSpan InactivityTimeout { get; init; }
    public TimeSpan ReconnectDelay { get; init; }
    public IReadOnlyList<DxClusterSpotFilter> Filters { get; init; } = [];
    public IReadOnlyList<DxClusterServerStatus> Servers { get; init; } = [];
}

public sealed record DxClusterServerStatus
{
    public required string Name { get; init; }
    public required string Host { get; init; }
    public int Port { get; init; }
    public bool Enabled { get; init; }
    public bool Connected { get; init; }
    public int ReconnectCount { get; init; }
    public DateTimeOffset? LastConnectUtc { get; init; }
    public DateTimeOffset? LastDisconnectUtc { get; init; }
    public DateTimeOffset? LastLineUtc { get; init; }
    public DateTimeOffset? LastSpotUtc { get; init; }
    public DateTimeOffset? LastErrorUtc { get; init; }
    public string? LastError { get; init; }
}
