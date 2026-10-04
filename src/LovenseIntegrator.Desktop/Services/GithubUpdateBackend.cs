using System.Reflection;
using Velopack;
using Velopack.Sources;

namespace LovenseIntegrator.Desktop.Services;

internal sealed class GithubUpdateBackend : IUpdateBackend
{
    private readonly UpdateManager manager = new(new GithubSource(UpdateService.RepositoryUrl, null, false));
    private UpdateInfo? available;
    public bool IsInstalled => manager.IsInstalled;
    public string Version => manager.CurrentVersion?.ToString()
        ?? Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0] ?? "unknown";
    public string? PendingVersion => manager.UpdatePendingRestart?.Version.ToString();
    public async Task<string?> CheckAsync(CancellationToken ct)
    {
        // Velopack's check has no cancellation parameter; bound the wait and ignore a late result.
        var update = await manager.CheckForUpdatesAsync().WaitAsync(ct);
        ct.ThrowIfCancellationRequested();
        available = update;
        return update?.TargetFullRelease.Version.ToString();
    }
    public Task DownloadAsync(Action<int> progress, CancellationToken ct) => manager.DownloadUpdatesAsync(available!, progress, cancelToken: ct);
    public void Restart() => manager.ApplyUpdatesAndRestart(manager.UpdatePendingRestart, restartArgs: []);
}
