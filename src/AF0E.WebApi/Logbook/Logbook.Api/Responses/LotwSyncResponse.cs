namespace Logbook.Api.Responses;

// ReSharper disable NotAccessedPositionalProperty.Global
#pragma warning disable CA1002 // Do not expose generic lists
public sealed record LotwSyncResponse(int Received, int Matched, List<string> Unmatched);
