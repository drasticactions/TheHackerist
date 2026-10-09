using System.Globalization;
using System.Web;

namespace TheHackerist.Helpers;

public static class HackerNewsLinks
{
    public static long? TryGetItemId(Uri url)
    {
        if (!url.IsAbsoluteUri
            || !url.Host.Equals("news.ycombinator.com", StringComparison.OrdinalIgnoreCase)
            || !url.AbsolutePath.Equals("/item", StringComparison.Ordinal))
        {
            return null;
        }

        var id = HttpUtility.ParseQueryString(url.Query)["id"];
        return long.TryParse(id, NumberStyles.None, CultureInfo.InvariantCulture, out var value) ? value : null;
    }
}
