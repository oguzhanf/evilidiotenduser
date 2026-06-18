using EvilEndUser.App.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;

namespace EvilEndUser_App;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{
    private Window? _window;

    /// <summary>The application service provider (simple DI container).</summary>
    public static IServiceProvider Services { get; private set; } = null!;

    /// <summary>The main application window.</summary>
    public static Window? MainWindow { get; private set; }

    /// <summary>
    /// Initializes the singleton application object. This is the logical equivalent of main().
    /// </summary>
    public App()
    {
        InitializeComponent();
        Services = ConfigureServices();
    }

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton<SimulationService>();
        return services.BuildServiceProvider();
    }

    /// <summary>Invoked when the application is launched.</summary>
    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        _window = new MainWindow();
        MainWindow = _window;
        _window.Activate();
    }
}
