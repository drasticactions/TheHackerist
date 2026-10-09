using System.Net;
using System.Text;

namespace TheHackerist.Helpers;

public abstract record HnTextBlock;

public sealed record HnParagraph(IReadOnlyList<HnInline> Inlines) : HnTextBlock;

public sealed record HnCodeBlock(string Code) : HnTextBlock;

public abstract record HnInline(string Text);

public sealed record HnRun(string Text, bool Italic = false) : HnInline(Text);

public sealed record HnLink(string Text, Uri Url) : HnInline(Text);

public static class HnText
{
    public static IReadOnlyList<HnTextBlock> Parse(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return [];
        }

        var state = new ParseState();
        var i = 0;
        while (i < html.Length)
        {
            var open = html.IndexOf('<', i);
            if (open < 0)
            {
                state.AddText(html[i..]);
                break;
            }

            if (open > i)
            {
                state.AddText(html[i..open]);
            }

            var close = html.IndexOf('>', open + 1);
            if (close < 0)
            {
                state.AddText(html[open..]);
                break;
            }

            state.AddTag(html.AsSpan(open + 1, close - open - 1));
            i = close + 1;
        }

        return state.Finish();
    }

    private sealed class ParseState
    {
        private readonly List<HnTextBlock> _blocks = [];
        private readonly List<HnInline> _inlines = [];
        private readonly StringBuilder _code = new();
        private int _italicDepth;
        private bool _inPre;
        private StringBuilder? _linkText;
        private Uri? _linkUrl;

        public void AddText(string raw)
        {
            var text = WebUtility.HtmlDecode(raw);
            if (_inPre)
            {
                _code.Append(text);
            }
            else if (_linkText is not null)
            {
                _linkText.Append(text);
            }
            else
            {
                AddRun(text);
            }
        }

        public void AddTag(ReadOnlySpan<char> tag)
        {
            var closing = tag.StartsWith("/");
            if (closing)
            {
                tag = tag[1..];
            }

            var nameEnd = tag.IndexOfAny(" \t\r\n/");
            var name = (nameEnd < 0 ? tag : tag[..nameEnd]).ToString().ToLowerInvariant();

            switch (name)
            {
                case "p":
                    if (!_inPre)
                    {
                        EndParagraph();
                    }

                    break;
                case "br":
                    AddText("\n");
                    break;
                case "i" or "em":
                    _italicDepth = Math.Max(0, _italicDepth + (closing ? -1 : 1));
                    break;
                case "pre":
                    if (closing)
                    {
                        EndCode();
                    }
                    else
                    {
                        EndLink();
                        EndParagraph();
                        _inPre = true;
                    }

                    break;
                case "a":
                    if (closing)
                    {
                        EndLink();
                    }
                    else if (!_inPre)
                    {
                        EndLink();
                        _linkText = new StringBuilder();
                        _linkUrl = ReadHref(tag);
                    }

                    break;
            }
        }

        public IReadOnlyList<HnTextBlock> Finish()
        {
            EndLink();
            EndCode();
            EndParagraph();
            return _blocks;
        }

        private void AddRun(string text)
        {
            if (text.Length == 0)
            {
                return;
            }

            var italic = _italicDepth > 0;
            if (_inlines.Count > 0 && _inlines[^1] is HnRun last && last.Italic == italic)
            {
                _inlines[^1] = last with { Text = last.Text + text };
            }
            else
            {
                _inlines.Add(new HnRun(text, italic));
            }
        }

        private void EndLink()
        {
            if (_linkText is null)
            {
                return;
            }

            var text = _linkText.ToString();
            _linkText = null;
            if (_linkUrl is { } url)
            {
                _inlines.Add(new HnLink(text.Length > 0 ? text : url.AbsoluteUri, url));
            }
            else
            {
                AddRun(text);
            }

            _linkUrl = null;
        }

        private void EndCode()
        {
            if (!_inPre)
            {
                return;
            }

            _inPre = false;
            var code = _code.ToString().Trim('\n', '\r');
            _code.Clear();
            if (code.Trim().Length > 0)
            {
                _blocks.Add(new HnCodeBlock(code));
            }
        }

        private void EndParagraph()
        {
            EndLink();

            while (_inlines.Count > 0 && _inlines[0] is HnRun first && first.Text.Trim().Length == 0)
            {
                _inlines.RemoveAt(0);
            }

            while (_inlines.Count > 0 && _inlines[^1] is HnRun end && end.Text.Trim().Length == 0)
            {
                _inlines.RemoveAt(_inlines.Count - 1);
            }

            if (_inlines.Count == 0)
            {
                return;
            }

            if (_inlines[0] is HnRun head)
            {
                _inlines[0] = head with { Text = head.Text.TrimStart() };
            }

            if (_inlines[^1] is HnRun tail)
            {
                _inlines[^1] = tail with { Text = tail.Text.TrimEnd() };
            }

            _blocks.Add(new HnParagraph([.. _inlines]));
            _inlines.Clear();
        }

        private static Uri? ReadHref(ReadOnlySpan<char> tag)
        {
            var attributes = tag.ToString();
            var at = attributes.IndexOf("href", StringComparison.OrdinalIgnoreCase);
            if (at < 0)
            {
                return null;
            }

            var eq = attributes.IndexOf('=', at + 4);
            if (eq < 0)
            {
                return null;
            }

            var start = eq + 1;
            while (start < attributes.Length && char.IsWhiteSpace(attributes[start]))
            {
                start++;
            }

            if (start >= attributes.Length)
            {
                return null;
            }

            string value;
            if (attributes[start] is '"' or '\'')
            {
                var quote = attributes[start];
                var end = attributes.IndexOf(quote, start + 1);
                value = end < 0 ? attributes[(start + 1)..] : attributes[(start + 1)..end];
            }
            else
            {
                var end = attributes.IndexOfAny([' ', '\t', '\r', '\n'], start);
                value = end < 0 ? attributes[start..] : attributes[start..end];
            }

            value = WebUtility.HtmlDecode(value);
            return Uri.TryCreate(value, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
                ? uri
                : null;
        }
    }
}
