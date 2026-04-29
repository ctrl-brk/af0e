using System.Net;
using System.Text.Json;
using System.Diagnostics.CodeAnalysis;

namespace RigCommander.Services;

public sealed record ActivationIdValidationResult(
    int Id,
    string ParkNum,
    string ParkName,
    DateTime StartDate,
    string Status);

[SuppressMessage("Design", "CA1054:URI-like parameters should not be strings")]
public sealed class ActivationIdValidationService : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly Uri _activationBaseUri;

    public ActivationIdValidationService(string logbookApiUrl, AdifForwardingSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentException.ThrowIfNullOrWhiteSpace(logbookApiUrl);

        _activationBaseUri = BuildActivationBaseUri(logbookApiUrl);
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds)
        };

        if (!string.IsNullOrWhiteSpace(settings.ApiKey))
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation(settings.ApiKeyHeaderName, settings.ApiKey);
    }

    public async Task<ActivationIdValidationResult?> ValidateAsync(int activationId, CancellationToken cancellationToken = default)
    {
        if (activationId <= 0)
            return null;

        using var response = await _httpClient.GetAsync(BuildActivationUri(activationId), cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<ActivationIdValidationResult>(JsonSerializerOptions.Web, cancellationToken)
            ?? throw new InvalidOperationException("Activation lookup returned an empty response.");
    }

    private Uri BuildActivationUri(int activationId)
        => new(_activationBaseUri, $"{activationId}");

    private static Uri BuildActivationBaseUri(string logbookApiUrl)
    {
        var apiUri = new Uri(logbookApiUrl, UriKind.Absolute);
        var path = apiUri.AbsolutePath;

        if (!path.EndsWith('/'))
            path += "/";

        var builder = new UriBuilder(apiUri)
        {
            Path = $"{path}pota/activations/",
            Query = string.Empty
        };

        return builder.Uri;
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}







