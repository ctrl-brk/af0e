using AF0E.Functions.DX.Infrastructure;

namespace AF0E.Functions.DX.Activities;

public sealed class DxInfoDotNetActivity
{
    public const string ActivityName = nameof(DxInfoDotNetActivity);

    private readonly ILogger<DxInfoDotNetActivity> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public DxInfoDotNetActivity(ILoggerFactory loggerFactory, IHttpClientFactory httpClientFactory)
    {
        _logger = loggerFactory.CreateLogger<DxInfoDotNetActivity>();
        _httpClientFactory = httpClientFactory;
    }

    [Function(ActivityName)]
#pragma warning disable IDE0060
    public async Task<ScrapeActivityResult> Run([ActivityTrigger] object x)
#pragma warning restore IDE0060
    {
        string? body;

        try
        {
            using var client = _httpClientFactory.CreateClient();

            var uri = new Uri(
                Environment.GetEnvironmentVariable("DX_INFO_NET_URL", EnvironmentVariableTarget.Process) ??
                throw new InvalidOperationException("DX_INFO_NET_URL environment variable is not set"));

            var response = await client.GetAsync(uri);
            body = await response.Content.ReadAsStringAsync();

            body = ExtractJsBody();

            var dates = ExtractDates();

            var callSigns = ExtractCallSigns();

            var info = ExtractInfo();

            List<DxInfo> dxInfo = new();
            var idx = 0;
            foreach (var delimitedSigns in callSigns)
            {
                if (string.IsNullOrEmpty(delimitedSigns)) //spacers
                    continue;

                var infoToAdd = DxInfo.FromMultipleCallsigns(DxInfoSource.DxInfoNet, delimitedSigns).ToArray();
                foreach (var data in infoToAdd)
                {
                    data.BeginDate = dates[idx].Item1;
                    data.EndDate = dates[idx].Item2;
                    data.Description = info[idx].location;
                    data.Links.Add(info[idx].url);
                }

                dxInfo.AddRange(infoToAdd);
                idx++;
            }

            _logger.LogSourceStats(DxInfoSource.DxInfoNet, dxInfo.Count);

            return new ScrapeActivityResult { IsSuccess = true, Result = dxInfo };
        }
        catch (Exception e)
        {
            _logger.LogAppError(e);
            return new ScrapeActivityResult { IsSuccess = false };
        }

        #region Local functions

        string ExtractJsBody()
        {
            var token = "<script>";
            var startIdx = body.IndexOf(token, StringComparison.OrdinalIgnoreCase) + token.Length;
            var endIdx = body.IndexOf("</script>", startIdx, StringComparison.OrdinalIgnoreCase);
            return body[startIdx..endIdx];
        }

        List<Tuple<DateTime, DateTime>> ExtractDates()
        {
            // Parse dates array
            // 0, ,,, - spacer, no duration specified
            // ex: data = [ [[14, 13,,,'#A7FF33'],[0, ,,,'#8FF4FF']],
            //              [[19, 11,,,'#FF8CF7'],[0, ,,,'#FF8F1F']] ];
            var token = "data = [";
            var startIdx = body.IndexOf(token, StringComparison.OrdinalIgnoreCase) + token.Length;
            var endIdx = body.IndexOf("];", startIdx, StringComparison.Ordinal);
            var jsArray = body[startIdx..endIdx];
            jsArray = jsArray.Replace("\n", "").Replace("\r", "").Replace("[", "").Replace("]]", "]").Replace("],", "|");
            var data = jsArray.Split('|');

            // Let's find the month
            token = "IK8LOV Max Laconca";
            startIdx = body.IndexOf(token, endIdx, StringComparison.Ordinal) + token.Length;
            token = "fillText('";
            startIdx = body.IndexOf("fillText('", startIdx, StringComparison.Ordinal) + token.Length;
            endIdx = body.IndexOf('\'', startIdx);
            var monthString = body[startIdx..endIdx];

            var now = DateTime.UtcNow;
            var year = now.Year;
            var month = now.Month;

            var months = new Dictionary<string, int>
            {
                { "JANUARY", 1 }, { "FEBRUARY", 2 }, { "MARCH", 3 }, { "APRIL", 4 }, { "MAY", 5 }, { "JUNE", 6 }, { "JULY", 7 },
                { "AUGUST", 8 }, { "SEPTEMBER", 9 }, { "OCTOBER", 10 }, { "NOVEMBER", 11 }, { "DECEMBER", 12 }
            };
            var publishedMonth = months[monthString];

            if (month != 1 && publishedMonth == 1) //next year
            {
                year++;
                month = 1;
            }
            else
            {
                month = publishedMonth;
            }

            return (from d in data
                    select d.Split(',', StringSplitOptions.TrimEntries)
                    into parts
                    where !string.IsNullOrEmpty(parts[1])
                    let startDay = int.Parse(parts[0]) + 1
                    let endDay = startDay + int.Parse(parts[1]) - 1
                    select new Tuple<DateTime, DateTime>(new DateTime(year, month, startDay), new DateTime(year, month, endDay, 23, 59, 59)))
                .ToList();
        }

        IEnumerable<string> ExtractCallSigns()
        {
            // Callsigns
            // ex: var labels = ['VK9XY','','FW5N','','','H44WA','','XW4DX','','7O73T','ZL7IO','','','9L5M','','5H3FM','3B9/M0CFW','TO9W','J79AN, J79BH','', 'A31DL & A31DK'];
            var token = "var labels = [";
            var startIdx = body.IndexOf(token, StringComparison.OrdinalIgnoreCase) + token.Length;
            var endIdx = body.IndexOf(']', startIdx);
            return body[startIdx..endIdx].Split("','").Select(s => s.Replace("'", ""));
        }

        List<(string location, string url)> ExtractInfo()
        {
            var result = new List<(string, string)>();

            var token = ".set('tooltips', [";
            var startIdx = body.IndexOf(token, StringComparison.OrdinalIgnoreCase);
            token = "<b>";
            startIdx = body.IndexOf(token, startIdx, StringComparison.OrdinalIgnoreCase) + token.Length;
            var endIdx = body.IndexOf("])", startIdx, StringComparison.Ordinal);
            var tooltips = body[startIdx..endIdx].Split("<b>");

            foreach (var tt in tooltips)
            {
                token = "</b>";
                endIdx = tt.IndexOf(token, StringComparison.OrdinalIgnoreCase);
                var location = tt[..endIdx];
                if (string.IsNullOrEmpty(location))
                    continue;

                token = @"<a href=\""";
                startIdx = tt.IndexOf(token, endIdx, StringComparison.OrdinalIgnoreCase) + token.Length;
                token = @"/\";
                endIdx = tt.IndexOf(token, startIdx + token.Length, StringComparison.OrdinalIgnoreCase);

                result.Add((location, tt[startIdx..endIdx]));
            }

            return result;
        }

        #endregion
    }
}
