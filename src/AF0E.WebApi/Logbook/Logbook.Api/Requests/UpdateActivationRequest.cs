namespace Logbook.Api.Requests;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed record UpdateActivationRequest(
    int Id,
    string ParkNum,
    string Grid,
    string County,
    string State,
    decimal Lat,
    decimal Long,
    DateTime StartDate,
    DateTime? EndDate,
    DateTime? LogSubmittedDate,
    string Status,
    string? SiteComments
);
