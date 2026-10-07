using System.Diagnostics.CodeAnalysis;
using AF0E.DB.Models;

namespace Logbook.Api.Models;

[SuppressMessage("ReSharper", "UnusedMember.Global")]
public sealed class PotaParkDetails(PotaPark park)
{
    public int ParkId { get; } = park.ParkId;
    public string ParkNum { get; } = park.ParkNum;
    public string ParkName { get; } = park.ParkName;
    public decimal? Lat { get; } = park.Lat;
#pragma warning disable CA1720
    public decimal? Long { get; } = park.Long;
#pragma warning restore CA1720
    public string? Grid { get; } = park.Grid;
    public string? Location { get; } = park.Location;
    public string Country { get; } = park.Country;
    public int TotalActivationCount { get; } = park.TotalActivationCount;
    public int TotalQsoCount { get; } = park.TotalQsoCount;
}
