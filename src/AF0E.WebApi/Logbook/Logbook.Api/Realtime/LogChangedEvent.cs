// ReSharper disable NotAccessedPositionalProperty.Global
namespace Logbook.Api.Realtime;

public sealed record LogChangedEvent(
    string Operation,
    int? LogId,
    int? ActivationId,
    string? Call,
    string Source,
    DateTime OccurredUtc,
    long Version);
