namespace AF0E.Services.Pota.Models;

public record PotaActivityInfo
(
    string CallSign,
    bool Active,
    string? ParkNum,
    string? FreqKhz,
    string? Band,
    string? Mode,
    DateTimeOffset? LastSpotTime
);
