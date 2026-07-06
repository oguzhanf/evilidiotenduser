using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using EvilEndUser.App.Pages;
using EvilEndUser.App.Services;
using Microsoft.Extensions.DependencyInjection;

namespace EvilEndUser_App;

/// <summary>
/// The main application shell hosting the navigation view and content frame.
/// </summary>
public sealed partial class MainWindow : Window
{
    /// <summary>Initializes the shell window.</summary>
    public MainWindow()
    {
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
        AppWindow.SetIcon("Assets/AppIcon.ico");

        NavFrame.Navigate(typeof(DashboardPage));

        // Kick off zero-config Entra + Foundry auto-detection using this window as the broker parent.
        StartAutoConfig();
    }

    private void StartAutoConfig()
    {
        try
        {
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var sim = EvilEndUser_App.App.Services.GetRequiredService<SimulationService>();
            _ = sim.EnsureAutoConfiguredAsync(hwnd);
        }
        catch
        {
            // Best-effort; auto-config failures are surfaced via the service status/log.
        }
    }

    private void TitleBar_PaneToggleRequested(TitleBar sender, object args)
        => NavView.IsPaneOpen = !NavView.IsPaneOpen;

    private void TitleBar_BackRequested(TitleBar sender, object args)
    {
        if (NavFrame.CanGoBack)
        {
            NavFrame.GoBack();
        }
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is not NavigationViewItem item)
        {
            return;
        }

        NavigateTo(item.Tag?.ToString() ?? "dashboard");
    }

    /// <summary>Navigates the content frame to the page identified by <paramref name="tag"/>.</summary>
    public void NavigateTo(string tag)
    {
        var pageType = tag switch
        {
            "dashboard" => typeof(DashboardPage),
            "scenarios" => typeof(ScenariosPage),
            "activity" => typeof(ActivityLogPage),
            "configuration" => typeof(ConfigurationPage),
            "about" => typeof(AboutPage),
            _ => typeof(DashboardPage),
        };

        if (NavFrame.CurrentSourcePageType != pageType)
        {
            NavFrame.Navigate(pageType);
        }

        // Keep the NavigationView selection in sync when navigation is programmatic.
        foreach (var menuItem in NavView.MenuItems.OfType<NavigationViewItem>())
        {
            if ((menuItem.Tag?.ToString()) == tag)
            {
                NavView.SelectedItem = menuItem;
                break;
            }
        }
    }
}
