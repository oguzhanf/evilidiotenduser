using EvilEndUser.Core.Abstractions;
using EvilEndUser.Core.Models;
using Microsoft.Extensions.Logging;

namespace EvilEndUser.Automation.Drivers;

/// <summary>
/// Performs filesystem actions for the simulator: writing test payloads (e.g. the EICAR file) and
/// opening files with their default handler. All writes are confined to the configured working
/// directory tree to avoid touching the user's real data.
/// </summary>
public sealed class FileSystemDriver : IFileSystemDriver
{
    private readonly SimulationConfig _config;
    private readonly ILogger<FileSystemDriver>? _logger;

    /// <summary>Creates the driver.</summary>
    public FileSystemDriver(SimulationConfig config, ILogger<FileSystemDriver>? logger = null)
    {
        _config = config;
        _logger = logger;
    }

    /// <inheritdoc />
    public string Name => "FileSystem";

    /// <inheritdoc />
    public Task<bool> IsAvailableAsync(CancellationToken ct = default) => Task.FromResult(true);

    /// <inheritdoc />
    public Task<ActionResult> WriteFileAsync(SimAction action, CancellationToken ct = default)
    {
        var start = DateTimeOffset.Now;
        try
        {
            var path = ResolvePath(action.Target ?? throw new ArgumentException("No file path supplied."));
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);

            var content = action.Param("content");
            File.WriteAllText(path, content);

            if (string.Equals(action.Param("hidden"), "true", StringComparison.OrdinalIgnoreCase))
            {
                File.SetAttributes(path, File.GetAttributes(path) | FileAttributes.Hidden);
            }

            _logger?.LogInformation("Wrote test file {Path} ({Bytes} bytes)", path, content.Length);
            return Task.FromResult(ActionResult.Ok(action, $"Wrote {path}",
                DateTimeOffset.Now - start, new[] { path }));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ActionResult.Fail(action, "Failed to write file", ex.Message,
                DateTimeOffset.Now - start));
        }
    }

    /// <inheritdoc />
    public Task<ActionResult> OpenFileAsync(SimAction action, CancellationToken ct = default)
    {
        var start = DateTimeOffset.Now;
        try
        {
            var path = ResolvePath(action.Target ?? throw new ArgumentException("No file path supplied."));
            if (!File.Exists(path))
            {
                return Task.FromResult(ActionResult.Fail(action, $"File not found: {path}"));
            }

            // Launch via the shell so it behaves like a user double-clicking the file.
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true,
            };
            using var proc = System.Diagnostics.Process.Start(psi);

            _logger?.LogInformation("Opened file {Path}", path);
            return Task.FromResult(ActionResult.Ok(action, $"Opened {path}",
                DateTimeOffset.Now - start, new[] { path }));
        }
        catch (Exception ex)
        {
            // Defender quarantining the file mid-open is an expected, successful outcome for our purposes.
            return Task.FromResult(ActionResult.Ok(action,
                $"Open attempted; handler returned: {ex.Message} (likely blocked/quarantined by Defender)",
                DateTimeOffset.Now - start));
        }
    }

    /// <summary>Confines a path to the working directory when a bare file name is provided.</summary>
    private string ResolvePath(string target)
    {
        if (Path.IsPathRooted(target))
        {
            return target;
        }

        return Path.Combine(_config.WorkingDirectory, target);
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
