using System.Text;
using System.Text.Json;
using EvilEndUser.App.Services;
using EvilEndUser.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace EvilEndUser.App.Pages;

/// <summary>Full activity log with JSON/CSV export for correlation against Defender alerts.</summary>
public sealed partial class ActivityLogPage : Page
{
    /// <summary>The shared orchestration service (bound from XAML).</summary>
    public SimulationService Sim { get; }

    /// <summary>Initializes the log page.</summary>
    public ActivityLogPage()
    {
        Sim = EvilEndUser_App.App.Services.GetRequiredService<SimulationService>();
        InitializeComponent();
    }

    private void Clear_Click(object sender, RoutedEventArgs e) => Sim.ClearEvents();

    private async void Export_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dir = Sim.Config.WorkingDirectory;
            Directory.CreateDirectory(dir);
            var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");

            var history = Sim.EventBus.History;
            var jsonPath = Path.Combine(dir, $"evilenduser-log-{stamp}.json");
            var csvPath = Path.Combine(dir, $"evilenduser-log-{stamp}.csv");

            var json = JsonSerializer.Serialize(history.Select(e => new
            {
                e.Timestamp,
                Severity = e.Severity.ToString(),
                e.Source,
                e.TechniqueId,
                Tactic = e.Tactic?.ToString(),
                RiskLevel = e.RiskLevel?.ToString(),
                e.ScenarioId,
                e.Message,
                e.Data,
            }), new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(jsonPath, json);

            var csv = new StringBuilder();
            csv.AppendLine("Timestamp,Severity,Source,TechniqueId,Tactic,RiskLevel,ScenarioId,Message");
            foreach (var ev in history)
            {
                csv.AppendLine(string.Join(",",
                    Csv(ev.Timestamp.ToString("o")),
                    Csv(ev.Severity.ToString()),
                    Csv(ev.Source),
                    Csv(ev.TechniqueId),
                    Csv(ev.Tactic?.ToString()),
                    Csv(ev.RiskLevel?.ToString()),
                    Csv(ev.ScenarioId),
                    Csv(ev.Message)));
            }
            await File.WriteAllTextAsync(csvPath, csv.ToString());

            ExportBar.Title = "Exported";
            ExportBar.Message = $"Wrote {history.Count} events to:\n{jsonPath}\n{csvPath}";
            ExportBar.Severity = InfoBarSeverity.Success;
            ExportBar.IsOpen = true;
        }
        catch (Exception ex)
        {
            ExportBar.Title = "Export failed";
            ExportBar.Message = ex.Message;
            ExportBar.Severity = InfoBarSeverity.Error;
            ExportBar.IsOpen = true;
        }
    }

    private static string Csv(string? value)
    {
        value ??= string.Empty;
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
        {
            value = "\"" + value.Replace("\"", "\"\"") + "\"";
        }
        return value;
    }
}
