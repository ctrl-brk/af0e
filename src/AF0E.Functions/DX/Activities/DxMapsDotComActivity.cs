using System.Text.RegularExpressions;
using AF0E.Functions.DX.Infrastructure;

namespace AF0E.Functions.DX.Activities;

public sealed class DxMapsDotComActivity(ILoggerFactory loggerFactory, IHttpClientFactory httpClientFactory)
{
    public const string ActivityName = nameof(DxMapsDotComActivity);

    private readonly ILogger<DxMapsDotComActivity> _logger = loggerFactory.CreateLogger<DxMapsDotComActivity>();

    [Function(ActivityName)]
#pragma warning disable IDE0060
    public async Task<ScrapeActivityResult> Run([ActivityTrigger] object x)
#pragma warning restore IDE0060
    {
        try
        {
            using var client = httpClientFactory.CreateClient();

            var uri = new Uri(
                Environment.GetEnvironmentVariable("DX_MAPS_COM_URL", EnvironmentVariableTarget.Process) ??
                throw new InvalidOperationException("DX_MAPS_COM_URL environment variable is not set"));

            //KeyValuePair<string, string>[] formValues = { new("View", "30DAY"), new("DXCC", "0"), new("CQ", "ALL"), new("Mode", "ALL"), new("Band", "HF") };
            //var response = await client.PostAsync(uri, new FormUrlEncodedContent(formValues));
            var response = await client.GetAsync(uri);
            var body = await response.Content.ReadAsStringAsync();

            body = ExtractTableBody(body);

            var ret = string.IsNullOrEmpty(body) ? new List<DxInfo>() : ParseDxData(body);

            _logger.LogSourceStats(DxInfoSource.DxMapsCom, ret.Count);

            return new ScrapeActivityResult { IsSuccess = true, Result = ret };
        }
        catch (Exception e)
        {
            _logger.LogAppError(e);
            return new ScrapeActivityResult { IsSuccess = false };
        }
    }

    private static string ExtractTableBody(string body)
    {
        body = Regex.Replace(body, "<!--.*?-->", string.Empty, RegexOptions.Singleline);

        var startIdx = body.IndexOf("<tr>", StringComparison.OrdinalIgnoreCase);

        if (startIdx < 0) // assuming there will be no rows if there are no DX for this period
            return string.Empty;

        var endIdx = body.IndexOf("</table>", startIdx, StringComparison.OrdinalIgnoreCase);
        return body[startIdx..endIdx];
    }

    private static List<DxInfo> ParseDxData(string html)
    {
        Dictionary<string, DxInfo> callsWithData = new();

        var rows = html.Split("<tr>").Where(x => !string.IsNullOrEmpty(x));
        foreach (var row in rows)
        {
            var cells = row.Split("<td ").Where(x => !string.IsNullOrEmpty(x)).ToArray();
            var startIdx = cells[0].IndexOf('>') + 1;
            var endIdx = cells[0].IndexOf('<', startIdx);
            var dateStr = cells[0][startIdx..endIdx].Replace("&nbsp;", " ");
            var beginDate = DateTime.SpecifyKind(DateTime.Parse(dateStr), DateTimeKind.Utc);
            var endDate = beginDate.AddSeconds(23 * 3600 + 59 * 60 + 59); //23:59:59

            for (var i = 1; i < cells.Length; i++)
            {
                var cell = cells[i];

                var token = "href=\"";
                startIdx = cell.IndexOf(token, StringComparison.Ordinal);

                if (startIdx < 0)
                    continue;

                startIdx += token.Length;

                endIdx = cell.IndexOf('"', startIdx);
                var url = cell[startIdx..endIdx];

                startIdx = endIdx + 2;
                endIdx = cell.IndexOf('<', startIdx);
                var callSign = cell[startIdx..endIdx];

                if (callsWithData.TryGetValue(callSign, out var existing))
                {
                    existing.EndDate = endDate;
                }
                else
                {
                    var info = new DxInfo(DxInfoSource.DxMapsCom, callSign) {BeginDate = beginDate, EndDate = endDate};
                    info.Links.Add($"https://www.dxmaps.com/{url}");
                    callsWithData.Add(callSign, info);
                }
            }
        }

        return callsWithData.Select(x => x.Value).ToList();
    }
}
