using System.IO;
using System.Text.Json;
using Velopack;

namespace LovenseIntegrator.Desktop.Services;

internal static class UpdateHarness
{
    // Explicit diagnostic mode. Never creates a VM, opens devices or reads the user's profile.
    public static async Task RunAsync(string[] args)
    {
        var index = Array.IndexOf(args, "--update-harness");
        var source = args[index + 1];
        var report = Path.GetFullPath(args[index + 2]);
        Directory.CreateDirectory(Path.GetDirectoryName(report)!);
        try
        {
            var manager = source == "github"
                ? new UpdateManager(new Velopack.Sources.GithubSource(UpdateService.RepositoryUrl, null, false))
                : new UpdateManager(Path.GetFullPath(source));
            if (!manager.IsInstalled) throw new InvalidOperationException("Install the test package before running this harness.");
            var available = await manager.CheckForUpdatesAsync();
            if (args.Contains("--download") && available is not null) await manager.DownloadUpdatesAsync(available);
            if (args.Contains("--apply"))
            {
                if (manager.UpdatePendingRestart is null) throw new InvalidOperationException("No verified update is ready.");
                manager.WaitExitThenApplyUpdates(manager.UpdatePendingRestart, silent: true, restart: false);
            }
            File.WriteAllText(report, JsonSerializer.Serialize(new { Result = "PASS", Version = manager.CurrentVersion!.ToString(),
                Available = available?.TargetFullRelease.Version.ToString(), Pending = manager.UpdatePendingRestart?.Version.ToString() }));
        }
        catch (Exception ex)
        {
            File.WriteAllText(report, JsonSerializer.Serialize(new { Result = "FAIL", Error = ex.ToString() }));
            throw;
        }
    }
}
