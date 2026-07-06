using Azure.Core;
using EvilEndUser.Automation.Drivers;
using EvilEndUser.Automation.Graph;
using EvilEndUser.Core.Abstractions;
using EvilEndUser.Core.Models;
using Microsoft.Extensions.Logging;

namespace EvilEndUser.Automation;

/// <summary>
/// Assembles the set of <see cref="IAutomationDriver"/>s appropriate for a given configuration.
/// Drivers that require Graph share a single <see cref="GraphClientProvider"/> so the operator only
/// signs in once.
/// </summary>
public sealed class DriverFactory
{
    private readonly ILoggerFactory? _loggerFactory;

    /// <summary>Creates the factory.</summary>
    public DriverFactory(ILoggerFactory? loggerFactory = null) => _loggerFactory = loggerFactory;

    /// <summary>
    /// The shared Graph provider created by the most recent <see cref="Build"/> call, so the UI can
    /// wire up its device-code callback. Null until email/Teams are enabled.
    /// </summary>
    public GraphClientProvider? GraphProvider { get; private set; }

    /// <summary>
    /// Builds the driver collection honouring the enable-flags in <paramref name="config"/>.
    /// When <paramref name="brokerCredential"/> is supplied (the signed-in Entra account), Graph uses
    /// it for silent SSO instead of the device-code flow.
    /// </summary>
    public IReadOnlyList<IAutomationDriver> Build(SimulationConfig config, TokenCredential? brokerCredential = null)
    {
        var drivers = new List<IAutomationDriver>
        {
            new BrowserDriver(config, _loggerFactory?.CreateLogger<BrowserDriver>()),
            new DesktopAppDriver(_loggerFactory?.CreateLogger<DesktopAppDriver>()),
        };

        if (config.EnableFileSystem)
        {
            drivers.Add(new FileSystemDriver(config, _loggerFactory?.CreateLogger<FileSystemDriver>()));
        }

        if (config.EnableProcessExecution)
        {
            drivers.Add(new ProcessDriver(_loggerFactory?.CreateLogger<ProcessDriver>()));
        }

        if (config.EnableEmail || config.EnableTeams)
        {
            GraphProvider = new GraphClientProvider(
                config.Llm.TenantId,
                useDeviceCode: brokerCredential is null,
                _loggerFactory?.CreateLogger<GraphClientProvider>(),
                credential: brokerCredential);

            if (config.EnableEmail)
            {
                drivers.Add(new GraphEmailDriver(GraphProvider, _loggerFactory?.CreateLogger<GraphEmailDriver>()));
            }

            if (config.EnableTeams)
            {
                drivers.Add(new GraphTeamsDriver(GraphProvider, _loggerFactory?.CreateLogger<GraphTeamsDriver>()));
            }
        }

        return drivers;
    }
}
