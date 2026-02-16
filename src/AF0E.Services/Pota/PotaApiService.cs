using System.Text.Json;
using AF0E.Services.Pota.Extensions;
using AF0E.Services.Pota.Models;

namespace AF0E.Services.Pota;

public interface IPotaApiService
{
    Task<List<PotaActivityInfo>> CheckActivityAsync(string? band = null, string? mode = null, CancellationToken ct = default);
    Task<PotaActivityInfo> CheckActivityAsync(string callSign, CancellationToken ct = default);
}

public sealed class PotaApiService(IHttpClientFactory httpClientFactory) : IPotaApiService
{
    private const string SpotsUrl = "https://api.pota.app/v1/spots";

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<PotaActivityInfo> CheckActivityAsync(string callSign, CancellationToken ct = default)
    {
        var spot = (await CheckActivityAsync(null, null, ct))
            .WithActivator(callSign)
            .FirstOrDefault();

        return new PotaActivityInfo(spot?.CallSign ?? callSign, spot is not null, spot?.ParkNum, spot?.FreqKhz, spot?.Band, spot?.Mode, spot?.LastSpotTime);
    }

    public async Task<List<PotaActivityInfo>> CheckActivityAsync(string? band = null, string? mode = null, CancellationToken ct = default)
    {
        var spots = await CheckActivityInternalAsync(band, mode, ct);
        return [.. spots.WithMaxFrequency(54000)]; //filter out everything above 6m
    }

    private async Task<List<PotaActivityInfo>> CheckActivityInternalAsync(string? band = null, string? mode = null, CancellationToken ct = default)
    {
        using var client = httpClientFactory.CreateClient();

        var response = await client.GetAsync(SpotsUrl, ct);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(ct);
        var spots = JsonSerializer.Deserialize<List<PotaSpot>>(content, _jsonOptions);

        if (spots is null)
            return [];

        var query = spots.ActiveOnly();

        if (!string.IsNullOrWhiteSpace(band))
            query = query.OnBand(band);

        if (!string.IsNullOrWhiteSpace(mode))
            query = query.Where(s => string.Equals(s.Mode, mode, StringComparison.OrdinalIgnoreCase));

        return [.. query
            .OrderByDescending(s => s.SpotTime)
            .Select(s => new PotaActivityInfo(
                s.Activator, 
                true, 
                s.Reference, 
                s.Frequency, 
                s.Frequency.Band, 
                s.Mode, 
                new DateTimeOffset(DateTime.SpecifyKind(s.SpotTime, DateTimeKind.Utc))))];
    }
}
