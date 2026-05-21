// ReSharper disable NotAccessedPositionalProperty.Global
namespace Logbook.Api.Models;

public sealed record AdifDetails(
    string Call,
    DateTime Date,
    string Band,
    string? BandRx,
    double? FreqHz,
    double? FreqRxHz,
    string Mode,
    string? PropMode,
    string? Sat,
    string? RstSent,
    string? RstRcvd,
    string MyGrid,
    string MyCounty,
    string MyState,
    string? StationCallsign,
    string? OperatorCallsign
);
