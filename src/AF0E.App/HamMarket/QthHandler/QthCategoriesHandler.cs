// ReSharper disable once CheckNamespace
namespace HamMarket;

public partial class QthHandler
{
    public async Task<IEnumerable<ScanResult?>> ProcessCategoriesAsync(HttpClient httpClient, CookieContainer? cookies, CancellationToken token)
    {
        var res = new List<ScanResult?>();

        if (_settings.QthCom.CategorySearch.MaxPosts <= 0)
            return res;

        _thisScan = new ScanInfo {Ids = []};
#if DEBUG && CLEARHIST
            File.Delete(_settings.QthCom.CategorySearch.ResultFile);
#endif
        if (File.Exists(_settings.QthCom.CategorySearch.ResultFile))
        {
            _lastCategoryScan = JsonConvert.DeserializeObject<ScanInfo>(await File.ReadAllTextAsync(_settings.QthCom.CategorySearch.ResultFile, token))!;
            _lastCategoryScan.OtherIds = [.._lastKeywordScan.Ids];
        }

        foreach (var category in _settings.QthCom.CategorySearch.Categories.Split(','))
        {
            if (token.IsCancellationRequested) break;
            _newPosts = [];
            res.Add(await ProcessCategory(httpClient, category, token));
        }

        return res;
    }

    private async Task<ScanResult?> ProcessCategory(HttpClient httpClient, string category, CancellationToken token)
    {
        var postNum = 0;

        while (postNum < _settings.QthCom.CategorySearch.MaxPosts)
        {
            _logger.LogDebug("Fetching {Category} category. Page {PageSize} of maximum {CategorySearchMaxPosts} from qth.com", category, postNum/PAGE_SIZE + 1, _settings.QthCom.CategorySearch.MaxPosts/PAGE_SIZE);

            //var uri = new Uri($"https://swap.qth.com/c_{category}.php?page={postNum/PAGE_SIZE + 1}");
            //var message = new HttpRequestMessage(HttpMethod.Get, uri);
            //message.Headers.Add("Cache-Control", "no-cache");
            //var res = await httpClient.SendAsync(message, token);
            var res = await httpClient.PostAsync("https://swap.qth.com/advsearchresults.php", new StringContent($"category%5B%5D={category}&startnum={postNum}", Encoding.Default, "application/x-www-form-urlencoded"), token);
            var msg = await res.Content.ReadAsStringAsync(token);
            postNum += PAGE_SIZE;
            if (!await ScanResults(msg, ScanType.Category, httpClient))
                break;
        }

        _newPosts.ForEach(x => _thisScan.Ids.Add(x.Id));

#if !DEBUG || SAVEHIST
            if (_newPosts.Count > 0)
                await File.WriteAllTextAsync(_settings.QthCom.CategorySearch.ResultFile, JsonConvert.SerializeObject(_thisScan), token);
#endif
        return BuildResults(_lastCategoryScan);
    }
}
