using EvilEndUser.Core.Models;

namespace EvilEndUser.Core.Safety;

/// <summary>
/// Thrown when an action is blocked by the <see cref="SafetyGuard"/>.
/// </summary>
public sealed class SafetyViolationException : Exception
{
    /// <summary>The action that was rejected.</summary>
    public SimAction Action { get; }

    /// <summary>Creates a new violation for the given action.</summary>
    public SafetyViolationException(SimAction action, string reason)
        : base($"Safety guard blocked {action.Type} action: {reason}")
        => Action = action;
}

/// <summary>
/// Central choke-point that enforces the simulator's safety contract before any action is executed.
/// It guarantees two invariants:
/// <list type="number">
///   <item>No action above the configured <see cref="SimulationConfig.MaxRiskLevel"/> runs.</item>
///   <item>In <see cref="SimulationConfig.SafeMode"/>, network/file/process targets must come from
///         the vetted <see cref="TestPayloads"/> catalogue (or be obviously inert).</item>
/// </list>
/// This is what makes the tool safe to publish and run inside a production-monitored tenant.
/// </summary>
public sealed class SafetyGuard
{
    private readonly SimulationConfig _config;

    /// <summary>Domains considered safe to contact in SafeMode (test infrastructure only).</summary>
    private static readonly string[] AllowedHosts =
    {
        "secure.eicar.org",
        "demo.smartscreen.msft.net",
        "smartscreentestratings2.net",
        "demo.wd.microsoft.com",
        "www.amtso.org",
        "amtso.org",
        "127.0.0.1",
        "localhost",
    };

    /// <summary>Creates a guard bound to the supplied configuration.</summary>
    public SafetyGuard(SimulationConfig config) => _config = config;

    /// <summary>
    /// Validates an action. Returns the (possibly unchanged) action when allowed; throws
    /// <see cref="SafetyViolationException"/> when blocked.
    /// </summary>
    public SimAction Validate(SimAction action)
    {
        if (!_config.Permits(action.RiskLevel))
        {
            throw new SafetyViolationException(action,
                $"risk {action.RiskLevel} exceeds ceiling {_config.MaxRiskLevel}.");
        }

        if (!_config.SafeMode)
        {
            return action; // Operator has explicitly disabled the allow-list.
        }

        switch (action.Type)
        {
            case SimActionType.BrowseUrl:
            case SimActionType.DownloadFile:
            case SimActionType.ClickEmailLink:
            case SimActionType.UploadToCloud:
                EnsureUrlAllowed(action);
                break;

            case SimActionType.RunProcess:
                EnsureCommandAllowed(action);
                break;
        }

        return action;
    }

    /// <summary>True when the action would be permitted, without throwing.</summary>
    public bool IsAllowed(SimAction action, out string? reason)
    {
        try
        {
            Validate(action);
            reason = null;
            return true;
        }
        catch (SafetyViolationException ex)
        {
            reason = ex.Message;
            return false;
        }
    }

    private static void EnsureUrlAllowed(SimAction action)
    {
        if (string.IsNullOrWhiteSpace(action.Target))
        {
            throw new SafetyViolationException(action, "no target URL supplied.");
        }

        if (!Uri.TryCreate(action.Target, UriKind.Absolute, out var uri))
        {
            throw new SafetyViolationException(action, $"'{action.Target}' is not an absolute URL.");
        }

        var host = uri.Host;
        var allowed = AllowedHosts.Any(h =>
            host.Equals(h, StringComparison.OrdinalIgnoreCase) ||
            host.EndsWith("." + h, StringComparison.OrdinalIgnoreCase));

        if (!allowed)
        {
            throw new SafetyViolationException(action,
                $"host '{host}' is not in the SafeMode allow-list. Disable SafeMode to target arbitrary URLs.");
        }
    }

    private static void EnsureCommandAllowed(SimAction action)
    {
        var cmd = action.Target ?? string.Empty;

        // In SafeMode only the vetted benign commands may run verbatim.
        var allowed = TestPayloads.AllSafeCommands.Any(c =>
            string.Equals(c, cmd, StringComparison.OrdinalIgnoreCase));

        if (!allowed)
        {
            throw new SafetyViolationException(action,
                "command is not in the SafeMode allow-list of benign test commands.");
        }
    }
}
