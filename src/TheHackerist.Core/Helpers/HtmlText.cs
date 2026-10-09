using System.Net;
using System.Text.RegularExpressions;

namespace TheHackerist.Helpers;

public static partial class HtmlText
{
    public static string ToPlainText(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return string.Empty;
        }

        var text = ParagraphRegex().Replace(html, "\n\n");
        text = LineBreakRegex().Replace(text, "\n");
        text = AnyTagRegex().Replace(text, string.Empty);
        text = WebUtility.HtmlDecode(text);
        return text.Trim();
    }

    [GeneratedRegex(@"<p(\s[^>]*)?>", RegexOptions.IgnoreCase)]
    private static partial Regex ParagraphRegex();

    [GeneratedRegex(@"<br\s*/?>", RegexOptions.IgnoreCase)]
    private static partial Regex LineBreakRegex();

    [GeneratedRegex(@"<[^>]+>")]
    private static partial Regex AnyTagRegex();
}
