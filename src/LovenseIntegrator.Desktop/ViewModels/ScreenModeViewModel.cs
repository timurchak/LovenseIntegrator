using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using LovenseIntegrator.Core;
using LovenseIntegrator.Desktop.Localization;
using LovenseIntegrator.Desktop.Services;

namespace LovenseIntegrator.Desktop.ViewModels;

public sealed class ScreenRegion
{
    public int X { get; set; }
    public int Y { get; set; }
    public int CellSize { get; set; } = 4;
    public bool Valid => X is >= 0 and <= 32768 && Y is >= 0 and <= 32768 && CellSize is >= 1 and <= 16;
}

public sealed class ScreenModeViewModel : INotifyPropertyChanged, IAsyncDisposable
{
    private readonly MainViewModel owner;
    private readonly Func<int, int, int, ScreenCaptureResult> capture;
    private readonly Dispatcher dispatcher = Dispatcher.CurrentDispatcher;
    private readonly ScreenPacketTracker tracker = new();
    private CancellationTokenSource? lifetime;
    private Task? loop;
    private Task stopOperation = Task.CompletedTask;
    private bool stopping;
    private int revision;
    private bool armed;
    private Rule? selected;
    private RuleEditor draft = new(new Rule { Event = EventKind.ScreenEvent, Keys = "", Process = "Wow", DurationSeconds = 0.15, Intensity = 5 });
    private string status = L.T("Screen capture is stopped.");
    private ImageSource? preview;
    public event PropertyChangedEventHandler? PropertyChanged;
    public ScreenModeViewModel(MainViewModel owner, Func<int, int, int, ScreenCaptureResult>? capture = null)
    {
        this.owner = owner;
        this.capture = capture ?? ScreenCapture.Read;
        owner.Rules.CollectionChanged += (_, _) => Changed(nameof(Assignments));
        try { Region = JsonSerializer.Deserialize<ScreenRegion>(File.ReadAllText(SettingsPath)) is { Valid: true } saved ? saved : new(); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException) { Region = new(); }
    }
    private static string SettingsPath => Path.Combine(Path.GetDirectoryName(ProfileStore.PathName)!, "screen.json");
    public ScreenRegion Region { get; }
    public IReadOnlyList<Rule> Assignments => owner.Rules.Where(r => r.Event == EventKind.ScreenEvent).ToArray();
    public IReadOnlyList<Choice<string>> Events => ScreenEvents.All.Where(e => e.Id != "wow.test").Select(e => new Choice<string>(e.Id, L.T(e.Name))).ToArray();
    public IReadOnlyList<Choice<ActionKind>> Actions { get; } = [new(ActionKind.Vibrate, L.T("Vibration")), new(ActionKind.Pulse, L.T("Pulse")), new(ActionKind.Stop, L.T("Stop"))];
    public IReadOnlyList<Toy> Targets => owner.Toys.Prepend(new Toy("", L.T("All connected"), true))
        .Concat(Draft.ToyId.Length > 0 && !owner.Toys.Any(t => t.Id == Draft.ToyId) ? [new Toy(Draft.ToyId, L.T("Unavailable toy from profile"), false)] : []).ToArray();
    public Rule? Selected { get => selected; set { selected = value; if (value is not null) Draft = new(value); Changed(); Changed(nameof(Targets)); } }
    public RuleEditor Draft { get => draft; private set { draft = value; Changed(); Changed(nameof(Targets)); } }
    public bool Monitoring => lifetime is not null;
    public bool CanConfigure => !Monitoring;
    public bool CanToggle => !stopping;
    public string MonitorLabel => Monitoring ? L.T("Stop reading") : L.T("Start reading");
    public string Status { get => status; private set { status = value; Changed(); } }
    public ImageSource? Preview { get => preview; private set { preview = value; Changed(); } }
    public ObservableCollection<string> RecentEvents { get; } = [];
    public void New() { Selected = null; Draft = new(new Rule { Name = L.T("Combat started"), Event = EventKind.ScreenEvent, Keys = "", Process = "Wow", DurationSeconds = 0.15, Intensity = 5 }); }
    public void Save() { if (Draft.Validate() is { } error) { owner.Log(error); return; } owner.SaveScreenRule(Draft.Copy()); Selected = Assignments.First(r => r.Id == Draft.Copy().Id); }
    public void Delete() { if (Selected is { } rule) owner.RemoveScreenRule(rule.Id); New(); }
    public void ResetBaseline() { revision++; tracker.Reset(); armed = false; }
    public async Task ToggleMonitoringAsync()
    {
        if (stopping) return;
        if (Monitoring) { await StopMonitoringAsync(); return; }
        if (!Region.Valid) { Status = L.T("Use nonnegative coordinates and a cell size from 1 to 16 pixels."); return; }
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
            File.WriteAllText(SettingsPath + ".tmp", JsonSerializer.Serialize(Region)); File.Move(SettingsPath + ".tmp", SettingsPath, true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { owner.Log(ex.Message); return; }
        ResetBaseline(); lifetime = new();
        var token = lifetime.Token;
        var x = Region.X; var y = Region.Y; var cellSize = Region.CellSize;
        loop = Task.Run(async () =>
        {
            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(50));
            try
            {
                while (await timer.WaitForNextTickAsync(token))
                {
                    var currentRevision = Volatile.Read(ref revision);
                    var result = capture(x, y, cellSize);
                    var at = Environment.TickCount64;
                    var packet = result.Pixels is null ? null : ScreenProtocol.DecodeBgra(result.Pixels, cellSize);
                    await dispatcher.InvokeAsync(() =>
                    {
                        if (token.IsCancellationRequested || currentRevision != revision) return;
                        ApplySample(result, packet, at, cellSize);
                    });
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                await dispatcher.InvokeAsync(() => { if (!token.IsCancellationRequested) { Status = L.T("Screen capture failed."); owner.Log(ex.Message); _ = StopMonitoringAsync(); } });
            }
        });
        Changed(nameof(Monitoring)); Changed(nameof(CanConfigure)); Changed(nameof(MonitorLabel)); Changed(nameof(Targets));
        Status = L.T("Waiting for World of Warcraft in the foreground.");
    }
    internal void ApplySample(ScreenCaptureResult result, ScreenPacket? packet, long at, int cellSize)
    {
        var wasHealthy = tracker.Healthy;
        var received = tracker.Accept(packet, at);
        if (tracker.Healthy) armed = true;
        if (result.Pixels is { } bytes)
        {
            var bitmap = BitmapSource.Create(8 * cellSize, 8 * cellSize, 96, 96, PixelFormats.Bgr32, null, bytes, 8 * cellSize * 4);
            bitmap.Freeze(); Preview = bitmap;
        }
        else Preview = null;
        Status = result.Status.Length > 0 ? L.T(result.Status) : packet is null ? L.T("No valid addon signal. Check the region and cell size.") :
            !tracker.Healthy ? L.T("Waiting for a live addon heartbeat.") : packet.Restricted ? L.T("Connected. Some combat data is hidden by WoW.") : L.T("Connected to WowScreenEvents.");
        // Focus loss is immediate; noisy/occluded frames allow at most one second.
        if (armed && (result.Pixels is null || wasHealthy && !tracker.Healthy)) { armed = false; _ = owner.ScreenLostAsync(); }
        if (received is null) return;
        var definition = ScreenEvents.Find(received.EventCode)!;
        RecentEvents.Insert(0, $"{DateTime.Now:HH:mm:ss}  {L.T(definition.Name)}{(received.IsTest ? " · " + L.T("Preview only") : "")} · {received.Value}");
        while (RecentEvents.Count > 30) RecentEvents.RemoveAt(RecentEvents.Count - 1);
        if (!received.IsTest) owner.ReceiveScreen(new(EventKind.ScreenEvent, at, Process: result.Process, WindowTitle: result.Title, Value: received.Value, ScreenEventId: definition.Id));
    }
    public Task StopMonitoringAsync()
    {
        if (stopping) return stopOperation;
        if (lifetime is null) return Task.CompletedTask;
        stopping = true; Changed(nameof(CanToggle));
        stopOperation = StopCoreAsync();
        return stopOperation;
    }
    private async Task StopCoreAsync()
    {
        var current = lifetime!;
        var currentLoop = loop;
        lifetime = null; current.Cancel(); ResetBaseline();
        try
        {
            await owner.ScreenLostAsync();
            if (currentLoop is not null) await currentLoop;
        }
        finally
        {
            current.Dispose(); loop = null; Preview = null; Status = L.T("Screen capture is stopped."); stopping = false;
            Changed(nameof(Monitoring)); Changed(nameof(CanConfigure)); Changed(nameof(MonitorLabel)); Changed(nameof(CanToggle));
        }
    }
    private void Changed([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new(name));
    public async ValueTask DisposeAsync() => await StopMonitoringAsync();
}
