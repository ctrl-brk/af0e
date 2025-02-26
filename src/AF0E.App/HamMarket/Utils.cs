namespace HamMarket;

using HamMarket.Settings;

public static class Utils
{
    internal static string? GetValue(string src, string startToken, string endToken, ref int index, bool req = true)
    {
        var start = src.IndexOf(startToken, index, StringComparison.OrdinalIgnoreCase);
        if (start < 0)
        {
            if (!req)
                return null;

            throw new ApplicationException("Invalid response format");
        }
        var end = src.IndexOf(endToken, start + startToken.Length, StringComparison.OrdinalIgnoreCase);
        if (end < 0) throw new ApplicationException("Invalid response format");

        index = end + endToken.Length;

        var res = src[(start + startToken.Length)..end];

        return res;
    }

    internal static string HighlightPrices(string text)
    {
        var prevInd = 0;
        var ind = text.IndexOf('$');

        if (ind < 0) return text;

        var result = new StringBuilder();

        var delimiters = new[] { ' ', ',', '.' };
        while (ind >= 0)
        {
            result.Append(text.AsSpan(prevInd, ind - prevInd));
            result.Append("<span class='price'>$");
            ind++;
            var cnt = 0;
            for (; cnt < 15 && ind < text.Length && (char.IsNumber(text[ind]) || delimiters.Contains(text[ind])); cnt++)
            {
                result.Append(text[ind++]);
            }
            result.Append("</span>");

            prevInd = ind;

            if (ind + cnt >= text.Length) break;

            ind = text.IndexOf('$', ind + cnt);
        }

        if (ind < text.Length)
            result.Append(text.AsSpan(prevInd));

        return result.ToString();
    }

    internal static string? GetPrice(Post post)
    {
        var value = "";
        var cnt = 0;

        var ind = post.Description.LastIndexOf('$');
        if (ind < 0)
            return null;

        ind++;
        var delimiters = new[] { ' ', ',', '.' };
        while (cnt < 15 && ind < post.Description.Length && (char.IsNumber(post.Description[ind]) || delimiters.Contains(post.Description[ind])))
        {
            value += post.Description[ind++];
            cnt++;
        }

        return value.Length > 0 ? value.Trim() : null;
    }

    internal static async Task GetImage(HttpClient httpClient, AppSettings settings, Post post, string format, Cache? cache)
    {
        if (!post.HasImage || string.IsNullOrEmpty(cache?.ImageFolder)) return;

        var fName = $"{settings.ResourceFolder}/{cache.ImageFolder}/{post.Id}.jpg";
        if (File.Exists(fName)) return;

        var uri = new Uri(string.Format(format, post.Id));

        using var res = await httpClient.GetAsync(uri);
        res.EnsureSuccessStatusCode();

        var data = await res.Content.ReadAsByteArrayAsync();
        Directory.CreateDirectory(cache.ImageFolder);
        await File.WriteAllBytesAsync(fName, data);
    }
}
