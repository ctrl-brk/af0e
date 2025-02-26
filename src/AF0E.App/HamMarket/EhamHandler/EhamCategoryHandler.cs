// ReSharper disable once CheckNamespace
namespace HamMarket;

public partial class EhamHandler
{
    public async Task<IEnumerable<ScanResult?>> ProcessCategoriesAsync(HttpClient httpClient, CookieContainer cookies, CancellationToken token)
    {
        var res = new List<ScanResult?>();

        if (_settings.EhamNet.CategorySearch.MaxPosts <= 0) return res;

        _thisScan = new ScanInfo { Ids = [] };
#if DEBUG && CLEARHIST
            File.Delete(_settings.EhamNet.CategorySearch.ResultFile);
#endif
        if (File.Exists(_settings.EhamNet.CategorySearch.ResultFile))
        {
            _lastCategoryScan = JsonConvert.DeserializeObject<ScanInfo>(await File.ReadAllTextAsync(_settings.EhamNet.CategorySearch.ResultFile, token))!;
            _lastCategoryScan.OtherIds = [.._lastKeywordScan.Ids];
        }



        foreach (var category in _settings.EhamNet.CategorySearch.Categories.Split(','))
        {
            if (token.IsCancellationRequested) break;
            _newPosts = [];
            res.Add(await ProcessCategory(httpClient, category, cookies, token));
        }

        return res;
    }

    private async Task<ScanResult?> ProcessCategory(HttpClient httpClient, string category, CookieContainer cookies, CancellationToken token)
    {
        _logger.LogDebug("Fetching {Category} category from eHam.net", category);

        var uri = new Uri($"https://www.eham.net/classifieds/view-category?id={_categories.First(x => x.Key.Equals(category, StringComparison.OrdinalIgnoreCase)).Value}");

        var sessionCookie = await GetSessionCookie(httpClient, cookies);

        using var message = new HttpRequestMessage(HttpMethod.Get, uri);
        message.Headers.Add("Cache-Control", "no-cache");
        message.Headers.Add("Cookie", $"{sessionCookie.Name}={sessionCookie.Value}");

        var res = await httpClient.SendAsync(message, token);
        if (token.IsCancellationRequested)
            return null;

        var msg = await res.Content.ReadAsStringAsync(token);
        if (!await ScanResults(msg, ScanType.Category, httpClient))
            return null;

        _newPosts.ForEach(x => _thisScan.Ids.Add(x.Id));

#if !DEBUG || SAVEHIST
            if (_newPosts.Count > 0)
                await File.WriteAllTextAsync(_settings.EhamNet.CategorySearch.ResultFile, JsonConvert.SerializeObject(_thisScan), token);
#endif
        return BuildResults(_lastCategoryScan);
    }
}
