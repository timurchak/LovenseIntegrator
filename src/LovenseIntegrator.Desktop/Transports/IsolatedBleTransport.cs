using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using LovenseIntegrator.Core;
using LovenseIntegrator.Desktop.Services;

namespace LovenseIntegrator.Desktop.Transports;

internal sealed record BleRequest(int Id, string Operation, ToyCommand? Command = null);
internal sealed record BleReply(int Id, Toy[]? Toys = null, string? Error = null, string? Message = null);

// The vendor DLL uses native FailFast on some shutdown paths. It must never be loaded in the UI process.
public sealed class IsolatedBleTransport : IToyTransport
{
    private readonly SemaphoreSlim requests = new(1, 1);
    private readonly ConcurrentDictionary<int, TaskCompletionSource<BleReply>> pending = new();
    private readonly bool fake;
    private NamedPipeServerStream? pipe;
    private StreamWriter? writer;
    private Process? worker;
    private int nextId, failed;
    private bool disposing, disposed;
    public event Action<string>? Message;
    public event Action<string>? Faulted;
    public string Name => "Bluetooth · Lovense BLE";
    public bool CanReuse => !disposed && !disposing && Volatile.Read(ref failed) == 0;
    internal int? WorkerId => worker?.Id;
    public IsolatedBleTransport() { }
    internal IsolatedBleTransport(bool fake) => this.fake = fake;

    private async Task StartAsync(CancellationToken ct)
    {
        if (worker is not null) return;
        var sdkPath = fake ? null : await BleSdkInstaller.EnsureAsync(ct, message => Message?.Invoke(message)).ConfigureAwait(false);
        var name = "LovenseIntegrator-" + Guid.NewGuid().ToString("N");
        pipe = new NamedPipeServerStream(name, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
        var start = new ProcessStartInfo(Environment.ProcessPath!) { UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden, WorkingDirectory = AppContext.BaseDirectory };
        if (Path.GetFileNameWithoutExtension(start.FileName).Equals("dotnet", StringComparison.OrdinalIgnoreCase)) start.ArgumentList.Add(Assembly.GetExecutingAssembly().Location);
        start.ArgumentList.Add("--ble-worker"); start.ArgumentList.Add(name);
        if (fake) start.ArgumentList.Add("--fake-ble");
        if (sdkPath is not null) { start.ArgumentList.Add("--ble-sdk"); start.ArgumentList.Add(sdkPath); }
        worker = new Process { StartInfo = start, EnableRaisingEvents = true };
        worker.Exited += (_, _) => Fail(new IOException("Bluetooth worker exited. Click «Connect / refresh» to reconnect."));
        if (!worker.Start()) throw new IOException("Could not start the Bluetooth worker.");
        await pipe.WaitForConnectionAsync(ct).WaitAsync(TimeSpan.FromSeconds(8), ct).ConfigureAwait(false);
        writer = new StreamWriter(pipe, new UTF8Encoding(false), leaveOpen: true) { AutoFlush = true };
        _ = ReadAsync(pipe);
    }
    private async Task ReadAsync(Stream stream)
    {
        try
        {
            using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
            while (await reader.ReadLineAsync().ConfigureAwait(false) is { } line)
            {
                var reply = JsonSerializer.Deserialize<BleReply>(line) ?? throw new IOException("Empty Bluetooth response.");
                if (reply.Message is { } message) Message?.Invoke(message);
                if (pending.TryRemove(reply.Id, out var completion)) completion.TrySetResult(reply);
            }
            Fail(new IOException("Bluetooth worker connection closed. Connect again."));
        }
        catch (Exception ex) { Fail(ex); }
    }
    private void Fail(Exception error)
    {
        if (Interlocked.Exchange(ref failed, 1) != 0) return;
        if (disposing || disposed)
        {
            foreach (var item in pending) if (pending.TryRemove(item.Key, out var completion)) completion.TrySetException(error);
            return;
        }
        try
        {
            var path = Path.Combine(Path.GetDirectoryName(ProfileStore.PathName)!, "bluetooth-errors.log");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.AppendAllText(path, $"{DateTimeOffset.Now:O} worker={worker?.Id} {error.Message}{Environment.NewLine}");
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
        Faulted?.Invoke(error.Message);
        foreach (var item in pending) if (pending.TryRemove(item.Key, out var completion)) completion.TrySetException(error);
    }
    private async Task<BleReply> RequestAsync(string operation, ToyCommand? command, CancellationToken ct)
    {
        await requests.WaitAsync(ct).ConfigureAwait(false);
        var id = Interlocked.Increment(ref nextId);
        try
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (Volatile.Read(ref failed) != 0) throw new IOException("Bluetooth worker unavailable. Connect again.");
            await StartAsync(ct).ConfigureAwait(false);
            var completion = new TaskCompletionSource<BleReply>(TaskCreationOptions.RunContinuationsAsynchronously); pending[id] = completion;
            await writer!.WriteLineAsync(JsonSerializer.Serialize(new BleRequest(id, operation, command))).ConfigureAwait(false);
            var reply = await completion.Task.WaitAsync(TimeSpan.FromSeconds(operation == "discover" ? 60 : 4), ct).ConfigureAwait(false);
            if (reply.Error is not null) throw new InvalidOperationException(reply.Error);
            return reply;
        }
        catch (Exception ex) when (ex is IOException or TimeoutException or OperationCanceledException) { Fail(ex); throw; }
        finally { pending.TryRemove(id, out _); requests.Release(); }
    }
    public async Task<IReadOnlyList<Toy>> DiscoverAsync(CancellationToken ct) => (await RequestAsync("discover", null, ct).ConfigureAwait(false)).Toys ?? [];
    public async Task SendAsync(ToyCommand command, CancellationToken ct)
    {
        command.Validate();
        if (command.Kind == ActionKind.Stop && worker is null) return;
        await RequestAsync("send", command, ct).ConfigureAwait(false);
    }
    internal async Task CrashForHarnessAsync() { if (!fake) throw new InvalidOperationException(); await RequestAsync("crash-test", null, CancellationToken.None); }
    public async ValueTask DisposeAsync()
    {
        if (disposed || disposing) return;
        disposing = true;
        if (worker is not null)
        {
            try { if (!worker.HasExited && failed == 0) await RequestAsync("shutdown", null, CancellationToken.None).ConfigureAwait(false); }
            catch (Exception) { /* A crashed native process cannot acknowledge Stop. The UI remains alive. */ }
            try { await worker.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(false); }
            catch (TimeoutException) { worker.Kill(entireProcessTree: true); await worker.WaitForExitAsync().ConfigureAwait(false); }
        }
        disposed = true; pipe?.Dispose(); worker?.Dispose();
    }
}

internal static class BleWorkerHost
{
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool TerminateProcess(IntPtr process, uint exitCode);

    public static async Task RunAsync(string pipeName, bool fake)
    {
        if (!fake)
        {
            var args = Environment.GetCommandLineArgs();
            var index = Array.IndexOf(args, "--ble-sdk");
            if (index < 0 || index + 1 >= args.Length || !await BleSdkInstaller.IsValidAsync(args[index + 1], CancellationToken.None))
                throw new IOException("A verified Bluetooth SDK is required.");
            NativeLibrary.SetDllImportResolver(typeof(BleTransport).Assembly, (name, _, _) =>
                name == BleSdkInstaller.FileName ? NativeLibrary.Load(args[index + 1]) : IntPtr.Zero);
        }
        using var pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
        await pipe.ConnectAsync(7000);
        using var reader = new StreamReader(pipe, Encoding.UTF8, leaveOpen: true);
        using var writer = new StreamWriter(pipe, new UTF8Encoding(false), leaveOpen: true) { AutoFlush = true };
        var outputGate = new SemaphoreSlim(1, 1);
        async Task Reply(BleReply reply)
        {
            await outputGate.WaitAsync();
            try { await writer.WriteLineAsync(JsonSerializer.Serialize(reply)); }
            finally { outputGate.Release(); }
        }
        async Task Notify(string message) { try { await Reply(new(0, Message: message)); } catch (Exception) { } }
        IToyTransport? backend = null;
        try
        {
            while (await reader.ReadLineAsync() is { } line)
            {
                var request = JsonSerializer.Deserialize<BleRequest>(line)!;
                try
                {
                    if (request.Operation == "crash-test" && fake) Environment.Exit(73);
                    if (backend is null)
                    {
                        backend = fake ? new DemoTransport() : new BleTransport();
                        if (backend is BleTransport ble) ble.Message += message => _ = Notify(message);
                    }
                    if (request.Operation == "discover") await Reply(new(request.Id, Toys: (await backend.DiscoverAsync(CancellationToken.None)).ToArray()));
                    else if (request.Operation == "send") { await backend.SendAsync(request.Command!, CancellationToken.None); await Reply(new(request.Id)); }
                    else if (request.Operation == "shutdown") { await backend.SendAsync(ToyCommand.Stop, CancellationToken.None); await Reply(new(request.Id)); break; }
                    else await Reply(new(request.Id, Error: "Unknown Bluetooth command."));
                }
                catch (Exception ex) { await Reply(new(request.Id, Error: ex.Message)); }
            }
        }
        finally
        {
            // Do not call vendor _Quit: the captured dump shows a native fail-fast in that function.
            // Environment.Exit also raced native callbacks during CLR shutdown in physical testing.
            // After Stop, terminate only this disposable worker without running DLL/CLR teardown.
            try { if (backend is not null) await backend.SendAsync(ToyCommand.Stop, CancellationToken.None); } catch (Exception) { }
            if (!TerminateProcess(new IntPtr(-1), 0)) throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        }
    }
}
