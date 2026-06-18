using EvilEndUser.App.Services;
using EvilEndUser.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using EvilEndUser_App;

namespace EvilEndUser.App.Pages;

/// <summary>The main control-panel page: run profile, quick actions, and a live activity feed.</summary>
public sealed partial class DashboardPage : Page
{
    /// <summary>The shared orchestration service (bound from XAML).</summary>
    public SimulationService Sim { get; }

    private bool _initializing = true;

    /// <summary>Initializes the dashboard.</summary>
    public DashboardPage()
    {
        Sim = EvilEndUser_App.App.Services.GetRequiredService<SimulationService>();
        InitializeComponent();
        PopulateControls();
        _initializing = false;
    }

    private void PopulateControls()
    {
        foreach (var preset in PersonaProfile.Presets)
        {
            PersonaCombo.Items.Add(preset.Name);
        }
        PersonaCombo.SelectedIndex = 0;

        foreach (var level in Enum.GetValues<RiskLevel>())
        {
            RiskCombo.Items.Add(level.ToString());
        }
        RiskCombo.SelectedItem = Sim.Config.MaxRiskLevel.ToString();

        SafeModeToggle.IsOn = Sim.Config.SafeMode;
        DryRunToggle.IsOn = Sim.Config.DryRun;
    }

    private void PersonaCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_initializing || PersonaCombo.SelectedIndex < 0) return;
        var preset = PersonaProfile.Presets[PersonaCombo.SelectedIndex];
        // Copy preset values into the live persona.
        Sim.Config.Persona.Name = preset.Name;
        Sim.Config.Persona.Role = preset.Role;
        Sim.Config.Persona.Gullibility = preset.Gullibility;
        Sim.Config.Persona.Erraticness = preset.Erraticness;
        Sim.Config.Persona.Backstory = preset.Backstory;
    }

    private void RiskCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_initializing || RiskCombo.SelectedItem is not string s) return;
        if (Enum.TryParse<RiskLevel>(s, out var level))
        {
            Sim.Config.MaxRiskLevel = level;
        }
    }

    private void SafeModeToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_initializing) return;
        Sim.Config.SafeMode = SafeModeToggle.IsOn;
    }

    private void DryRunToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_initializing) return;
        Sim.Config.DryRun = DryRunToggle.IsOn;
    }

    private async void KillChain_Click(object sender, RoutedEventArgs e)
        => await Sim.RunKillChainAsync();

    private async void Wander_Click(object sender, RoutedEventArgs e)
    {
        if (Sim.Config.Llm.Provider == LlmProvider.None || !Sim.Config.Llm.IsConfigured)
        {
            var dialog = new ContentDialog
            {
                Title = "LLM not configured",
                Content = "Configure an Azure OpenAI / OpenAI deployment on the Configuration page to use LLM free roam.",
                CloseButtonText = "OK",
                XamlRoot = XamlRoot,
            };
            await dialog.ShowAsync();
            return;
        }

        await Sim.RunWanderAsync(steps: 12);
    }

    private void Stop_Click(object sender, RoutedEventArgs e) => Sim.Stop();

    private void Scenarios_Click(object sender, RoutedEventArgs e) => NavigateShell("scenarios");

    private void Scenarios_OpenLog(object sender, RoutedEventArgs e) => NavigateShell("activity");

    private static void NavigateShell(string tag)
    {
        if (EvilEndUser_App.App.MainWindow is MainWindow mw)
        {
            mw.NavigateTo(tag);
        }
    }
}
