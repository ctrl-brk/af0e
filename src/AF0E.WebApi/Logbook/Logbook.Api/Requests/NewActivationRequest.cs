namespace Logbook.Api.Requests;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed record NewActivationRequest(
    int? PrevDayActivationId,
    string ParkNumber,
    string Grid,
    string County,
    string State,
    decimal Lat,
    decimal Lon,
    DateTime? StartDate
);
