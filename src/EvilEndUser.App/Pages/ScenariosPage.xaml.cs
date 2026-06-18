using System.Collections.ObjectModel;
using EvilEndUser.App.Models;
using EvilEndUser.App.Services;
using EvilEndUser.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace EvilEndUser.App.Pages;

/// <summary>Lists every scenario grouped by MITRE tactic, with a Run action per scenario.</summary>
public sealed partial class ScenariosPage : Page
{
    private readonly SimulationService _sim;

    /// <summary>A scenario group rendered as a header + cards.</summary>
    public sealed class ScenarioGroup : ObservableCollection<ScenarioCard>
    {
        /// <summary>The group header (tactic name).</summary>
        public string Key { get; }

        /// <summary>Creates a group.</summary>
        public ScenarioGroup(string key, IEnumerable<ScenarioCard> items) : base(items) => Key = key;
    }

    /// <summary>Initializes the scenarios page.</summary>
    public ScenariosPage()
    {
        _sim = EvilEndUser_App.App.Services.GetRequiredService<SimulationService>();
        InitializeComponent();
        BuildGroups();
    }

    private void BuildGroups()
    {
        var groups = _sim.Scenarios.All
            .GroupBy(s => s.Tactic)
            .OrderBy(g => (int)g.Key)
            .Select(g =>
            {
                var info = MitreTactics.Get(g.Key);
                return new ScenarioGroup(
                    $"{info.Name} ({info.Id})",
                    g.Select(s => new ScenarioCard(s)));
            })
            .ToList();

        GroupedScenarios.Source = groups;
        ScenarioList.ItemsSource = GroupedScenarios.View;
    }

    private async void RunScenario_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string id })
        {
            await _sim.RunScenarioAsync(id);
        }
    }
}
