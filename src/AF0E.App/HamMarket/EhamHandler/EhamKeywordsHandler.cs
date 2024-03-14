// ReSharper disable once CheckNamespace
namespace HamMarket;

public partial class EhamHandler
{
    public async Task<ScanResult> ProcessKeywordsAsync(HttpClient httpClient, CookieContainer cookies, CancellationToken token)
    {
        _thisScan = new ScanInfo { Ids = [], OtherIds = [] };
        _newPosts = [];
#if DEBUG && CLEARHIST
            File.Delete(_settings.EhamNet.KeywordSearch.ResultFile);
#endif
        if (_settings.EhamNet.KeywordSearch.MaxPosts <= 0) return null;

        var postNum = 0;

        if (File.Exists(_settings.EhamNet.KeywordSearch.ResultFile))
        {
            _lastKeywordScan = JsonConvert.DeserializeObject<ScanInfo>(await File.ReadAllTextAsync(_settings.EhamNet.KeywordSearch.ResultFile, token));
            _lastKeywordScan.OtherIds = [];
        }

        var sessionCookie = await GetSessionCookie(httpClient, cookies);

        while (postNum < _settings.EhamNet.KeywordSearch.MaxPosts)
        {
            _logger.LogDebug("""Fetching \"{KeywordSearchKeywords}\" page {PageSize} of maximum {KeywordSearchMaxPosts} from eHam.net""", _settings.EhamNet.KeywordSearch.Keywords, postNum / PAGE_SIZE + 1, _settings.EhamNet.KeywordSearch.MaxPosts / PAGE_SIZE);

            var uri = new Uri($"https://www.eham.net/classifieds/?view=detail&page={postNum / PAGE_SIZE + 1}");

            using var message = new HttpRequestMessage(HttpMethod.Get, uri);
            message.Headers.Add("Cache-Control", "no-cache");
            message.Headers.Add("Cookie", $"{sessionCookie.Name}={sessionCookie.Value}");
            var res = await httpClient.SendAsync(message, token);

            //var res = await httpClient.GetAsync(uri, token);
            if (token.IsCancellationRequested) break;

            var msg = await res.Content.ReadAsStringAsync(token);
            postNum += PAGE_SIZE;
            if (!await ScanResults(msg, ScanType.Keyword, httpClient)) break;
        }

        _newPosts.ForEach(x => _thisScan.Ids.Add(x.Id));

#if !DEBUG || SAVEHIST
            if (_newPosts.Count > 0)
                await File.WriteAllTextAsync(_settings.EhamNet.KeywordSearch.ResultFile, JsonConvert.SerializeObject(_thisScan), token);
#endif

        return BuildResults(_lastKeywordScan);
    }
}
