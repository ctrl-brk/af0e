using System.Globalization;
using Microsoft.Extensions.Options;

// ReSharper disable once CheckNamespace
namespace HamMarket;

using HamMarket.Settings;

public partial class QthHandler(ILogger<HostedService> logger, IOptions<AppSettings> settings) : IQthHandler
{
    private const int PAGE_SIZE = 10;

    private readonly ILogger _logger = logger;
    private readonly AppSettings _settings = settings.Value;

    private ScanInfo _lastKeywordScan = new() { Date = DateTime.MinValue, Ids = [], OtherIds = [] };
    private ScanInfo _lastCategoryScan = new() { Date = DateTime.MinValue, Ids = [], OtherIds = [] };
    private ScanInfo _thisScan = null!;
    private List<Post> _newPosts = null!;

    private async Task<bool> ScanResults(string msg, ScanType scanType, HttpClient httpClient)
    {
        // ReSharper disable InconsistentNaming
        const string NO_ADS = "There were no ads that matched your search";
        const string START = "Displaying ads ";
        const string TO = " to ";
        const string OF = " of ";
        const string END = " ads ";
        const string AD_START = "<DT><IMG SRC=\"https://swap.qth.com/mdoc";
        const string AD_END = "<br /><br />";
        // ReSharper restore InconsistentNaming

        var lastScan = scanType == ScanType.Keyword ? _lastKeywordScan : _lastCategoryScan;

        if (msg.IndexOf(NO_ADS, StringComparison.Ordinal) > 0) return false;

        var from = msg.IndexOf(START, StringComparison.Ordinal);
        if (from < 0) throw new ApplicationException("Invalid response format");

        var to = msg.IndexOf(TO, from + START.Length, StringComparison.Ordinal);
        if (to < 0 || to - (from + START.Length) > 6) throw new ApplicationException("Invalid response format");

        var of = msg.IndexOf(OF, to + TO.Length, StringComparison.Ordinal);
        if (of < 0 || of - (to + TO.Length) > 6) throw new ApplicationException("Invalid response format");

        var ads = msg.IndexOf(END, of + OF.Length, StringComparison.Ordinal);
        if (ads < 0 || ads - (of + OF.Length) > 6) throw new ApplicationException("Invalid response format");

        var arr = msg.Substring(from + START.Length, ads - (from + START.Length)).Split(' ');

        var startAd = int.Parse(arr[0]);
        //var endAd = int.Parse(arr[2]);
        var totalAds = int.Parse(arr[4]);

        if (startAd > totalAds) return false;

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

            adStartIndex += AD_START.Length;

            var adEndIndex = msg.IndexOf(AD_END, adStartIndex, StringComparison.Ordinal);
            if (adEndIndex < 0) throw new ApplicationException("Invalid response format");

            var post = ProcessPost(msg.Substring(adStartIndex, adEndIndex - adStartIndex), scanType);

            if (post == null) continue; //WTB

            if (post.ActivityDate < lastScan.Date) return false;
            if (post.ActivityDate == lastScan.Date && lastScan.Ids.Any(x => x == post.Id)) return false;

            if (lastScan.OtherIds.Any(x => x == post.Id)) continue; //ignore if listed in other searches before

            if (!string.IsNullOrEmpty(_settings.ResourceUrl))
                await Utils.GetImage(httpClient, _settings, post, "https://swap.qth.com/segamida/thumb_{0}.jpg", _settings.QthCom.Cache);

            lastScan.OtherIds.Add(post.Id);
            _newPosts.Add(post);

            if (_thisScan.Date == DateTime.MinValue)
                _thisScan.Date = post.ActivityDate;

        } while (adStartIndex > 0);

        return true;
    }

    private static Post? ProcessPost(string html, ScanType scanType)
    {
        //var title = scanType == ScanType.Keyword ? "<font size=2 face=arial> - " : "<font size=2 face=arial COLOR=\"#0000FF\">";
        const string title = "<font size=2 face=arial> - ";
        const string END = "</font>";

        var index = 0;

#pragma warning disable IDE0017
        var post = new Post
#pragma warning restore IDE0017
        {
            IsNew = html[0] == '2',
            HasImage = html.IndexOf("camera_icon.gif", index, StringComparison.Ordinal) > 0,
        };

        post.Category = Utils.GetValue(html, """<font size=2 face=arial color=0000FF>""", END, ref index, false);
        post.Title = Utils.GetValue(html, title, END, ref index)!;
        post.Description = Utils.HighlightPrices(Utils.GetValue(html, "<DD><font size=2 face=arial>", END, ref index)!);
        post.Id = int.Parse(Utils.GetValue(html, "<DD><i><font size=2 face=arial>Listing #", " -  ", ref index)!);
        post.SubmittedOn = DateTime.Parse(Utils.GetValue(html, "Submitted on ", " by ", ref index)!);
        post.CallSign = GetCallSign(html, ref index);

        if (post.Title.StartsWith("WTB ", true, null) || post.Title.StartsWith("WTB:", true, null) || post.Title.StartsWith("WTB-", true, null))
            return null;

#pragma warning disable CA1806
        DateTime.TryParse(Utils.GetValue(html, "Modified on ", " - IP:", ref index, false), out var dt);
#pragma warning restore CA1806
        post.ModifiedOn = dt == DateTime.MinValue ? null : dt;

        post.Price = Utils.GetPrice(post);
        if (!string.IsNullOrEmpty(post.Category))
            post.Category = post.Category.ToLower(CultureInfo.InvariantCulture);

        return post;
    }

    private static string? GetCallSign(string src, ref int index)
    {
        var call = Utils.GetValue(src, "Callsign <a", "</a>", ref index, false);
        var ind = call?.IndexOf('>');
        return ind == null ? null : call?[(ind.Value + 1)..];
    }

    private ScanResult? BuildResults(ScanInfo lastScan)
    {
        if (_newPosts.Count == 0)
        {
            _logger.LogDebug("No new posts found");
            return null;
        }

        _logger.LogDebug("{NewPostsCount} new posts", _newPosts.Count);

        string imgFmt;
        if (!string.IsNullOrEmpty(_settings.ResourceUrl) && !string.IsNullOrEmpty(_settings.QthCom.Cache?.ImageFolder))
            imgFmt = $"{_settings.ResourceUrl}/{_settings.QthCom.Cache.ImageFolder}/{{0}}.jpg";
        else
            imgFmt = "https://swap.qth.com/segamida/thumb_{0}.jpg";

        var sb = new StringBuilder();

        foreach (var post in _newPosts)
        {
            sb.AppendLine("<table>");
            sb.AppendLine("  <tr>");
            sb.AppendLine("    <td rowspan='3' class='thumb'>");
            if (post.HasImage) sb.AppendFormat($"    <a href='https://swap.qth.com/view_ad.php?counter={{0}}' target='_blank'><img src='{imgFmt}'></a>{Environment.NewLine}", post.Id);
            sb.AppendLine("    </td>");
            sb.AppendLine("    <td class='title'>");

            sb.Append($"      <a class='link' href='https://swap.qth.com/view_ad.php?counter={post.Id}' target='_blank'>{post.Title}</a>");
            if (post.Price != null) sb.Append($"&nbsp;&nbsp;&nbsp;${post.Price}");
            if (post.Category != null) sb.AppendLine($"<a class='cat' href='https://swap.qth.com/c_{post.Category}.php' target='_blank'>{post.Category}</a>");

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
            sb.Append($"on {post.SubmittedOn:d}");
            if (post.ModifiedOn.HasValue) sb.Append($" Modified: <span class='modified'>{post.ModifiedOn:d}</span>");
            sb.AppendLine("");

            sb.AppendLine("    </td>");
            sb.AppendLine("  </tr>");
            sb.AppendLine("</table>\n");
        }

        return new ScanResult { Title = _settings.QthCom.Title, Items = _newPosts.Count, LastScan = lastScan.Date, Html = sb.ToString() };
    }
}
