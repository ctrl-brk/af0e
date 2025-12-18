using AF0E.DB.Models;

namespace Logbook.Api.Models;

public sealed class PotaParkDetails(PotaPark park)
{
    public int ParkId { get; init; } = park.ParkId;
    public string ParkNum { get; init; } = park.ParkNum;
    public string ParkName { get; init; } = park.ParkName;
    public decimal? Lat { get; init; } = park.Lat;
#pragma warning disable CA1720
    public decimal? Long { get; init; } = park.Long;
    public string? Grid { get; init; } = park.Grid;
    public string? Location { get; init; } = park.Location;
    public string Country { get; init; } = park.Country;
    public int TotalActivationCount { get; set; } = park.TotalActivationCount;
    public int TotalQsoCount { get; set; } = park.TotalQsoCount;
}
