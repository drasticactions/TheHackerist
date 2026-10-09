using AvaEnyo;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.VisualTree;
using TheHackerist.ViewModels;

namespace TheHackerist.Views;

public partial class ReaderView : UserControl
{
    internal static TimeSpan StartTimeout { get; set; } = TimeSpan.FromSeconds(3);

    internal static Func<Control?> WebViewFactory { get; set; } = () =>
    {
        if (!CanEmbedWebView())
        {
            return null;
        }

        var web = new NativeWebView();
        web.EnvironmentRequested += OnEnvironmentRequested;
        return web;
    };

    internal static Func<Control, Task<bool>> StartProbe { get; set; } = WaitForAdapterAsync;

    private static void OnEnvironmentRequested(object? sender, WebViewEnvironmentRequestedEventArgs e)
    {
        switch (e)
        {
            case LinuxWpeWebViewEnvironmentRequestedEventArgs wpe:
                wpe.PreferWebKitGtkInstead = true;
                break;
            case GtkWebViewEnvironmentRequestedEventArgs gtk:
                gtk.ExperimentalOffscreen = true;
                break;
        }
    }

    private ReaderViewModel? _reader;
    private Control? _web;
    private Point? _nativeOrigin;

    public ReaderView()
    {
        InitializeComponent();
        AddHandler(EnyoBack.BackRequestedEvent, OnBackRequested);
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (_reader is not null)
        {
            _reader.ActionRequested -= OnActionRequested;
        }

        _reader = DataContext as ReaderViewModel;
        if (_reader is not null)
        {
            _reader.ActionRequested += OnActionRequested;
        }

        if (this.IsAttachedToVisualTree())
        {
            Start();
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Start();
    }

    private void Start()
    {
        if (_reader is not { } reader || _web is not null)
        {
            return;
        }

        _web = WebViewFactory();
        if (_web is null)
        {
            _ = reader.ReportWebViewUnavailableAsync();
            return;
        }

        if (_web is NativeWebView web)
        {
            web.NavigationStarted += OnNavigationStarted;
            web.NavigationCompleted += OnNavigationCompleted;
            web.NewWindowRequested += OnNewWindowRequested;
            web.LayoutUpdated += OnWebLayoutUpdated;
            reader.IsLoading = true;
            web.Source = reader.Url;
        }

        WebHost.Content = _web;
        _ = ProbeAsync(reader, _web);
    }

    private static bool CanEmbedWebView() => true;

    private static async Task ProbeAsync(ReaderViewModel reader, Control web)
    {
        if (!await StartProbe(web))
        {
            await reader.ReportWebViewUnavailableAsync();
        }
    }

    private static async Task<bool> WaitForAdapterAsync(Control control)
    {
        if (control is not NativeWebView web)
        {
            return false;
        }

        var started = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        void OnCreated(object? sender, WebViewAdapterEventArgs e) => started.TrySetResult(true);

        void OnUnhandled(object? sender, DispatcherUnhandledExceptionEventArgs e)
        {
            if (IsWebViewStartFailure(e.Exception))
            {
                e.Handled = true;
                started.TrySetResult(false);
            }
        }

        void OnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == NativeWebView.AdapterInfoProperty && IsUnavailable(web.AdapterInfo))
            {
                started.TrySetResult(false);
            }
        }

        web.AdapterCreated += OnCreated;
        web.PropertyChanged += OnPropertyChanged;
        Dispatcher.UIThread.UnhandledException += OnUnhandled;
        try
        {
            if (IsUnavailable(web.AdapterInfo))
            {
                return false;
            }

            var finished = await Task.WhenAny(started.Task, Task.Delay(StartTimeout));
            return finished == started.Task && started.Task.Result;
        }
        finally
        {
            web.AdapterCreated -= OnCreated;
            web.PropertyChanged -= OnPropertyChanged;
            Dispatcher.UIThread.UnhandledException -= OnUnhandled;
        }
    }

    private static bool IsWebViewStartFailure(Exception exception) =>
        exception.StackTrace?.Contains("Avalonia.Controls.NativeWebView.OnAttached", StringComparison.Ordinal) == true
        || (exception.InnerException is { } inner && IsWebViewStartFailure(inner));

    private static bool IsUnavailable(WebViewAdapterInfo? info) =>
        info is DetailedWebViewAdapterInfo { IsSupported: false } or DetailedWebViewAdapterInfo { IsInstalled: false };

    private void OnWebLayoutUpdated(object? sender, EventArgs e)
    {
        if (sender is not NativeWebView web
            || web.GetVisualChildren().OfType<NativeControlHost>().FirstOrDefault() is not { } host
            || TopLevel.GetTopLevel(host) is not { } top
            || host.TranslatePoint(default, top) is not { } origin)
        {
            return;
        }

        if (origin != _nativeOrigin)
        {
            _nativeOrigin = origin;
            host.TryUpdateNativeControlPosition();
        }
    }

    private void OnNavigationStarted(object? sender, WebViewNavigationStartingEventArgs e)
    {
        if (_reader is not null)
        {
            _reader.IsLoading = true;
        }
    }

    private void OnNavigationCompleted(object? sender, WebViewNavigationCompletedEventArgs e)
    {
        if (_reader is null || sender is not NativeWebView web)
        {
            return;
        }

        _reader.IsLoading = false;

        if (e.Request is { IsAbsoluteUri: true } url)
        {
            _reader.CurrentUrl = url;
        }

        _reader.CanGoBack = web.CanGoBack;
        _reader.CanGoForward = web.CanGoForward;
    }

    private void OnNewWindowRequested(object? sender, WebViewNewWindowRequestedEventArgs e)
    {
        if (sender is NativeWebView web && e.Request is { } url)
        {
            e.Handled = true;
            web.Navigate(url);
        }
    }

    private void OnActionRequested(object? sender, ReaderAction action)
    {
        if (_web is not NativeWebView web)
        {
            return;
        }

        switch (action)
        {
            case ReaderAction.Back:
                web.GoBack();
                break;
            case ReaderAction.Forward:
                web.GoForward();
                break;
            case ReaderAction.Reload:
                web.Refresh();
                break;
        }
    }

    private void OnBackRequested(object? sender, RoutedEventArgs e)
    {
        if (_web is NativeWebView { CanGoBack: true } web)
        {
            web.GoBack();
            e.Handled = true;
        }
    }
}
