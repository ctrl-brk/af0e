// ReSharper disable once CheckNamespace
namespace HamMarket;

public interface ISwapHandler
{
    Task<ScanResult> ProcessKeywordsAsync(HttpClient client, CookieContainer cookies, CancellationToken token);
    Task<IEnumerable<ScanResult>> ProcessCategoriesAsync(HttpClient httpClient, CookieContainer cookies, CancellationToken token);
}
