namespace Logbook.Api.Requests;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed record CopyActivationRequest(int ActivationId, string ParkNumber);
