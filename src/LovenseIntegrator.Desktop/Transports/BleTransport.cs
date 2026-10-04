using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using LovenseIntegrator.Core;

namespace LovenseIntegrator.Desktop.Transports;

// ABI checked against the official C# demo, which specifies more callback arguments than the web summary.
// x64 uses a unified calling convention. Delegate fields stay rooted through _Quit().
public sealed class BleTransport : IToyTransport
{
    private readonly ConcurrentDictionary<string, Toy> toys = new();
    private readonly Dictionary<string, CancellationTokenSource> jobs = [];
    private readonly Dictionary<string, (string Owner, int Level)> renewable = [];
    private readonly object gate = new();
    private readonly Native.Notify notify;
    private readonly Native.Found found;
    private readonly Native.Connection connection;
    private readonly Native.Battery battery;
    private readonly Native.Status status;
    private readonly Action<string, int> writeLevel = (id, level) => Native._SendCommand(id, 0, level);
    private readonly Func<int, CancellationToken, Task> delay = (milliseconds, ct) => Task.Delay(milliseconds, ct);
    private readonly Action quit = Native._Quit;
    private readonly Action startScan = Native._StartBLEScan, stopScan = Native._StopBLEScan;
    private readonly Action<string> connectToy = Native._ConnectToy;
    private readonly Action<string> queryBattery = id => Native._SendCommand(id, 12, 0);
    private readonly SemaphoreSlim discoveryGate = new(1, 1);
    private readonly ConcurrentDictionary<string, TaskCompletionSource<bool>> connections = new();
    private readonly ConcurrentDictionary<string, TaskCompletionSource<bool>> batteries = new();
    private TaskCompletionSource<bool>? scanStopped;
    private bool disposed;
    private readonly TaskCompletionSource<int> availability = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public event Action<string>? Message;
    public string Name => "Bluetooth · Lovense BLE";

    public BleTransport() : this(true) { }
    // Replay harness uses the same command/timer implementation without loading the vendor DLL.
    internal BleTransport(IReadOnlyList<Toy> initialToys, Action<string, int> write, Func<int, CancellationToken, Task> wait,
        Action? scan = null, Action? stop = null, Action<string>? connect = null, Action<string>? batteryQuery = null) : this(false)
    {
        writeLevel = write; delay = wait; quit = () => { };
        startScan = scan ?? (() => { }); stopScan = stop ?? (() => ReportScanStopped());
        connectToy = connect ?? (id => ReportConnection(id, true)); queryBattery = batteryQuery ?? (id => ReportBattery(id, "80"));
        foreach (var toy in initialToys) toys[toy.Id] = toy;
    }
    private BleTransport(bool registerSdk)
    {
        notify = (code, id) =>
        {
            if (disposed) return;
            if (code == 1) ReportScanStopped();
            if (code == 8 && connections.TryGetValue(id, out var failed)) failed.TrySetResult(false);
            Message?.Invoke(code switch
        {
            0 => "Scanning for Bluetooth devices…", 1 => "Bluetooth scan completed.", 2 => "Bluetooth command sent.",
            3 => "Bluetooth: command rejected.", 4 => "Bluetooth: stop scanning first.",
            5 => "Bluetooth: device disconnected.", 6 => "Bluetooth: could not disconnect the device.", 7 => "Bluetooth: connected.",
            8 => "Bluetooth: could not connect.", 9 => "Bluetooth: device disconnected.",
            10 => "Bluetooth unavailable or disabled.", _ => $"Bluetooth: unknown status {code}"
            });
        };
        found = (id, name, _, a, b) =>
        {
            // Official support CSV: s=Lush family, x=Ferri. Do not infer the generation from this symbol.
            var symbol = $"{(char)a}{(char)b}".Trim('\0', '0').ToLowerInvariant();
            var model = symbol switch { "s" => "Lush", "x" => "Ferri", _ => "" };
            toys.TryAdd(id, new(id, model.Length == 0 ? name : $"{model} · {name}", false));
        };
        connection = ReportConnection;
        battery = ReportBattery;
        status = code => { availability.TrySetResult(code); Message?.Invoke(code == 0 ? "Bluetooth enabled." : "Enable Bluetooth or connect an adapter."); };
        if (!registerSdk) { availability.TrySetResult(0); return; }
        Native._RegisterNotifyCallback(notify); Native._RegisterToyAddCallback(found);
        Native._RegisterConnectChangedCallback(connection); Native._RegisterBatteryCallback(battery);
        Native._RegisterCheckBLECallback(status);
        Native._CheckBLEStatus();
    }
    internal void ReportScanStopped() => scanStopped?.TrySetResult(true);
    internal void ReportConnection(string id, bool connected)
        {
            if (disposed) return;
            toys.AddOrUpdate(id, new Toy(id, "Lovense", connected), (_, toy) => toy with { Connected = connected });
            if (connections.TryGetValue(id, out var pending)) pending.TrySetResult(connected);
            if (!connected)
            {
                lock (gate) { renewable.Remove(id); if (jobs.Remove(id, out var job)) { job.Cancel(); job.Dispose(); } }
            }
            Message?.Invoke(connected ? "BLE: toy connected." : "BLE: toy disconnected.");
        }
    internal void ReportBattery(string id, string value)
    {
        if (disposed) return;
        if (int.TryParse(value, out var percent)) toys.AddOrUpdate(id, new Toy(id, "Lovense", false, percent), (_, toy) => toy with { Battery = percent });
        if (batteries.TryGetValue(id, out var pending)) pending.TrySetResult(true);
    }
    public async Task<int> CheckAvailabilityAsync(CancellationToken ct) => await availability.Task.WaitAsync(TimeSpan.FromSeconds(3), ct);
    public Task<IReadOnlyList<Toy>> DiscoverAsync(CancellationToken ct) => DiscoverAsync(ct, 4000, 6000);
    public async Task<IReadOnlyList<Toy>> DiscoverAsync(CancellationToken ct, int scanMs, int connectMs)
    {
        await discoveryGate.WaitAsync(ct);
        try
        {
        ObjectDisposedException.ThrowIf(disposed, this);
        var state = await CheckAvailabilityAsync(ct);
        if (state != 0) throw new InvalidOperationException(state == 1 ? "Enable Bluetooth in Windows settings." : "The adapter does not support the available Windows BLE interface.");
        // The SDK rejects commands during scanning. Stop current output before scanning.
        await SendAsync(ToyCommand.Stop, ct);
        scanStopped = new(TaskCreationOptions.RunContinuationsAsynchronously);
        startScan();
        try { await Task.Delay(scanMs, ct); }
        finally { stopScan(); }
        await scanStopped.Task.WaitAsync(TimeSpan.FromSeconds(3), ct);
        // Wait for each connection result before contacting the next device. Battery requests never run in callbacks.
        foreach (var toy in toys.Values.OrderBy(t => t.Id))
        {
            ct.ThrowIfCancellationRequested();
            if (!toys[toy.Id].Connected)
            {
                var pending = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously); connections[toy.Id] = pending;
                try
                {
                    Message?.Invoke($"Connecting: {toy.Name}…"); connectToy(toy.Id);
                    if (!await pending.Task.WaitAsync(TimeSpan.FromMilliseconds(connectMs), ct)) { Message?.Invoke($"Could not connect {toy.Name}."); continue; }
                }
                catch (TimeoutException) { Message?.Invoke($"{toy.Name}: connection not confirmed. Refresh to retry."); continue; }
                finally { connections.TryRemove(toy.Id, out _); }
            }
            var batteryPending = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously); batteries[toy.Id] = batteryPending;
            try { queryBattery(toy.Id); await batteryPending.Task.WaitAsync(TimeSpan.FromSeconds(1), ct); }
            catch (TimeoutException) { }
            finally { batteries.TryRemove(toy.Id, out _); }
        }
        return toys.Values.OrderBy(t => t.Name).ToArray();
        }
        finally { discoveryGate.Release(); }
    }
    public Task SendAsync(ToyCommand command, CancellationToken ct)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        ct.ThrowIfCancellationRequested(); command.Validate();
        lock (gate)
        {
            var targets = toys.Values.Where(t => t.Connected && (string.IsNullOrEmpty(command.ToyId) || t.Id == command.ToyId)).ToArray();
            if (targets.Length == 0 && command.Kind != ActionKind.Stop) throw new InvalidOperationException("No Bluetooth toys connected.");
            foreach (var toy in targets)
            {
                var extendOnly = command.RenewalId.Length > 0 && renewable.TryGetValue(toy.Id, out var state) && state == (command.RenewalId, command.Intensity) && jobs.ContainsKey(toy.Id);
                if (jobs.Remove(toy.Id, out var old)) { old.Cancel(); old.Dispose(); }
                renewable.Remove(toy.Id);
                if (!extendOnly) writeLevel(toy.Id, command.Kind == ActionKind.Stop ? 0 : command.Intensity);
                if (command.Kind == ActionKind.Stop) continue;
                if (command.RenewalId.Length > 0) renewable[toy.Id] = (command.RenewalId, command.Intensity);
                var job = CancellationTokenSource.CreateLinkedTokenSource(ct);
                jobs[toy.Id] = job;
                _ = FinishAsync(toy.Id, command, job.Token);
            }
        }
        return Task.CompletedTask;
    }
    private async Task FinishAsync(string id, ToyCommand command, CancellationToken ct)
    {
        try
        {
            var remaining = (int)Math.Round(command.DurationSeconds * 1000);
            var on = true;
            while (remaining > 0)
            {
                var wait = command.Kind == ActionKind.Pulse ? Math.Min(command.PulseMs, remaining) : remaining;
                await delay(wait, ct).ConfigureAwait(false); remaining -= wait;
                lock (gate)
                {
                    if (disposed || ct.IsCancellationRequested) return;
                    on = !on;
                    writeLevel(id, remaining == 0 || !on ? 0 : command.Intensity);
                    if (remaining == 0) { renewable.Remove(id); if (jobs.Remove(id, out var completed)) completed.Dispose(); }
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { lock (gate) { if (!ct.IsCancellationRequested) renewable.Remove(id); } Message?.Invoke($"BLE: {ex.Message}"); }
    }
    public async ValueTask DisposeAsync()
    {
        if (disposed) return;
        await SendAsync(ToyCommand.Stop, CancellationToken.None);
        lock (gate)
        {
            disposed = true;
            foreach (var job in jobs.Values) { job.Cancel(); job.Dispose(); }
            jobs.Clear();
            renewable.Clear();
        }
        quit();
        GC.KeepAlive(notify); GC.KeepAlive(found); GC.KeepAlive(connection); GC.KeepAlive(battery); GC.KeepAlive(status);
    }
    private static class Native
    {
        private const string Dll = "LovenseBLE_Lib.dll";
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] public delegate void Notify(int result, [MarshalAs(UnmanagedType.LPWStr)] string id);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] public delegate void Found([MarshalAs(UnmanagedType.LPWStr)] string id, [MarshalAs(UnmanagedType.LPWStr)] string name, [MarshalAs(UnmanagedType.LPWStr)] string address, byte a, byte b);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] public delegate void Connection([MarshalAs(UnmanagedType.LPWStr)] string id, [MarshalAs(UnmanagedType.I1)] bool connected);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] public delegate void Battery([MarshalAs(UnmanagedType.LPWStr)] string id, [MarshalAs(UnmanagedType.LPWStr)] string value);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] public delegate void Status(int status);
        [DllImport(Dll)] public static extern void _RegisterNotifyCallback(Notify cb);
        [DllImport(Dll)] public static extern void _RegisterToyAddCallback(Found cb);
        [DllImport(Dll)] public static extern void _RegisterConnectChangedCallback(Connection cb);
        [DllImport(Dll)] public static extern void _RegisterBatteryCallback(Battery cb);
        [DllImport(Dll)] public static extern void _RegisterCheckBLECallback(Status cb);
        [DllImport(Dll)] public static extern void _CheckBLEStatus();
        [DllImport(Dll)] public static extern void _StartBLEScan();
        [DllImport(Dll)] public static extern void _StopBLEScan();
        [DllImport(Dll, CharSet = CharSet.Unicode)] public static extern void _ConnectToy(string id);
        [DllImport(Dll, CharSet = CharSet.Unicode)] public static extern void _SendCommand(string id, int type, int value);
        [DllImport(Dll)] public static extern void _Quit();
    }
}
