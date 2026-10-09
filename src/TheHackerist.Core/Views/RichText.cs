using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Input;
using Avalonia.Interactivity;
using TheHackerist.Helpers;

namespace TheHackerist.Views;

public static class RichText
{
    public static readonly AttachedProperty<IReadOnlyList<HnTextBlock>?> BlocksProperty =
        AvaloniaProperty.RegisterAttached<SelectableTextBlock, IReadOnlyList<HnTextBlock>?>("Blocks", typeof(RichText));

    public static readonly AttachedProperty<ICommand?> LinkCommandProperty =
        AvaloniaProperty.RegisterAttached<SelectableTextBlock, ICommand?>("LinkCommand", typeof(RichText));

    private static readonly AttachedProperty<LinkMap?> LinksProperty =
        AvaloniaProperty.RegisterAttached<SelectableTextBlock, LinkMap?>("Links", typeof(RichText));

    private const double TapSlop = 6;

    static RichText()
    {
        BlocksProperty.Changed.AddClassHandler<SelectableTextBlock>(OnBlocksChanged);
    }

    public static IReadOnlyList<HnTextBlock>? GetBlocks(SelectableTextBlock element) => element.GetValue(BlocksProperty);

    public static void SetBlocks(SelectableTextBlock element, IReadOnlyList<HnTextBlock>? value) => element.SetValue(BlocksProperty, value);

    public static ICommand? GetLinkCommand(SelectableTextBlock element) => element.GetValue(LinkCommandProperty);

    public static void SetLinkCommand(SelectableTextBlock element, ICommand? value) => element.SetValue(LinkCommandProperty, value);

    public static Uri? LinkAt(SelectableTextBlock text, Point point)
    {
        if (text.GetValue(LinksProperty) is not { Count: > 0 } links)
        {
            return null;
        }

        var local = new Point(point.X - text.Padding.Left, point.Y - text.Padding.Top);
        foreach (var (start, length, url) in links.Ranges)
        {
            foreach (var rect in text.TextLayout.HitTestTextRange(start, length))
            {
                if (rect.Contains(local))
                {
                    return url;
                }
            }
        }

        return null;
    }

    private static void OnBlocksChanged(SelectableTextBlock text, AvaloniaPropertyChangedEventArgs e)
    {
        if (text.GetValue(LinksProperty) is null)
        {
            text.AddHandler(InputElement.PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel | RoutingStrategies.Bubble, handledEventsToo: true);
            text.AddHandler(InputElement.PointerReleasedEvent, OnPointerReleased, RoutingStrategies.Bubble, handledEventsToo: true);
            text.AddHandler(InputElement.PointerMovedEvent, OnPointerMoved, RoutingStrategies.Bubble, handledEventsToo: true);
        }

        var links = new LinkMap();
        text.SetValue(LinksProperty, links);
        text.Inlines = Build(e.GetNewValue<IReadOnlyList<HnTextBlock>?>() ?? [], links);
    }

    private static InlineCollection Build(IReadOnlyList<HnTextBlock> blocks, LinkMap links)
    {
        var inlines = new InlineCollection();
        var length = 0;

        void Add(Run run)
        {
            inlines.Add(run);
            length += run.Text?.Length ?? 0;
        }

        for (var b = 0; b < blocks.Count; b++)
        {
            if (b > 0)
            {
                Add(new Run("\n\n"));
            }

            switch (blocks[b])
            {
                case HnCodeBlock code:
                    var codeRun = new Run(code.Code);
                    codeRun.Classes.Add("code");
                    Add(codeRun);
                    break;
                case HnParagraph paragraph:
                    foreach (var inline in paragraph.Inlines)
                    {
                        var run = new Run(inline.Text);
                        switch (inline)
                        {
                            case HnLink link:
                                run.Classes.Add("link");
                                links.Add(length, inline.Text.Length, link.Url);
                                break;
                            case HnRun { Italic: true }:
                                run.Classes.Add("italic");
                                break;
                        }

                        Add(run);
                    }

                    break;
            }
        }

        return inlines;
    }

    private static Point? s_pressPoint;

    private static void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is SelectableTextBlock text && e.GetCurrentPoint(text).Properties.IsLeftButtonPressed)
        {
            s_pressPoint = e.GetPosition(TopLevel.GetTopLevel(text));
        }
    }

    private static void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (sender is not SelectableTextBlock text || s_pressPoint is not { } pressed)
        {
            return;
        }

        s_pressPoint = null;

        var released = e.GetPosition(TopLevel.GetTopLevel(text));
        if (Math.Abs(released.X - pressed.X) > TapSlop || Math.Abs(released.Y - pressed.Y) > TapSlop)
        {
            return;
        }

        if (LinkAt(text, e.GetPosition(text)) is { } url && GetLinkCommand(text) is { } command && command.CanExecute(url))
        {
            text.ClearSelection();
            command.Execute(url);
            e.Handled = true;
        }
    }

    private static void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (sender is SelectableTextBlock text)
        {
            var overLink = LinkAt(text, e.GetPosition(text)) is not null;
            text.Cursor = overLink ? new Cursor(StandardCursorType.Hand) : null;
        }
    }

    private sealed class LinkMap
    {
        private readonly List<(int Start, int Length, Uri Url)> _ranges = [];

        public int Count => _ranges.Count;

        public IReadOnlyList<(int Start, int Length, Uri Url)> Ranges => _ranges;

        public void Add(int start, int length, Uri url) => _ranges.Add((start, length, url));
    }
}
