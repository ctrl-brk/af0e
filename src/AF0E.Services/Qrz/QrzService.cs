using System.Web;
using System.Xml;
using System.Xml.Serialization;
using AF0E.Common.Qrz;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AF0E.Services.Qrz;

public interface IQrzService : IDisposable
{
    Task<(QrzDatabase? response, bool notFound)> QueryCallsignAsync(string callSign, CancellationToken ct = default);
}

public sealed class QrzService : IQrzService
{
    private const string Agent = "af0e_lookup";
    private readonly HttpClient _httpClient;
    private readonly QrzSettings _settings;
    private readonly ILogger<QrzService> _logger;
    private string? _sessionKey;

    public QrzService(IOptions<QrzSettings> options, ILogger<QrzService> logger)
    {
        _settings = options.Value;
        _logger = logger;
        _httpClient = new HttpClient { BaseAddress = _settings.ApiUrl };
    }

    public async Task<(QrzDatabase? response, bool notFound)> QueryCallsignAsync(string callSign, CancellationToken ct = default)
    {
        var retry = false;
        QrzDatabase? result;

    RETRY_SESSION:
        await GetSessionKeyAsync(ct);

        if (string.IsNullOrEmpty(_sessionKey))
            return (null, false);

        var response = await _httpClient.GetAsync($"?s={_sessionKey};callsign={HttpUtility.UrlEncode(callSign)};agent={Agent}", ct);

        await using (var stream = await response.Content.ReadAsStreamAsync(ct))
        {
            using var reader = XmlReader.Create(stream);
            var serializer = new XmlSerializer(typeof(QrzDatabase), "http://xmldata.qrz.com");
            try
            {
                result = serializer.Deserialize(reader) as QrzDatabase;
            }
            catch (Exception e)
            {
                _logger.LogInvalidXmlResponse(await response.Content.ReadAsStringAsync(ct), e);
                return (null, false);
            }
        }

        if (result?.Session.Error is null)
            return (result, false);

        if (string.Equals(result.Session.Error, "Invalid session key", StringComparison.OrdinalIgnoreCase) || string.Equals(result.Session.Error, "Session timeout", StringComparison.OrdinalIgnoreCase) && !retry)
        {
            _sessionKey = null;
            retry = true;
            goto RETRY_SESSION;
        }

        if (result.Session.Error.StartsWith("Not found", StringComparison.OrdinalIgnoreCase))
            return (null, true);

        _logger.LogInvalidQrzResponse(result.Session.Error);
        return (null, false);
    }

    private async Task GetSessionKeyAsync(CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(_sessionKey))
            return;

        if (string.IsNullOrEmpty(_settings.Username) || string.IsNullOrEmpty(_settings.Password))
        {
            _logger.LogConfigurationError("Qrz XML user name or password is missing.");
            return;
        }

        var response = await _httpClient.GetAsync($"?username={_settings.Username};password={_settings.Password};agent={Agent}", ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        var xmlDoc = new XmlDocument();
        xmlDoc.LoadXml(body);
        var node = xmlDoc.SelectSingleNode("/*[local-name()='QRZDatabase']/*[local-name()='Session']");

        if (node is null)
        {
            _sessionKey = null;
            _logger.LogInvalidQrzResponse(body);
            return;
        }

        var key = node["Key"]?.InnerText;
        if (!string.IsNullOrEmpty(key))
        {
            _sessionKey = key;
            return;
        }

        _logger.LogQrzApiError(node["Error"]?.InnerText, node["Message"]?.InnerText);
        _sessionKey = null;
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}
