using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Options;

// ReSharper disable once CheckNamespace
namespace HamMarket;

public partial class EhamHandler(ILogger<HostedService> logger, IOptions<AppSettings> settings) : IEhamHandler
{
    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    private readonly Dictionary<string, int> _categories = new()
    {
        {"amplifier parts", 67},
        {"antiques", 48},
        {"atv", 71},
        {"audio processors", 70},
        {"books &amp; magazines", 52},
        {"commercial radio", 69},
        {"components", 56},
        {"computers", 61},
        {"crystals", 73},
        {"digital / pacto", 43},
        {"dx rentals", 80},
        {"filters", 78},
        {"ham homes", 60},
        {"hf amplifiers", 37},
        {"hf antennas", 35},
        {"hf radios", 39},
        {"keyers &amp; keys", 44},
        {"manuals", 63},
        {"microwave components", 64},
        {"mics &amp; headsets", 53},
        {"military/surplus", 65},
        {"misc", 55},
        {"packet", 79},
        {"power supplies", 49},
        {"qrp", 68},
        {"receivers", 76},
        {"repeaters", 66},
        {"rotators", 45},
        {"satellite", 75},
        {"scams", 74},
        {"scanners", 59},
        {"sstv", 50},
        {"switches", 54},
        {"test equipment", 57},
        {"towers &amp; access", 42},
        {"tubes", 62},
        {"tuners", 46},
        {"vhf/uhf amplifiers", 38},
        {"vhf/uhf antennas", 36},
        {"vhf/uhf radios", 40},
        {"wanted", 72},
        {"watt meters", 47}
    };

    private const int PAGE_SIZE = 20;

    private readonly ILogger _logger = logger;
    private readonly AppSettings _settings = settings.Value;

    private static Cookie _sessionCookie;
    private ScanInfo _lastKeywordScan = new() { Date = DateTime.MinValue, Ids = [], OtherIds = [] };
    private ScanInfo _lastCategoryScan = new() { Date = DateTime.MinValue, Ids = [], OtherIds = [] };
    private ScanInfo _thisScan;
    private List<Post> _newPosts;

    private static async Task<Cookie> GetSessionCookie(HttpClient httpClient, CookieContainer cookies)
    {
        if (_sessionCookie != null) return _sessionCookie;

        var uri = new Uri("https://www.eham.net/classifieds/?view=all");

        var res = await httpClient.GetAsync(uri);

        //var responseCookies = cookies.GetCookies(uri).Cast<Cookie>();
        //var sessionCookie = responseCookies.FirstOrDefault(x => x.Name == "ehamsid");
        var (key, value) = res.Headers.FirstOrDefault(x => x.Key == "Set-Cookie");
        if (key == null) throw new ApplicationException("Invalid response format");
        var val = value.First().Split(';')[0].Split('=');
        _sessionCookie = new Cookie(val[0], val[1]);

        return _sessionCookie;
    }

    private async Task<bool> ScanResults(string msg, ScanType scanType, HttpClient httpClient)
    {
        // ReSharper disable InconsistentNaming
        const string AD_START = "<tr data-key=\"";
        // ReSharper restore InconsistentNaming

        var lastScan = scanType == ScanType.Keyword ? _lastKeywordScan : _lastCategoryScan;

        var keys = _settings.EhamNet.KeywordSearch.Keywords.Split(',');
        var adStartIndex = 0;
        var cnt = 0;

        do
        {
            adStartIndex = msg.IndexOf(AD_START, adStartIndex, StringComparison.Ordinal);

            if (adStartIndex < 0)
            {
                if (cnt == 0) throw new ApplicationException("Invalid response format");
                break;
            }

            cnt++;

            if (scanType == ScanType.Category && cnt > _settings.EhamNet.CategorySearch.MaxPosts) break;

            adStartIndex += AD_START.Length;
            adStartIndex += msg.IndexOf('"', adStartIndex) - adStartIndex + 2;

            var adEndIndex = msg.IndexOf(AD_START, adStartIndex, StringComparison.Ordinal);
            if (adEndIndex < 0) adEndIndex = msg.IndexOf("<ul class=\"pagination\">", adStartIndex, StringComparison.Ordinal);
                
            if (adEndIndex < 0) throw new ApplicationException("Invalid response format");

            var body = msg.Substring(adStartIndex, adEndIndex - adStartIndex);
                
            if (scanType == ScanType.Keyword)
            {
                var found = keys.Any(key => body.Contains(key, StringComparison.OrdinalIgnoreCase));
                if (!found) continue;
            }

            var post = ProcessPost(body, scanType);

            if (post == null) continue; //WTB

            if (post.ActivityDate < lastScan.Date) return false;
            if (post.ActivityDate == lastScan.Date && lastScan.Ids.IndexOf(post.Id) >= 0) return false;

            if (lastScan.OtherIds != null && lastScan.OtherIds.IndexOf(post.Id) >= 0) continue; //ignore if listed in other searches before

            if (!string.IsNullOrEmpty(_settings.ResourceUrl))
                await Utils.GetImage(httpClient, _settings, post, "https://www.eham.net/data/classifieds/images/{0}.t.jpg", _settings.EhamNet.Cache);

            _newPosts.Add(post);

            if (_thisScan.Date == DateTime.MinValue)
                _thisScan.Date = post.ActivityDate;

        } while (adStartIndex > 0);

        return true;
    }

    private static Post ProcessPost(string html, ScanType scanType)
    {
        var index = 0;

        var post = new Post
        {
            IsNew = true,
            Id = int.Parse(Utils.GetValue(html, $"href=\"/classifieds/", "\"", ref index)),
            Title = Utils.GetValue(html, ">", "</a>", ref index),
            SubmittedOn = DateTime.Parse(Utils.GetValue(html, "User IP</th>\n\t\t</tr><tr><td>", scanType == ScanType.Keyword ? "</td>" : "<" , ref index)),
            //CallSign = Utils.GetValue(html, "profile/", "\"", ref index),
            Category = Utils.GetValue(html, "\">", "</a>", ref index),
            HasImage = html.IndexOf("<img alt style", index, StringComparison.Ordinal) < 0 && html.IndexOf(";base64,TUNRS1", index, StringComparison.Ordinal) < 0,
            Description = Utils.HighlightPrices(Utils.GetValue(html, "float:right\">", "</div>", ref index)),
        };

        if (post.Title.StartsWith("WTB ", true, null) || post.Title.StartsWith("WTB:", true, null) || post.Title.StartsWith("WTB-", true, null)) return null;

        post.Price = Utils.GetPrice(post);
        if (!string.IsNullOrEmpty(post.Category)) post.Category = post.Category.ToLower();

        return post;
    }

    private ScanResult BuildResults(ScanInfo lastScan)
    {
        if (_newPosts.Count == 0)
        {
            _logger.LogDebug("No new posts found");
            return null;
        }

        _logger.LogDebug("{NewPostsCount} new posts", _newPosts.Count);

        string imgFmt;
        if (!string.IsNullOrEmpty(_settings.ResourceUrl) && !string.IsNullOrEmpty(_settings.EhamNet.Cache?.ImageFolder))
            imgFmt = $"{_settings.ResourceUrl}/{_settings.EhamNet.Cache.ImageFolder}/{{0}}.jpg";
        else
            imgFmt = "https://www.eham.net/data/classifieds/images/{0}.t.jpg";

        var sb = new StringBuilder();

        foreach (var post in _newPosts)
        {
            sb.AppendLine("<table>");
            sb.AppendLine("  <tr>");
            sb.AppendLine("    <td rowspan='3' class='thumb'>");
            if (post.HasImage) sb.AppendFormat($"    <a href='https://www.eham.net/classifieds/detail/{{0}}' target='_blank'><img src='{imgFmt}'></a>{Environment.NewLine}", post.Id);
            sb.AppendLine("    </td>");
            sb.AppendLine("    <td class='title'>");

            sb.Append($"      <a class='link' href='https://www.eham.net/classifieds/detail/{post.Id}' target='_blank'>{post.Title}</a>");
            if (post.Price != null) sb.Append($"&nbsp;&nbsp;&nbsp;${post.Price}");
            var category = _categories.FirstOrDefault(x => x.Key == post.Category);
            if (category.Key != null) sb.AppendLine($"<a class='cat' href='https://www.eham.net/classifieds/results/{category.Value}' target='_blank'>{post.Category}</a>");

            sb.AppendLine("    </td>");
            sb.AppendLine("  </tr>");
            sb.AppendLine("  <tr class='content'>");
            sb.AppendLine("    <td>");
            sb.AppendLine($"      {post.Description}");
            sb.AppendLine("    </td>");
            sb.AppendLine("  </tr>");
            sb.AppendLine("  <tr>");
            sb.AppendLine("    <td class='info'>");

            sb.Append("      Submitted ");
            if (post.CallSign != null)
                sb.Append($"by <a class='call' href='https://www.qrz.com/lookup?tquery={post.CallSign}&mode=callsign' target='_blank'>{post.CallSign}</a> ");
            sb.AppendLine($"on {post.SubmittedOn:d}");

            sb.AppendLine("    </td>");
            sb.AppendLine("  </tr>");
            sb.AppendLine("</table>\n");
        }

        return new ScanResult { Title = _settings.EhamNet.Title, Items = _newPosts.Count, LastScan = lastScan.Date, Html = sb.ToString() };
    }
}
