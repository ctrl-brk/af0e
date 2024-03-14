// ReSharper disable once CheckNamespace
namespace HamMarket;

public partial class QthHandler
{
    public async Task<ScanResult> ProcessKeywordsAsync(HttpClient httpClient, CookieContainer cookies, CancellationToken token)
    {
        if (_settings.QthCom.KeywordSearch.MaxPosts <= 0) return null;

        _thisScan = new ScanInfo { Ids = [], OtherIds = [] };
        _newPosts = [];
#if DEBUG && CLEARHIST
            File.Delete(_settings.QthCom.KeywordSearch.ResultFile);
#endif
        int startIndex = 0, postNum = 0;
        var uri = new Uri("https://swap.qth.com/advsearchresults.php");

        if (File.Exists(_settings.QthCom.KeywordSearch.ResultFile))
        {
            _lastKeywordScan = JsonConvert.DeserializeObject<ScanInfo>(await File.ReadAllTextAsync(_settings.QthCom.KeywordSearch.ResultFile, token));
            _lastKeywordScan.OtherIds = new List<int>();
        }

        while (postNum < _settings.QthCom.KeywordSearch.MaxPosts)
        {
            var formData = new List<KeyValuePair<string, string>>
            {
                new("anywords", string.Join(' ', _settings.QthCom.KeywordSearch.Keywords.Split(',')))
            };

            if (startIndex > 0)
            {
                formData.AddRange(new[]
                {
                    new KeyValuePair<string, string>("startnum", startIndex.ToString()),
                    new KeyValuePair<string, string>("submit", "Next 10 Ads")
                });
            }

            using var content = new FormUrlEncodedContent(formData);

            _logger.LogDebug("""Fetching \"{KeywordSearchKeywords}\" keywords. Page {PageSize} of maximum {KeywordSearchMaxPosts} from qth.com""", _settings.QthCom.KeywordSearch.Keywords, postNum/PAGE_SIZE + 1, _settings.QthCom.KeywordSearch.MaxPosts/PAGE_SIZE);
            var res = await httpClient.PostAsync(uri, content, token);
            if (token.IsCancellationRequested) break;
            var msg = await res.Content.ReadAsStringAsync(token);
            startIndex += 10;
            postNum += PAGE_SIZE;

            if (await ScanResults(msg, ScanType.Keyword, httpClient))
                continue;
            
            _logger.LogDebug("No results found");
            break;
        }

        _newPosts.ForEach(x => _thisScan.Ids.Add(x.Id));

#if !DEBUG || SAVEHIST
            if (_newPosts.Count > 0)
                await File.WriteAllTextAsync(_settings.QthCom.KeywordSearch.ResultFile, JsonConvert.SerializeObject(_thisScan), token);
#endif
        _lastKeywordScan.Ids = [.._thisScan.Ids];

        return BuildResults(_lastKeywordScan);
    }
}
