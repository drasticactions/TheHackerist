using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace TheHackerist.UI.Tests;

public static class TestHost
{
    public static Window Show(Control control, double width = 1280, double height = 800)
    {
        var window = new Window { Content = control, Width = width, Height = height };
        window.Show();
        Settle(window);
        return window;
    }

    public static void Settle(Window window)
    {
        for (var i = 0; i < 6; i++)
        {
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            window.UpdateLayout();
        }

        Dispatcher.UIThread.RunJobs();
    }

    public static bool Pump(Window window, Func<bool> until, int ms = 2000)
    {
        var start = Environment.TickCount64;
        while (!until())
        {
            if (Environment.TickCount64 - start > ms)
            {
                return false;
            }

            Settle(window);
        }

        return true;
    }

    public static void Click(Window window, Control control, Point? offset = null)
    {
        var local = offset ?? new Point(control.Bounds.Width / 2, control.Bounds.Height / 2);
        var point = control.TranslatePoint(local, window) ?? throw new InvalidOperationException("Control is not in the window.");
        window.MouseDown(point, MouseButton.Left);
        window.MouseUp(point, MouseButton.Left);
        Settle(window);
    }

    public static T Find<T>(Control root, string name)
        where T : Control =>
        root.GetVisualDescendants().OfType<T>().FirstOrDefault(c => c.Name == name)
        ?? throw new InvalidOperationException($"No {typeof(T).Name} named {name}.");

    public static IEnumerable<T> All<T>(Control root) => root.GetVisualDescendants().OfType<T>();
}
