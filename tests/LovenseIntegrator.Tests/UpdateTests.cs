using LovenseIntegrator.Desktop.Services;

internal static class UpdateTests
{
    public static async Task RunAsync(Action<bool, string> check)
    {
        var directory = Path.GetFullPath(Path.Combine("artifacts", "update-tests", Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(directory);
        string PathFor(string name) => Path.Combine(directory, name + ".json");
        var backend = new FakeBackend { IsInstalled = false };
        await using (var service = new UpdateService(backend, PathFor("development")))
        {
            await service.CheckAsync();
            check(!service.CanCheck && !service.CanRestart && backend.Checks == 0, "uninstalled build never queries updates");
        }
        backend = new();
        await using (var service = new UpdateService(backend, PathFor("success")))
        {
            check(service.Automatic && service.CanCheck && !service.CanRestart, "installed update defaults");
            await service.CheckAsync();
            check(backend.Checks == 1 && backend.Downloads == 0 && service.Status.Contains("up to date"), "no release does not download");
            backend.Available = "0.2.0";
            await service.CheckAsync();
            check(service.CanRestart && service.Progress == 100 && !service.Busy, "download completes ready for restart");
            check(backend.Restarts == 0, "background download never interrupts an active session");
            service.Automatic = false;
            check(File.Exists(PathFor("success")), "update preference is persisted separately");
            await using var reopened = new UpdateService(backend, PathFor("success"));
            check(!reopened.Automatic && reopened.CanRestart, "preference and pending update survive service restart");
        }
        backend = new() { Error = new IOException("offline") };
        await using (var service = new UpdateService(backend, PathFor("error")))
        {
            await service.CheckAsync();
            check(service.CanCheck && !service.CanRestart && service.Status.Contains("offline"), "offline check is recoverable");
            backend.Error = null; backend.Available = "0.2.0"; backend.DownloadError = new IOException("checksum mismatch");
            await service.CheckAsync();
            check(!service.CanRestart && service.CanCheck && service.Status.Contains("checksum"), "failed integrity check cannot enable restart");
            backend.DownloadError = null;
            await service.CheckAsync();
            check(service.CanRestart, "download can retry after failure");
        }
        backend = new() { Block = new(TaskCreationOptions.RunContinuationsAsynchronously) };
        var blocked = new UpdateService(backend, PathFor("cancel"));
        var first = blocked.CheckAsync();
        var second = blocked.CheckAsync();
        check(ReferenceEquals(first, second) && backend.Checks == 1 && blocked.Busy, "concurrent checks share one operation");
        await blocked.DisposeAsync();
        check(first.IsCompleted && !blocked.CanCheck && !blocked.CanRestart, "shutdown cancels an in-flight update check");
        await blocked.DisposeAsync();
        check(backend.Downloads == 0, "cancelled check cannot download later");
        File.WriteAllText(PathFor("corrupt"), "broken json");
        await using (var service = new UpdateService(new FakeBackend(), PathFor("corrupt")))
            check(service.Automatic && service.PreferenceError.Length > 0, "malformed preferences do not crash startup");
        var badDll = Path.Combine(directory, "wrong.dll"); File.WriteAllText(badDll, "corrupt SDK");
        check(!await BleSdkInstaller.IsValidAsync(badDll, CancellationToken.None), "modified Bluetooth SDK is rejected");
        check(!await BleSdkInstaller.IsValidAsync(badDll + ".missing", CancellationToken.None), "missing Bluetooth SDK is detected");
        if (Environment.GetEnvironmentVariable("LOVENSE_TEST_SDK_DOWNLOAD") == "1")
        {
            var sdkPath = await BleSdkInstaller.EnsureAsync(CancellationToken.None);
            check(await BleSdkInstaller.IsValidAsync(sdkPath, CancellationToken.None), "official SDK download/cache passes pinned SHA-256 without loading native code");
        }
    }
    private sealed class FakeBackend : IUpdateBackend
    {
        public bool IsInstalled { get; set; } = true;
        public string Version => "0.1.0";
        public string? PendingVersion { get; private set; }
        public string? Available { get; set; }
        public Exception? Error { get; set; }
        public Exception? DownloadError { get; set; }
        public TaskCompletionSource? Block { get; set; }
        public int Checks, Downloads, Restarts;
        public async Task<string?> CheckAsync(CancellationToken ct)
        {
            Checks++;
            if (Error is not null) throw Error;
            if (Block is not null) await Block.Task.WaitAsync(ct);
            return Available;
        }
        public Task DownloadAsync(Action<int> progress, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested(); Downloads++;
            if (DownloadError is not null) throw DownloadError;
            PendingVersion = Available;
            return Task.CompletedTask;
        }
        public void Restart() => Restarts++;
    }
}
