using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using LovenseIntegrator.Core;
using LovenseIntegrator.Desktop.Services;
using LovenseIntegrator.Desktop.Transports;

namespace LovenseIntegrator.Desktop.ViewModels;

public sealed record Choice<T>(T Value, string Label);
public sealed class MainViewModel : INotifyPropertyChanged, IAsyncDisposable
{
    private readonly WindowsInput input = new();
    private readonly RuleEngine engine = new();
    private readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromMilliseconds(50) };
    private readonly Dispatcher dispatcher = Dispatcher.CurrentDispatcher;
    private readonly SemaphoreSlim transportGate = new(1, 1);
    private IToyTransport transport = new DemoTransport();
    private int activeTransportIndex = -1;
    private string activeApiUrl = "";
    private readonly Func<int, string, IToyTransport> createTransport;
    private long generation;
    private bool running, busy, sending;
    private string status = "Demo mode. No commands are sent to real toys.";
    private string process = "", apiUrl = "", connectionName = "Demo mode";
    private string hotkeyStatus = "Ctrl + Alt + F12 — stop all, even in another application";
    private Rule? selected;
    private RuleEditor draft = new(new Rule());
    private string lastDraftToyId = "";
    private bool closing;
    private bool canSave = true;
    private int transportIndex, manualIntensity = 5;
    private bool observeOnly;
    public event PropertyChangedEventHandler? PropertyChanged;
    public ObservableCollection<Rule> Rules { get; } = [];
    public IReadOnlyList<Rule> AutomationRules => Rules.Where(r => !r.KeyboardLayer && !r.MouseLayer).ToArray();
    public ObservableCollection<Toy> Toys { get; } = [];
    public IReadOnlyList<Toy> Targets => new[] { new Toy("", "All connected", true) }.Concat(Toys)
        .Concat(Draft.ToyId.Length > 0 && !Toys.Any(t => t.Id == Draft.ToyId) ? [new Toy(Draft.ToyId, "Unavailable toy from profile", false)] : Array.Empty<Toy>()).ToArray();
    public ObservableCollection<string> Journal { get; } = [];
    public IReadOnlyList<RuleRecipe> Recipes => RulePresentation.Recipes;
    public IReadOnlyList<Choice<string>> MouseButtons { get; } = [new("", "Any button"), new("Left", "Left"), new("Right", "Right"), new("Middle", "Middle"), new("X1", "Side 1"), new("X2", "Side 2")];
    public IReadOnlyList<Choice<string>> WheelDirections { get; } = [new("", "Either direction"), new("Up", "Up"), new("Down", "Down")];
    public string[] Transports { get; } = ["Demo mode", "Bluetooth (direct connection)", "Lovense Remote (Local API)"];
    public int TransportIndex { get => transportIndex; set { transportIndex = value; Changed(); } }
    public Toy? SelectedToy { get; set; }
    public int ManualIntensity { get => manualIntensity; set { manualIntensity = value; Changed(); } }
    public double ManualDuration { get; set; } = 3;
    public bool Running { get => running; private set { running = value; Changed(); Changed(nameof(RunLabel)); Changed(nameof(RunStatus)); } }
    public bool Busy { get => busy; private set { busy = value; Changed(); } }
    public string Status { get => status; private set { status = value; Changed(); } }
    public string Process { get => process; private set { process = value; Changed(); } }
    public string ConnectionName { get => connectionName; private set { connectionName = value; Changed(); } }
    public string HotkeyStatus { get => hotkeyStatus; set { hotkeyStatus = value; Changed(); } }
    public string ApiUrl { get => apiUrl; set { apiUrl = value; Changed(); } }
    public string RunLabel => Running ? "Pause" : "Enable rules";
    public bool ObserveOnly { get => observeOnly; set { observeOnly = value; Changed(); Changed(nameof(RunStatus)); } }
    public string RunStatus => Running ? ObserveOnly ? "Observation only" : "Monitoring events" : "Rules paused";
    public Rule? SelectedRule { get => selected; set { selected = value; Changed(); Draft = new(value ?? new Rule()); } }
    public RuleEditor Draft
    {
        get => draft;
        private set
        {
            draft.PropertyChanged -= DraftChanged;
            draft = value; draft.PropertyChanged += DraftChanged; lastDraftToyId = draft.ToyId;
            Changed(); Changed(nameof(EventHelp)); Changed(nameof(Targets));
            RefreshApplications();
        }
    }
    public IReadOnlyList<EventOption> Events => RulePresentation.Events;
    public IReadOnlyList<Choice<ActionKind>> Actions { get; } =
    [new(ActionKind.Vibrate, "Vibration"), new(ActionKind.Pulse, "Pulse"), new(ActionKind.RateMapped, "Intensity from typing speed"), new(ActionKind.Stop, "Stop")];
    public string EventHelp => Draft.EventHelp;
    public MainViewModel(int initialTransport = 0, Func<int, string, IToyTransport>? transportFactory = null)
    {
        createTransport = transportFactory ?? ((index, url) => index switch { 1 => new IsolatedBleTransport(), 2 => new LocalApiTransport(url), _ => new DemoTransport() });
        Rules.CollectionChanged += (_, _) => Changed(nameof(AutomationRules));
        TransportIndex = initialTransport;
        try { SetProfile(ProfileStore.Load()); }
        catch (Exception ex) { canSave = false; SetProfile(ProfileStore.Defaults()); Log($"Could not read the profile: {ex.Message}. Original file preserved; autosave is disabled."); }
        input.Received += e =>
        {
            // Keep the OS hook short. Evaluate outside the native callback.
            if (!Running || closing) return;
            var revision = generation;
            dispatcher.BeginInvoke(() => { if (revision == generation) Handle(e); }, DispatcherPriority.Input);
        };
        timer.Tick += (_, _) =>
        {
            if (!Running) return;
            var now = Environment.TickCount64;
            var foreground = WindowsInput.ForegroundProcess();
            if (foreground != Process) { Process = foreground; Handle(new(EventKind.ForegroundChanged, now)); }
            Handle(new(EventKind.Timer, now));
        };
        SelectedRule = AutomationRules.FirstOrDefault();
        _ = DiscoverAsync();
    }
    private void Handle(InputEvent e)
    {
        if (!Running || closing) return;
        var focus = WindowsInput.ForegroundContext(Rules.Any(r => r.Enabled && r.WindowTitleContains.Length > 0));
        var match = engine.Process(e with { Process = focus.Process, WindowTitle = focus.Title }, Rules);
        if (match is not null) _ = FireAsync(match);
    }
    private void DraftChanged(object? sender, PropertyChangedEventArgs e)
    {
        Changed(nameof(EventHelp));
        if (lastDraftToyId == Draft.ToyId) return;
        lastDraftToyId = Draft.ToyId;
        Changed(nameof(Targets));
    }
    public void RefreshApplications()
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var app in System.Diagnostics.Process.GetProcesses())
        {
            using (app)
            {
                try { if (app.MainWindowHandle != 0 && app.ProcessName != "LovenseIntegrator") names.Add(app.ProcessName); }
                catch (System.ComponentModel.Win32Exception) { }
                catch (InvalidOperationException) { }
            }
        }
        if (Draft.Process.Length > 0) names.Add(Draft.Process);
        // Replace as one list to keep the selected process stable while refreshing.
        applicationChoices = new[] { new Choice<string>("", "Any application") }.Concat(names.OrderBy(n => n).Select(n => new Choice<string>(n, n))).ToArray();
        Changed(nameof(ApplicationChoices));
    }
    private IReadOnlyList<Choice<string>> applicationChoices = [];
    public IReadOnlyList<Choice<string>> ApplicationChoices => applicationChoices;
    public void Log(string message)
    {
        if (dispatcher.HasShutdownStarted) return;
        if (!dispatcher.CheckAccess()) { dispatcher.BeginInvoke(() => Log(message)); return; }
        Status = message;
        Journal.Insert(0, $"{DateTime.Now:HH:mm:ss}  {message}");
        while (Journal.Count > 100) Journal.RemoveAt(Journal.Count - 1);
    }
    public void NewRule() { SelectedRule = null; Draft = new(new Rule()); }
    public void UseRecipe(RuleRecipe recipe)
    {
        var rule = recipe.Rule.Copy(); rule.Id = Guid.NewGuid();
        SelectedRule = null; Draft = new(rule);
    }
    public void SaveRule()
    {
        if (Draft.Validate() is { } error) { Log(error); return; }
        var saved = Draft.Copy();
        var index = SelectedRule is null ? -1 : Rules.IndexOf(SelectedRule);
        if (index < 0) Rules.Add(saved); else Rules[index] = saved;
        SelectedRule = saved; engine.Reset(Environment.TickCount64); Persist();
        Log($"Rule «{saved.Name}» saved.");
    }
    public void DeleteRule()
    {
        if (SelectedRule is null) return;
        Rules.Remove(SelectedRule); SelectedRule = AutomationRules.FirstOrDefault(); engine.Reset(Environment.TickCount64); Persist();
    }
    public void SaveKeyboardRule(Rule rule)
    {
        if (!rule.KeyboardLayer || rule.Validate() is { }) throw new ArgumentException(rule.Validate() ?? "Not a keyboard assignment.");
        var index = Rules.ToList().FindIndex(r => r.Id == rule.Id);
        if (index < 0) Rules.Add(rule.Copy()); else Rules[index] = rule.Copy();
        if (SelectedRule?.Id == rule.Id) SelectedRule = AutomationRules.FirstOrDefault();
        engine.Reset(Environment.TickCount64); Persist(); Log($"Assignment «{rule.Name}» saved.");
    }
    public void RemoveKeyboardRule(Guid id)
    {
        var rule = Rules.FirstOrDefault(r => r.Event == EventKind.KeyDown && r.Id == id);
        if (rule is null) return;
        Rules.Remove(rule); engine.Reset(Environment.TickCount64); Persist(); Log("Assignment deleted.");
        if (SelectedRule?.Id == id) SelectedRule = AutomationRules.FirstOrDefault();
    }
    public async Task ToggleAsync()
    {
        if (Running) { await StopAsync(); return; }
        if (Busy) { Log("Wait for the connection to finish."); return; }
        try
        {
            engine.Reset(Environment.TickCount64); input.Start(); Running = true; timer.Start();
            Log("Rules enabled. Input is processed locally; typed text is not stored.");
        }
        catch (Exception ex) { Log($"Could not enable events: {ex.Message}"); }
    }
    public void SaveMouseRule(Rule rule)
    {
        MousePresetStore.Validate([rule], MouseInputs.All);
        var index = Rules.ToList().FindIndex(r => r.Id == rule.Id);
        if (index < 0) Rules.Add(rule.Copy()); else Rules[index] = rule.Copy();
        if (SelectedRule?.Id == rule.Id) SelectedRule = AutomationRules.FirstOrDefault();
        engine.Reset(Environment.TickCount64); Persist(); Log($"Mouse assignment «{rule.Name}» saved.");
    }
    public void RemoveMouseRule(Guid id)
    {
        var rule = Rules.FirstOrDefault(r => MousePresetStore.CanEdit(r) && r.Id == id);
        if (rule is null) return;
        Rules.Remove(rule); engine.Reset(Environment.TickCount64); Persist(); Log("Mouse assignment deleted.");
        if (SelectedRule?.Id == id) SelectedRule = AutomationRules.FirstOrDefault();
    }
    private void Pause()
    {
        Running = false; input.Stop(); timer.Stop(); engine.Reset(Environment.TickCount64);
        Interlocked.Increment(ref generation);
    }
    public async Task StopAsync()
    {
        Pause(); await transportGate.WaitAsync();
        try { await transport.SendAsync(ToyCommand.Stop, CancellationToken.None); Log("All toys stopped. Rules are paused."); }
        catch (Exception ex) { Log($"Could not confirm stop: {ex.Message}"); }
        finally { transportGate.Release(); }
    }
    public async Task DiscoverAsync()
    {
        if (Busy || closing) return;
        Pause(); Busy = true;
        await transportGate.WaitAsync();
        try
        {
            if (transport is not IsolatedBleTransport broken || broken.CanReuse) await transport.SendAsync(ToyCommand.Stop, CancellationToken.None);
            var reuse = activeTransportIndex == TransportIndex && (TransportIndex != 2 || activeApiUrl == ApiUrl) && (transport is not IsolatedBleTransport isolated || isolated.CanReuse);
            if (!reuse)
            {
                await transport.DisposeAsync(); transport = new OfflineTransport(); activeTransportIndex = -1;
                ConnectionName = transport.Name;
                transport = createTransport(TransportIndex, ApiUrl); activeTransportIndex = TransportIndex; activeApiUrl = ApiUrl;
                if (transport is IsolatedBleTransport ble)
                {
                    ble.Message += Log;
                    ble.Faulted += error => dispatcher.BeginInvoke(() =>
                    {
                        if (!ReferenceEquals(transport, ble)) return;
                        Pause();
                        for (var i = 0; i < Toys.Count; i++) Toys[i] = Toys[i] with { Connected = false };
                        SelectedToy = null; Changed(nameof(SelectedToy)); Changed(nameof(Targets));
                        Log("Bluetooth worker exited. Rules are paused. " + error);
                    });
                }
            }
            var selectedId = SelectedToy?.Id;
            ConnectionName = transport.Name; Toys.Clear();
            Log("Finding devices…");
            foreach (var toy in await transport.DiscoverAsync(CancellationToken.None)) Toys.Add(toy);
            SelectedToy = Toys.FirstOrDefault(t => t.Id == selectedId) ?? Toys.FirstOrDefault(); Changed(nameof(SelectedToy)); Persist();
            Changed(nameof(Targets));
            Log(Toys.Any(t => t.Connected) ? $"Devices found: {Toys.Count}. Rules are paused." : "No devices connected. Check power and connection.");
        }
        catch (DllNotFoundException) { Log("LovenseBLE_Lib.dll not found. Run scripts/Setup-Ble.ps1 and rebuild the application."); }
        catch (BadImageFormatException) { Log("A 64-bit LovenseBLE_Lib.dll is required."); }
        catch (Exception ex) { Log($"Connection failed: {ex.Message}"); }
        finally { Busy = false; transportGate.Release(); }
    }
    public Task TestRuleAsync()
    {
        if (Draft.Validate() is { } error) { Log(error); return Task.CompletedTask; }
        return FireAsync(new(Draft.Copy(), Draft.Threshold), false);
    }
    public Task ManualAsync(bool pulse) => SendAsync(new(pulse ? ActionKind.Pulse : ActionKind.Vibrate, ManualIntensity, ManualDuration, ToyId: SelectedToy?.Id ?? ""), "Manual control");
    internal Task FireAsync(RuleMatch match, bool automatic = true)
    {
        var r = match.Rule;
        if (automatic && ObserveOnly) { Log($"Observation: «{r.Name}» triggered; value {RulePresentation.Number(match.Value)}. No command sent."); return Task.CompletedTask; }
        return SendAsync(match.Command(), $"Rule «{r.Name}»");
    }
    private async Task SendAsync(ToyCommand command, string origin)
    {
        if (closing || Busy || sending) return;
        var revision = generation; sending = true;
        await transportGate.WaitAsync();
        try
        {
            if (revision != generation || closing) return;
            if (command.Kind != ActionKind.Stop && !Toys.Any(t => t.Connected && (command.ToyId.Length == 0 || t.Id == command.ToyId)))
                throw new InvalidOperationException("The selected toy is not connected.");
            await transport.SendAsync(command, CancellationToken.None);
            var actionName = command.Kind == ActionKind.Pulse ? "pulse" : "vibration";
            if (command.RenewalId.Length == 0 || command.RenewalId != lastWheelLogId || Environment.TickCount64 - lastWheelLogAt >= 500)
            {
                lastWheelLogId = command.RenewalId; lastWheelLogAt = Environment.TickCount64;
                Log($"{origin}: {(command.Kind == ActionKind.Stop ? "stop" : $"{actionName}, {command.Intensity}/20, {RulePresentation.Duration(command.DurationSeconds)}")}{(transport is DemoTransport ? " [demo]" : "")}");
            }
        }
        catch (Exception ex) { Log($"{origin}: {ex.Message}"); }
        finally { sending = false; transportGate.Release(); }
    }
    public Profile GetProfile() => new() { ApiUrl = ApiUrl, Rules = Rules.Select(r => r.Copy()).ToList() };
    private string lastWheelLogId = "";
    private long lastWheelLogAt;
    public void SetProfile(Profile profile)
    {
        Rules.Clear(); foreach (var rule in profile.Rules) Rules.Add(rule);
        ApiUrl = profile.ApiUrl; SelectedRule = AutomationRules.FirstOrDefault();
        engine.Reset(Environment.TickCount64);
    }
    public async Task ImportProfileAsync(Profile profile)
    {
        await StopAsync(); SetProfile(profile); canSave = true; Persist(); Log("Profile imported and saved. Rules are paused.");
    }
    public async Task ImportKeyboardAsync(KeyboardPreset preset)
    {
        KeyboardPresetStore.Validate(preset.Rules, KeyboardLayout.Keys.Select(k => k.Id));
        var merged = KeyboardPresetStore.Merge(Rules, preset.Rules);
        var next = new Profile { ApiUrl = ApiUrl, Rules = merged };
        await StopAsync();
        // Persist before replacing live rules, so a failed write leaves the current configuration intact.
        if (System.IO.File.Exists(ProfileStore.PathName)) System.IO.File.Copy(ProfileStore.PathName, ProfileStore.PathName + ".before-keyboard-import.bak", true);
        ProfileStore.Save(next);
        SetProfile(next); canSave = true;
        Log($"Keyboard assignments imported: {preset.Rules.Count}. Other events preserved. Rules are paused.");
    }
    private void Persist()
    {
        if (!canSave) { Log("Autosave is disabled: recover the damaged profile or export a new one."); return; }
        try { ProfileStore.Save(GetProfile()); } catch (Exception ex) { Log($"Profile not saved: {ex.Message}"); }
    }
    public async Task ImportMouseAsync(MousePreset preset)
    {
        MousePresetStore.Validate(preset.Rules, MouseInputs.All);
        var next = new Profile { ApiUrl = ApiUrl, Rules = MousePresetStore.Merge(Rules, preset.Rules) };
        await StopAsync();
        if (System.IO.File.Exists(ProfileStore.PathName)) System.IO.File.Copy(ProfileStore.PathName, ProfileStore.PathName + ".before-mouse-import.bak", true);
        ProfileStore.Save(next); SetProfile(next); canSave = true;
        Log($"Mouse assignments imported: {preset.Rules.Count}. Other events preserved. Rules are paused.");
    }
    private void Changed([CallerMemberName] string? property = null) => PropertyChanged?.Invoke(this, new(property));
    public async ValueTask DisposeAsync()
    {
        closing = true; await StopAsync(); input.Dispose();
        await transportGate.WaitAsync();
        try { await transport.DisposeAsync(); } finally { transportGate.Release(); }
    }
}
