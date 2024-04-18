using AF0E.Functions.DX.Infrastructure;

namespace AF0E.Functions.DX.Activities;

public sealed class Va3RjActivity(ILoggerFactory loggerFactory, IHttpClientFactory httpClientFactory)
{
    public const string ActivityName = nameof(Va3RjActivity);

    private readonly ILogger<Va3RjActivity> _logger = loggerFactory.CreateLogger<Va3RjActivity>();

    [Function(ActivityName)]
    public async Task<ScrapeActivityResult> Run([ActivityTrigger] object x)
    {
        try
        {
            using var client = httpClientFactory.CreateClient();

            var uri = new Uri(Environment.GetEnvironmentVariable("VA3RJ_URL", EnvironmentVariableTarget.Process) ?? throw new InvalidOperationException("VA3RJ_URL environment variable is not set"));

            var response = await client.GetAsync(uri);
            var body = await response.Content.ReadAsStringAsync();

            body = ExtractTableBody(body);

            var ret = ParseDxData(body);

            _logger.LogSourceStats(DxInfoSource.VA3RJ, ret.Count);

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
        var startIdx = body.IndexOf("<th>Call<th>", StringComparison.OrdinalIgnoreCase);
        startIdx = body.IndexOf("<tr ", startIdx, StringComparison.OrdinalIgnoreCase);

        var endIdx = body.IndexOf("</table>", startIdx, StringComparison.OrdinalIgnoreCase);
        return body[startIdx..endIdx];
    }

    private static List<DxInfo> ParseDxData(string html)
    {
        List<DxInfo> results = [];

        var rows = html.Split("<tr ").Where(x => !string.IsNullOrEmpty(x));
        foreach (var row in rows)
        {
            var cells = row.Split("<td").Select(x => x.TrimEnd('\r', '\n')).ToArray();

            var callSign = cells[1].Replace(">", "");

            var startIdx = cells[2].IndexOf('>');
            var dxcc = cells[2][(startIdx+1)..];

            startIdx = cells[3].IndexOf('>');
            var iota = cells[3][(startIdx+1)..];

            startIdx = cells[4].IndexOf('>');
            var beginDate = ParseDate(cells[4][(startIdx + 1)..], out var beginDateSet);


            DateTime endDate;
            var endDateSet = false;
            startIdx = cells[5].IndexOf('>');
            try
            {
                endDate = ParseDate(cells[5][(startIdx + 1)..], out endDateSet);
            }
            catch
            {
                endDate = DateTime.Now.AddMonths(1);
            }

            if (endDate > DateTime.Now.AddMonths(1))
            {
                var strDate = DateTime.Now.AddMonths(1).AddDays(-1).ToShortDateString();
                endDate = DateTime.Parse(strDate);
            }

            startIdx = cells[6].IndexOf('>');
            var desc = cells[6][(startIdx+1)..].Replace("<br>", " ");

            results.Add(new DxInfo(DxInfoSource.VA3RJ, callSign)
            {
                BeginDate = beginDate,
                BeginDateSet = beginDateSet,
                EndDate = endDate,
                EndDateSet = endDateSet,
                DXCC = dxcc,
                IOTA = iota,
                Description = desc
            });
        }

        return results;
    }

    private static DateTime ParseDate(string dateStr, out bool isSet)
    {
        isSet = true;

        var startIdx = dateStr.IndexOf('>'); // <td><font color="gray">2023-12-31?</font>

        if (startIdx < 0)
            return TryParseDate(dateStr);

        var endIdx = dateStr.IndexOf('?', startIdx);
        dateStr = dateStr.Substring(startIdx+1, endIdx - startIdx - 1);
        isSet = false;

        return TryParseDate(dateStr);
    }

    private static DateTime TryParseDate(string dateStr)
    {
        if (DateTime.TryParse(dateStr, out var parsedDate))
            return parsedDate;

        //seen dates like 2024-31-12

        var dateParts = dateStr.Split('-');
        dateStr = $"{dateParts[0]}-{dateParts[2]}-{dateParts[1]}";
        return DateTime.Parse(dateStr);
    }
}
