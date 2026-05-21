// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace Logbook.Api.Services;

public sealed record LotwSettings
{
    public Uri ApiUrl { get; init; } = new("https://lotw.arrl.org/lotwuser/lotwreport.adi");
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string? OwnCallsign { get; init; }
}
