using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace LovenseIntegrator.Desktop.Services;

public interface IUpdateBackend
{
    bool IsInstalled { get; }
    string Version { get; }
    string? PendingVersion { get; }
    Task<string?> CheckAsync(CancellationToken ct);
    Task DownloadAsync(Action<int> progress, CancellationToken ct);
    void Restart();
}

// Created and awaited on the WPF Dispatcher; backend progress is marshalled by Progress<T>.
public sealed class UpdateService : INotifyPropertyChanged, IAsyncDisposable
{
    public const string RepositoryUrl = "https://github.com/timurchak/LovenseIntegrator";
    private readonly IUpdateBackend backend;
    private readonly string preferencesPath;
    private readonly CancellationTokenSource lifetime = new();
    private Task? loop, operation;
    private bool automatic = true, busy, disposed;
    private string status = "", preferenceError = "";
    private int progress;
    public event PropertyChangedEventHandler? PropertyChanged;
    public string Version => backend.Version;
    public string Status => status;
    public string PreferenceError => preferenceError;
    public int Progress => progress;
    public bool Busy => busy;
    public bool CanCheck => backend.IsInstalled && !busy && !disposed;
    public bool CanRestart => backend.PendingVersion is not null && !busy && !disposed;
    public string TabTitle => backend.PendingVersion is not null ? "  Updates • ready  " : "  Updates  ";
    public bool Automatic
    {
        get => automatic;
        set
        {
            if (automatic == value) return;
            automatic = value;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(preferencesPath)!);
                File.WriteAllText(preferencesPath + ".tmp", JsonSerializer.Serialize(new Preferences(value)));
                File.Move(preferencesPath + ".tmp", preferencesPath, true);
                preferenceError = "";
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            { preferenceError = "Could not save update preferences: " + ex.Message; }
            Changed(); Changed(nameof(PreferenceError));
            if (value) _ = CheckAsync();
        }
    }
    public UpdateService(IUpdateBackend backend, string preferencesPath)
    {
        this.backend = backend; this.preferencesPath = preferencesPath;
        try
        {
            if (File.Exists(preferencesPath)) automatic = JsonSerializer.Deserialize<Preferences>(File.ReadAllText(preferencesPath))?.Automatic ?? true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        { preferenceError = "Update preferences could not be read; automatic checks are enabled."; }
        SetStatus(!backend.IsInstalled ? "Development build. Install the GitHub release to enable updates."
            : backend.PendingVersion is { } pending ? $"Version {pending} is ready. Restart to update."
            : "Updates come from GitHub Releases (stable Windows x64).");
    }
    public void Start() => loop ??= RunAsync();
    private async Task RunAsync()
    {
        try
        {
            // Let startup, input controls and device discovery settle first.
            await Task.Delay(TimeSpan.FromSeconds(10), lifetime.Token);
            while (!lifetime.IsCancellationRequested)
            {
                if (Automatic) await CheckAsync();
                await Task.Delay(TimeSpan.FromHours(6), lifetime.Token);
            }
        }
        catch (OperationCanceledException) when (lifetime.IsCancellationRequested) { }
    }
    public Task CheckAsync()
    {
        if (!CanCheck) return operation ?? Task.CompletedTask;
        return operation = CheckCoreAsync();
    }
    private async Task CheckCoreAsync()
    {
        busy = true; progress = 0; Refresh();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
        timeout.CancelAfter(TimeSpan.FromMinutes(10));
        try
        {
            SetStatus("Checking GitHub Releases…");
            var version = await backend.CheckAsync(timeout.Token);
            if (version is null)
            {
                SetStatus(backend.PendingVersion is { } pending ? $"Version {pending} is ready. Restart to update." : "You are up to date.");
                return;
            }
            SetStatus($"Downloading version {version}…");
            var reporter = new Progress<int>(value => { if (!disposed && busy) { progress = Math.Clamp(value, 0, 100); Changed(nameof(Progress)); } });
            await backend.DownloadAsync(((IProgress<int>)reporter).Report, timeout.Token);
            progress = 100;
            SetStatus($"Version {version} is ready. It will be installed the next time you start the app.");
        }
        catch (OperationCanceledException)
        { if (!disposed) SetStatus("Update timed out. Check your connection and try again."); }
        catch (Exception ex)
        { SetStatus("Update unavailable. You can keep using the app and retry. " + ex.Message); }
        finally { busy = false; Refresh(); }
    }
    // The window must await transport shutdown before calling this.
    public void RestartAfterShutdown() => backend.Restart();
    public async ValueTask DisposeAsync()
    {
        if (disposed) return;
        disposed = true; lifetime.Cancel(); Refresh();
        if (operation is not null) await operation;
        if (loop is not null) await loop;
        lifetime.Dispose();
    }
    private void SetStatus(string value) { status = value; Changed(nameof(Status)); }
    private void Refresh() { Changed(nameof(Busy)); Changed(nameof(CanCheck)); Changed(nameof(CanRestart)); Changed(nameof(Progress)); Changed(nameof(TabTitle)); }
    private void Changed([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new(name));
    private sealed record Preferences(bool Automatic);
}
