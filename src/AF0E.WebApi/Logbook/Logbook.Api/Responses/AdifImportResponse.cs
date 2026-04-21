namespace Logbook.Api.Responses;

// ReSharper disable NotAccessedPositionalProperty.Global
#pragma warning disable CA1002 // Do not expose generic lists
public sealed record AdifImportResponse(int Received, int Accepted, List<string> Skipped, int Qrz);
