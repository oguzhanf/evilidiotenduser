using EvilEndUser.App.Services;
using EvilEndUser.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace EvilEndUser.App.Pages;

/// <summary>Settings page for identity, target apps, Graph sign-in, and the LLM brain.</summary>
public sealed partial class ConfigurationPage : Page
{
    private readonly SimulationService _sim;
    private bool _initializing = true;

    /// <summary>Initializes the configuration page.</summary>
    public ConfigurationPage()
    {
        _sim = EvilEndUser_App.App.Services.GetRequiredService<SimulationService>();
        InitializeComponent();
        PopulateCombos();
        LoadValues();
        _initializing = false;

        _sim.AutoConfigured += OnAutoConfigured;
        Unloaded += (_, _) => _sim.AutoConfigured -= OnAutoConfigured;

        UpdateAutoDetectBar();
        TriggerAutoDetect(force: false);
    }

    private void OnAutoConfigured()
    {
        // Reflect newly discovered identity / Foundry values without duplicating combo items.
        _initializing = true;
        LoadValues();
        _initializing = false;
        UpdateAutoDetectBar();
    }

    private void UpdateAutoDetectBar()
    {
        AutoDetectBar.Message = _sim.AutoConfigStatus;
        AutoDetectBar.Severity = _sim.IsAutoConfigured ? InfoBarSeverity.Success : InfoBarSeverity.Informational;
    }

    private void TriggerAutoDetect(bool force)
    {
        try
        {
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(EvilEndUser_App.App.MainWindow);
            _ = _sim.EnsureAutoConfiguredAsync(hwnd, force);
        }
        catch
        {
            // status/log surface any failure
        }
    }

    private async void Redetect_Click(object sender, RoutedEventArgs e)
    {
        AutoDetectBar.Severity = InfoBarSeverity.Informational;
        AutoDetectBar.Message = "Detecting…";
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(EvilEndUser_App.App.MainWindow);
        await _sim.EnsureAutoConfiguredAsync(hwnd, force: true);
        UpdateAutoDetectBar();
    }

    private void PopulateCombos()
    {
        foreach (var b in Enum.GetValues<BrowserKind>())
        {
            BrowserCombo.Items.Add(b.ToString());
        }
        foreach (var p in Enum.GetValues<LlmProvider>())
        {
            LlmProviderCombo.Items.Add(p.ToString());
        }
    }

    private void LoadValues()
    {
        var c = _sim.Config;

        SelfMailboxBox.Text = c.SelfMailbox ?? string.Empty;
        TenantBox.Text = c.Llm.TenantId ?? string.Empty;
        RecipientsBox.Text = string.Join(", ", c.ExternalRecipients);
        WorkingDirBox.Text = c.WorkingDirectory;

        BrowserCombo.SelectedItem = c.Browser.ToString();
        HeadlessToggle.IsOn = c.HeadlessBrowser;
        EmailToggle.IsOn = c.EnableEmail;
        TeamsToggle.IsOn = c.EnableTeams;
        ProcessToggle.IsOn = c.EnableProcessExecution;
        FileToggle.IsOn = c.EnableFileSystem;

        LlmProviderCombo.SelectedItem = c.Llm.Provider.ToString();
        LlmEndpointBox.Text = c.Llm.Endpoint ?? string.Empty;
        LlmDeploymentBox.Text = c.Llm.DeploymentName;
        TempSlider.Value = c.Llm.BaseTemperature;
    }

    private void SelfMailbox_Changed(object sender, TextChangedEventArgs e)
    {
        if (_initializing) return;
        _sim.Config.SelfMailbox = string.IsNullOrWhiteSpace(SelfMailboxBox.Text) ? null : SelfMailboxBox.Text.Trim();
    }

    private void Tenant_Changed(object sender, TextChangedEventArgs e)
    {
        if (_initializing) return;
        _sim.Config.Llm.TenantId = string.IsNullOrWhiteSpace(TenantBox.Text) ? null : TenantBox.Text.Trim();
    }

    private void Recipients_Changed(object sender, TextChangedEventArgs e)
    {
        if (_initializing) return;
        _sim.Config.ExternalRecipients = RecipientsBox.Text
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
    }

    private void WorkingDir_Changed(object sender, TextChangedEventArgs e)
    {
        if (_initializing || string.IsNullOrWhiteSpace(WorkingDirBox.Text)) return;
        _sim.Config.WorkingDirectory = WorkingDirBox.Text.Trim();
    }

    private void Browser_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_initializing || BrowserCombo.SelectedItem is not string s) return;
        if (Enum.TryParse<BrowserKind>(s, out var b)) _sim.Config.Browser = b;
    }

    private void Headless_Toggled(object sender, RoutedEventArgs e)
    {
        if (_initializing) return;
        _sim.Config.HeadlessBrowser = HeadlessToggle.IsOn;
    }

    private void Email_Toggled(object sender, RoutedEventArgs e)
    {
        if (_initializing) return;
        _sim.Config.EnableEmail = EmailToggle.IsOn;
    }

    private void Teams_Toggled(object sender, RoutedEventArgs e)
    {
        if (_initializing) return;
        _sim.Config.EnableTeams = TeamsToggle.IsOn;
    }

    private void Process_Toggled(object sender, RoutedEventArgs e)
    {
        if (_initializing) return;
        _sim.Config.EnableProcessExecution = ProcessToggle.IsOn;
    }

    private void File_Toggled(object sender, RoutedEventArgs e)
    {
        if (_initializing) return;
        _sim.Config.EnableFileSystem = FileToggle.IsOn;
    }

    private void LlmProvider_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_initializing || LlmProviderCombo.SelectedItem is not string s) return;
        if (Enum.TryParse<LlmProvider>(s, out var p)) _sim.Config.Llm.Provider = p;
    }

    private void LlmEndpoint_Changed(object sender, TextChangedEventArgs e)
    {
        if (_initializing) return;
        _sim.Config.Llm.Endpoint = string.IsNullOrWhiteSpace(LlmEndpointBox.Text) ? null : LlmEndpointBox.Text.Trim();
    }

    private void LlmDeployment_Changed(object sender, TextChangedEventArgs e)
    {
        if (_initializing || string.IsNullOrWhiteSpace(LlmDeploymentBox.Text)) return;
        _sim.Config.Llm.DeploymentName = LlmDeploymentBox.Text.Trim();
    }

    private void LlmKey_Changed(object sender, RoutedEventArgs e)
    {
        if (_initializing) return;
        _sim.Config.Llm.ApiKey = string.IsNullOrWhiteSpace(LlmKeyBox.Password) ? null : LlmKeyBox.Password;
    }

    private void Temp_Changed(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (_initializing) return;
        _sim.Config.Llm.BaseTemperature = TempSlider.Value;
    }

    private void SignInGraph_Click(object sender, RoutedEventArgs e)
    {
        GraphBar.Title = "Device sign-in";
        GraphBar.Message = "Graph sign-in is performed automatically (device-code flow) the first time an " +
                           "email or Teams action runs. Watch the Activity Log for the code and aka.ms/devicelogin URL.";
        GraphBar.Severity = InfoBarSeverity.Informational;
        GraphBar.IsOpen = true;
    }
}
