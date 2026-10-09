using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace TheHackerist.Views;

public sealed class DepthRails : Control
{
    public static readonly StyledProperty<int> DepthProperty =
        AvaloniaProperty.Register<DepthRails, int>(nameof(Depth));

    public static readonly StyledProperty<double> StepProperty =
        AvaloniaProperty.Register<DepthRails, double>(nameof(Step), 14);

    public static readonly StyledProperty<IBrush?> BrushProperty =
        AvaloniaProperty.Register<DepthRails, IBrush?>(nameof(Brush));

    static DepthRails()
    {
        AffectsRender<DepthRails>(DepthProperty, StepProperty, BrushProperty);
    }

    public int Depth
    {
        get => GetValue(DepthProperty);
        set => SetValue(DepthProperty, value);
    }

    public double Step
    {
        get => GetValue(StepProperty);
        set => SetValue(StepProperty, value);
    }

    public IBrush? Brush
    {
        get => GetValue(BrushProperty);
        set => SetValue(BrushProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        if (Brush is null || Depth <= 0)
        {
            return;
        }

        var pen = new Pen(Brush, 1);
        var height = Bounds.Height;
        for (var level = 0; level < Depth; level++)
        {
            var x = Math.Floor(level * Step + Step / 2) + 0.5;
            context.DrawLine(pen, new Point(x, 0), new Point(x, height));
        }
    }
}
