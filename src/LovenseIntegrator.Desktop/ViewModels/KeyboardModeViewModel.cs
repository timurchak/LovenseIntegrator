using LovenseIntegrator.Desktop.Localization;
using System.ComponentModel;
using System.Windows.Input;
using LovenseIntegrator.Core;
using LovenseIntegrator.Desktop.Services;

namespace LovenseIntegrator.Desktop.ViewModels;

public sealed record KeyboardKey(string Id, string Label, string Secondary, double X, double Y, double Width = 1, double Height = 1);
public static class KeyboardLayout
{
    public static IReadOnlyList<KeyboardKey> Keys { get; } = Build();
    private static IReadOnlyList<KeyboardKey> Build()
    {
        var result = new List<KeyboardKey>();
        void Add(int vk, string label, double x, double y, double width = 1, double height = 1, string secondary = "") =>
            result.Add(new(KeyInterop.KeyFromVirtualKey(vk).ToString(), label, secondary, x, y, width, height));
        Add(27, "Esc", 0, 0);
        for (var i = 0; i < 12; i++) Add(112 + i, $"F{i + 1}", 2 + i + i / 4 * 0.5, 0);
        Add(192, "~", 0, 1);
        for (var i = 0; i < 10; i++) Add(48 + (i + 1) % 10, ((i + 1) % 10).ToString(), i + 1, 1);
        Add(189, "−", 11, 1); Add(187, "+", 12, 1); Add(8, "Backspace", 13, 1, 2);
        Add(9, "Tab", 0, 2, 1.5);
        const string upper = "QWERTYUIOP", middle = "ASDFGHJKL", lower = "ZXCVBNM";
        for (var i = 0; i < upper.Length; i++) Add(upper[i], upper[i].ToString(), 1.5 + i, 2);
        Add(219, "[", 11.5, 2); Add(221, "]", 12.5, 2); Add(220, "\\", 13.5, 2, 1.5);
        Add(20, "Caps", 0, 3, 1.75);
        for (var i = 0; i < middle.Length; i++) Add(middle[i], middle[i].ToString(), 1.75 + i, 3);
        Add(186, ";", 10.75, 3); Add(222, "'", 11.75, 3); Add(13, "Enter", 12.75, 3, 2.25);
        Add(160, "Shift", 0, 4, 2.25);
        for (var i = 0; i < lower.Length; i++) Add(lower[i], lower[i].ToString(), 2.25 + i, 4);
        Add(188, ",", 9.25, 4); Add(190, ".", 10.25, 4); Add(191, "/", 11.25, 4); Add(161, "Shift", 12.25, 4, 2.75);
        Add(162, "Ctrl", 0, 5, 1.25); Add(91, "Win", 1.25, 5, 1.25); Add(164, "Alt", 2.5, 5, 1.25); Add(32, "Space", 3.75, 5, 6.25);
        Add(165, "Alt", 10, 5, 1.25); Add(92, "Win", 11.25, 5, 1.25); Add(93, "Menu", 12.5, 5, 1.25); Add(163, "Ctrl", 13.75, 5, 1.25);
        Add(44, "PrtSc", 15.5, 0); Add(145, "Scroll", 16.5, 0); Add(19, "Pause", 17.5, 0);
        Add(45, "Ins", 15.5, 1); Add(36, "Home", 16.5, 1); Add(33, "PgUp", 17.5, 1);
        Add(46, "Del", 15.5, 2); Add(35, "End", 16.5, 2); Add(34, "PgDn", 17.5, 2);
        Add(38, "↑", 16.5, 4); Add(37, "←", 15.5, 5); Add(40, "↓", 16.5, 5); Add(39, "→", 17.5, 5);
        Add(144, "Num", 19, 1); Add(111, "/", 20, 1); Add(106, "×", 21, 1); Add(109, "−", 22, 1);
        for (var row = 0; row < 3; row++) for (var col = 0; col < 3; col++) { var digit = 7 - row * 3 + col; Add(96 + digit, digit.ToString(), 19 + col, 2 + row); }
        Add(107, "+", 22, 2, 1, 2); Add(13, "Enter", 22, 4, 1, 2); Add(96, "0", 19, 5, 2); Add(110, ".", 21, 5);
        return result;
    }
}

public sealed record KeyboardAssignment(Guid Id, string Name, string KeysText, string Scope, bool Enabled);
public sealed class InputModeViewModel : INotifyPropertyChanged
{
    private readonly MainViewModel owner;
    public bool IsMouse { get; }
    private IEnumerable<string> InputIds => IsMouse ? MouseInputs.All : KeyboardLayout.Keys.Select(k => k.Id).Distinct();
    private readonly HashSet<string> included = new(StringComparer.OrdinalIgnoreCase), excluded = new(StringComparer.OrdinalIgnoreCase);
    private bool all;
    private string lastProcess = "", lastToyId = "";
    private RuleEditor draft = new(new Rule());
    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action? SelectionChanged;
    public RuleEditor Draft => draft;
    public Guid? SelectedId { get; private set; }
    private bool CanEdit(Rule r) => IsMouse ? MousePresetStore.CanEdit(r) : KeyboardPresetStore.CanEdit(r);
    public IReadOnlyList<KeyboardAssignment> Groups => owner.Rules.Where(CanEdit).Select(r => new KeyboardAssignment(r.Id, r.Name,
        IsMouse ? (r.MouseLayer && r.Keys.Length == 0 ? L.T("All buttons and wheel") + (r.ExcludedKeys.Length == 0 ? "" : L.T(" · exclusions")) : string.Join(", ", (r.MouseLayer ? r : MousePresetStore.Normalize(r)).KeyList().Select(MouseLabel))) : string.IsNullOrWhiteSpace(r.Keys) ? L.F($"All keys{(r.ExcludedKeys.Length == 0 ? "" : L.T(" · with exclusions"))}") : L.F($"Keys: {r.KeyList().Length}"),
        (r.Process.Length == 0 ? L.T("Any application") : r.Process) + (r.WindowTitleContains.Length == 0 ? "" : L.T(" · window filter")), r.Enabled)).ToArray();
    public IReadOnlyList<Choice<string>> Applications => owner.ApplicationChoices.Concat(draft.Process.Length > 0 && !owner.ApplicationChoices.Any(c => c.Value == draft.Process) ? [new Choice<string>(draft.Process, draft.Process)] : Array.Empty<Choice<string>>()).ToArray();
    public IReadOnlyList<Toy> Targets => new[] { new Toy("", L.T("All connected"), true) }.Concat(owner.Toys)
        .Concat(draft.ToyId.Length > 0 && !owner.Toys.Any(t => t.Id == draft.ToyId) ? [new Toy(draft.ToyId, L.T("Unavailable toy from profile"), false)] : Array.Empty<Toy>()).ToArray();
    public IReadOnlyList<Choice<ActionKind>> Effects { get; } = [new(ActionKind.Vibrate, L.T("Vibration")), new(ActionKind.Pulse, L.T("Pulse")), new(ActionKind.Stop, L.T("Stop"))];
    public int SelectionCount => all ? InputIds.Count(IsSelected) : included.Count;
    public string SelectionText => all ? L.F($"{(IsMouse ? L.T("Whole mouse") : L.T("All keys"))} · excluded: {excluded.Count}") : L.F($"Selected {(IsMouse ? L.T("inputs") : L.T("keys"))}: {included.Count}");
    public string EditStatus => SelectedId is null ? L.T("New assignment") : L.T("Edit assignment");
    public string Summary => $"{SelectionText}. {(draft.Process.Length == 0 ? L.T("Any application") : L.T("In ") + draft.Process)}{(draft.WindowTitleContains.Length == 0 ? "" : L.T(", window contains «") + draft.WindowTitleContains + "»")} → " +
        EffectSummary;
    private static string MouseLabel(string key) => key.ToLowerInvariant() switch { "left" => L.T("Left"), "right" => L.T("Right"), "middle" => L.T("Wheel click"), "up" => L.T("Wheel ↑"), "down" => L.T("Wheel ↓"), _ => key };
    private string EffectSummary
    {
        get
        {
            var effect = draft.Action == ActionKind.Stop ? L.T("stop.") : L.F($"{(draft.Action == ActionKind.Pulse ? L.T("pulse") : L.T("feedback"))} {draft.Intensity}/20 for {draft.DurationText}.");
            if (!IsMouse || !draft.WheelContinuous || !MouseInputs.Wheel.Any(IsSelected)) return effect;
            var wheel = L.F($"Wheel vibration {draft.Intensity}/20 while scrolling; stops after {draft.WheelIdleMs} ms without scrolling.");
            return MouseInputs.Buttons.Any(IsSelected) ? effect + " " + wheel : wheel;
        }
    }
    public InputModeViewModel(MainViewModel owner, bool isMouse = false)
    {
        this.owner = owner; IsMouse = isMouse;
        owner.Rules.CollectionChanged += (_, _) =>
        {
            if (SelectedId is { } id && !owner.Rules.Any(r => r.Id == id)) New();
            Changed(nameof(Groups)); SelectionChanged?.Invoke();
        };
        owner.Toys.CollectionChanged += (_, _) => Changed(nameof(Targets));
        owner.PropertyChanged += (_, args) => { if (args.PropertyName == nameof(MainViewModel.ApplicationChoices)) Changed(nameof(Applications)); };
        if (Groups.FirstOrDefault() is { } first) Load(first.Id); else New();
    }
    private void Changed(string property) => PropertyChanged?.Invoke(this, new(property));
    private void DraftChanged(object? sender, PropertyChangedEventArgs e)
    {
        Changed(nameof(Summary));
        if (lastProcess != draft.Process) { lastProcess = draft.Process; Changed(nameof(Applications)); }
        if (lastToyId != draft.ToyId) { lastToyId = draft.ToyId; Changed(nameof(Targets)); }
    }
    private void SetDraft(Rule rule)
    {
        draft.PropertyChanged -= DraftChanged; draft = new(rule); draft.PropertyChanged += DraftChanged;
        lastProcess = draft.Process; lastToyId = draft.ToyId;
        Changed(nameof(Draft)); Changed(nameof(EditStatus)); Changed(nameof(SelectedId)); Changed(nameof(Applications)); Changed(nameof(Targets)); UpdateSelection();
    }
    public void New()
    {
        SelectedId = null; all = false; included.Clear(); excluded.Clear();
        SetDraft(new Rule { KeyboardLayer = !IsMouse, MouseLayer = IsMouse, Event = IsMouse ? EventKind.MouseDown : EventKind.KeyDown, Name = L.T("New assignment"), Keys = "", DurationSeconds = IsMouse ? 0.1 : 0.15, CooldownMs = IsMouse ? 0 : 200, Intensity = 5 });
    }
    public void Load(Guid id)
    {
        var original = owner.Rules.FirstOrDefault(r => CanEdit(r) && r.Id == id); if (original is null) return;
        var rule = IsMouse ? MousePresetStore.Normalize(original) : original.Copy(); if (!IsMouse) rule.KeyboardLayer = true;
        SelectedId = id; all = string.IsNullOrWhiteSpace(rule.Keys); included.Clear(); excluded.Clear();
        included.UnionWith(rule.KeyList()); excluded.UnionWith(rule.ExcludedKeys.Split([',', '+'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        SetDraft(rule);
    }
    public void Duplicate()
    {
        var rule = BuildRule(); rule.Id = Guid.NewGuid(); rule.Name += L.T(" · copy"); SelectedId = null; SetDraft(rule);
    }
    public bool IsSelected(string key) => all ? !excluded.Contains(key) : included.Contains(key);
    public bool IsExcluded(string key) => all && excluded.Contains(key);
    public bool HasAssignment(string key) => owner.Rules.Any(r => CanEdit(r) && r.Enabled && (IsMouse ? MousePresetStore.Normalize(r) : r).MatchesKeyboardKey(key));
    public void Toggle(string key)
    {
        var set = all ? excluded : included; if (!set.Add(key)) set.Remove(key); UpdateSelection();
    }
    public void Select(string group)
    {
        all = group == "all"; included.Clear(); excluded.Clear();
        if (!all) included.UnionWith(InputIds.Where(k => group switch
        {
            "buttons" => MouseInputs.Buttons.Contains(k), "wheel" => MouseInputs.Wheel.Contains(k),
            "letters" => k.Length == 1 && char.IsLetter(k[0]), "typing" => k.Length == 1 && char.IsLetter(k[0]) || k.StartsWith("D") && k.Length == 2 && char.IsDigit(k[1]) || k.StartsWith("Oem") || k is "Space" or "Return" or "Tab" or "Back",
            "wasd" => k is "W" or "A" or "S" or "D", "arrows" => k is "Up" or "Down" or "Left" or "Right", "numpad" => k.StartsWith("NumPad") || k is "Add" or "Subtract" or "Multiply" or "Divide" or "Decimal", _ => false
        }));
        UpdateSelection();
    }
    private void UpdateSelection() { Changed(nameof(SelectionCount)); Changed(nameof(SelectionText)); Changed(nameof(Summary)); SelectionChanged?.Invoke(); }
    public void Preset(string name)
    {
        if (IsMouse)
        {
            draft.WheelContinuous = name == "scroll"; draft.Action = name == "pulse" ? ActionKind.Pulse : ActionKind.Vibrate;
            draft.DurationSeconds = name == "pulse" ? 2 : 0.1; draft.CooldownMs = name == "pulse" ? 2200 : 0; draft.Intensity = 5; draft.WheelIdleMs = 150; draft.PulseMs = 250;
            return;
        }
        draft.Action = name == "pulse" ? ActionKind.Pulse : ActionKind.Vibrate;
        draft.Intensity = name == "typewriter" ? 8 : 5;
        draft.DurationSeconds = name == "pulse" ? 2 : name == "typewriter" ? 0.12 : 0.15;
        draft.CooldownMs = name == "pulse" ? 2200 : 200;
        draft.PulseMs = 250;
    }
    public Rule BuildRule()
    {
        var rule = draft.Copy(); rule.Keys = all ? "" : string.Join(",", included.OrderBy(k => k, StringComparer.Ordinal));
        rule.ExcludedKeys = all ? string.Join(",", excluded.OrderBy(k => k, StringComparer.Ordinal)) : "";
        return rule;
    }
    public bool Save()
    {
        if (SelectionCount == 0) { owner.Log(IsMouse ? L.T("Select a button or wheel direction on the diagram.") : L.T("Select at least one key on the layout.")); return false; }
        var rule = BuildRule();
        if (rule.Validate() is { } error) { owner.Log(error); return false; }
        if (IsMouse) owner.SaveMouseRule(rule); else owner.SaveKeyboardRule(rule); SelectedId = rule.Id; Changed(nameof(SelectedId)); Changed(nameof(EditStatus)); return true;
    }
    public async Task TestAsync()
    {
        if (SelectionCount == 0) { owner.Log(L.T("Select inputs for this assignment.")); return; }
        var rule = BuildRule(); if (rule.Validate() is { } error) { owner.Log(error); return; }
        await owner.FireAsync(new(rule, 1, IsMouse && rule.WheelContinuous && MouseInputs.Wheel.Any(IsSelected)), false);
    }
    public void Delete() { if (SelectedId is { } id) { if (IsMouse) owner.RemoveMouseRule(id); else owner.RemoveKeyboardRule(id); } New(); }
    public void RefreshApplications() { owner.RefreshApplications(); Changed(nameof(Applications)); }
    public void ExportFile(string path, bool selectedOnly)
    {
        var rules = owner.Rules.Where(CanEdit).Where(r => !selectedOnly || r.Id == SelectedId);
        if (IsMouse) MousePresetStore.Save(path, rules, MouseInputs.All); else KeyboardPresetStore.Save(path, rules, InputIds);
        owner.Log(L.F($"Saved assignments {(IsMouse ? L.T("mouse") : L.T("keyboard"))} exported. Unsaved draft changes are not included."));
    }
    public async Task ImportFileAsync(string path)
    {
        if (IsMouse) { var mousePreset = MousePresetStore.Load(path, MouseInputs.All); await owner.ImportMouseAsync(mousePreset); Load(mousePreset.Rules[0].Id); return; }
        var preset = KeyboardPresetStore.Load(path, KeyboardLayout.Keys.Select(k => k.Id));
        await owner.ImportKeyboardAsync(preset);
        Load(preset.Rules[0].Id);
    }
    public void Report(string message) => owner.Log(message);
}
