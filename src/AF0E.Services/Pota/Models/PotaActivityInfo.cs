namespace AF0E.Services.Pota.Models;

public record PotaActivityInfo
(
    string CallSign,
    bool Active,
    string? ParkNum,
    string? Location,
    string? Grid,
    string? FreqKhz,
    string? Band,
    string? Mode,
    DateTimeOffset? LastSpotTime
);
