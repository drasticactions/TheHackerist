using System.ComponentModel;
using AvaEnyo;
using AvaEnyo.Controls;
using AvaEnyo.Services;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using TheHackerist.Services;
using TheHackerist.ViewModels;

namespace TheHackerist.Views;

public partial class MainView : UserControl
{
    private ShellViewModel? _shell;
    private EnyoUserNotifier? _dashboards;
    private TopLevel? _topLevel;

    public MainView()
    {
        InitializeComponent();

        foreach (var tapEvent in new[] { InputElement.TappedEvent, InputElement.DoubleTappedEvent })
        {
            FeedsPage.AddHandler(tapEvent, (_, e) =>
            {
                if (IsRowTap(e))
                {
                    Sliding.SelectView(StoriesPanel);
                }
            });
            StoriesPage.AddHandler(tapEvent, (_, e) =>
            {
                if (IsRowTap(e) && _shell?.CurrentPage is not null)
                {
                    DetailPanel.IsShowing = true;
                    Sliding.SelectView(DetailPanel);
                }
            });
        }

        DetailPanel.AddHandler(EnyoBack.BackRequestedEvent, OnDetailBackRequested);
        PreferencesPage.Header.BackRequested += (_, e) => e.Handled = ClosePreferences();

        RootPane.SelectionChanged += (_, _) =>
        {
            if (_shell is not null && RootPane.SelectedView != PreferencesPage)
            {
                _shell.IsSettingsOpen = false;
            }
        };
        RootPane.ViewHidden += (_, e) =>
        {
            if (e.View == PreferencesPage)
            {
                PreferencesPage.ShowSettings();
            }
        };
    }

    public DelegatingUserNotifier? Notifier { get; set; }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (TopLevel.GetTopLevel(this) is not { } top || _topLevel is not null)
        {
            return;
        }

        _topLevel = top;
        AppMenuService.Attach(top, BuildAppMenu());
        _dashboards = new EnyoUserNotifier(new InAppNotificationService(top));
        if (Notifier is not null)
        {
            Notifier.Target = _dashboards;
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (_topLevel is not null)
        {
            AppMenuService.Detach(_topLevel);
            _topLevel = null;
        }

        if (Notifier?.Target == _dashboards)
        {
            Notifier!.Target = null;
        }

        _dashboards?.Dispose();
        _dashboards = null;
        base.OnDetachedFromVisualTree(e);
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (_shell is not null)
        {
            _shell.PropertyChanged -= OnShellPropertyChanged;
        }

        _shell = DataContext as ShellViewModel;
        if (_shell is not null)
        {
            _shell.PropertyChanged += OnShellPropertyChanged;
        }
    }

    private void OnShellPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(ShellViewModel.SelectedFeedIndex):
                Sliding.SelectView(StoriesPanel);
                break;
            case nameof(ShellViewModel.CurrentPage) when _shell?.CurrentPage is null:
                Sliding.SelectView(StoriesPanel);
                break;
            case nameof(ShellViewModel.CurrentPage):
                DetailPanel.IsShowing = true;
                Sliding.SelectView(DetailPanel);
                break;
            case nameof(ShellViewModel.IsSettingsOpen) when _shell?.IsSettingsOpen == true:
                RootPane.SelectView(PreferencesPage);
                break;
            case nameof(ShellViewModel.IsSettingsOpen):
                RootPane.SelectView(Sliding);
                break;
        }
    }

    private static bool IsRowTap(RoutedEventArgs e) =>
        e.Source is Visual source
        && source.FindAncestorOfType<Button>(includeSelf: true) is null
        && source.FindAncestorOfType<ListBoxItem>(includeSelf: true) is not null;

    private bool ClosePreferences()
    {
        if (_shell is { IsSettingsOpen: true })
        {
            _shell.IsSettingsOpen = false;
            return true;
        }

        return RootPane.Back();
    }

    private AvaEnyo.Controls.AppMenu BuildAppMenu()
    {
        var refresh = new AppMenuItem { Header = "Refresh" };
        refresh.Click += (_, _) => _shell?.RefreshCommand.Execute(null);
        var preferences = new AppMenuItem { Header = "Preferences" };
        preferences.Click += (_, _) => _shell?.OpenSettingsCommand.Execute(null);

        var menu = new AvaEnyo.Controls.AppMenu();
        menu.Items.Add(refresh);
        menu.Items.Add(new EditMenu());
        menu.Items.Add(preferences);
        menu.Items.Add(new HelpMenu { Target = SettingsViewModel.ProjectUri });
        return menu;
    }

    private void OnDetailBackRequested(object? sender, RoutedEventArgs e)
    {
        if (_shell is { Pages.Count: > 1 })
        {
            _shell.GoBackCommand.Execute(null);
            e.Handled = true;
        }
    }
}
