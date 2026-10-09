using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace TheHackerist.Converters;

public sealed class DepthToThicknessConverter : IValueConverter
{
    public static readonly DepthToThicknessConverter Instance = new();

    public double Step { get; set; } = 14;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        new Thickness(value is int depth ? depth * Step : 0, 0, 0, 0);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
