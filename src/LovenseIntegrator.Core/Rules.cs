namespace LovenseIntegrator.Core;

public enum EventKind
{
    KeyDown, KeyUp, KeyHeld, DoublePress, Chord, Sequence, PressCount,
    TypingRateAbove, TypingRateBelow, InputIdle, MouseDown, MouseUp,
    MouseWheel, ForegroundChanged, Timer,
    KeyReleasedAfterHold, TriplePress, CleanTypingStreak, ActivityResumed, MouseDoubleClick, MouseHeld
}
public enum ActionKind { Vibrate, Pulse, RateMapped, Stop }

public sealed class Rule
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "New rule";
    public bool Enabled { get; set; } = true;
    public EventKind Event { get; set; } = EventKind.KeyDown;
    public string Keys { get; set; } = "Space";
    public double Threshold { get; set; } = 800;
    public int WindowMs { get; set; } = 2000;
    public int CooldownMs { get; set; } = 1000;
    public string Process { get; set; } = "";
    public int Priority { get; set; } = 0;
    public ActionKind Action { get; set; } = ActionKind.Vibrate;
    public int Intensity { get; set; } = 7;
    public double DurationSeconds { get; set; } = 3;
    public int PulseMs { get; set; } = 500;
    public string ToyId { get; set; } = "";
    public bool KeyboardLayer { get; set; }
    public bool MouseLayer { get; set; }
    public bool WheelContinuous { get; set; }
    public int WheelIdleMs { get; set; } = 150;
    public string ExcludedKeys { get; set; } = "";
    public string WindowTitleContains { get; set; } = "";

    public Rule Copy() => (Rule)MemberwiseClone();
    public string? Validate()
    {
        if (Keys is null || Process is null || ToyId is null || ExcludedKeys is null || WindowTitleContains is null) return "Keys, application and toy fields cannot be null.";
        if (KeyboardLayer && (Event != EventKind.KeyDown || Action == ActionKind.RateMapped)) return "Keyboard assignments trigger on key press: vibration, pulse or stop.";
        if (MouseLayer && (KeyboardLayer || Event != EventKind.MouseDown || Action == ActionKind.RateMapped)) return "Mouse assignment: MouseLayer=true, KeyboardLayer=false, Event=MouseDown, action Vibrate/Pulse/Stop.";
        if (WheelContinuous && (!MouseLayer || Action != ActionKind.Vibrate)) return "While scrolling is available for vibration in mouse mode.";
        if (MouseLayer && WheelIdleMs is < 100 or > 1000) return "Scroll idle timeout: 100–1000 ms.";
        if (string.IsNullOrWhiteSpace(Name)) return "Enter a rule name.";
        if (!Enum.IsDefined(Event) || !Enum.IsDefined(Action)) return "Unknown event or action type.";
        if (!double.IsFinite(Threshold) || Threshold <= 0) return "Threshold must be greater than zero.";
        if (WindowMs is < 100 or > 60000) return "Measurement window: 100–60000 ms.";
        if (CooldownMs < (MouseLayer ? 0 : 100) || CooldownMs > 3600000) return MouseLayer ? "Mouse cooldown: 0–3600000 ms." : "Cooldown: 100–3600000 ms.";
        if (Intensity is < 0 or > 20) return "Intensity: 0–20.";
        if (!double.IsFinite(DurationSeconds) || DurationSeconds is < 0.1 or > 300) return "Duration: 0.1–300 seconds.";
        if (PulseMs is < 150 or > 10000) return "Pulse interval: 150–10000 ms.";
        if (Event is EventKind.Chord or EventKind.Sequence && KeyList().Length < 2)
            return "Enter at least two keys separated by + (combination) or comma (sequence).";
        if (Event is EventKind.DoublePress or EventKind.TriplePress or EventKind.MouseDoubleClick && WindowMs < Threshold)
            return "The measurement window must cover the repeated-press interval.";
        return null;
    }
    public string[] KeyList() => Keys.Split(['+', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    public bool MatchesKeyboardKey(string key) => (string.IsNullOrWhiteSpace(Keys) || KeyList().Contains(key, StringComparer.OrdinalIgnoreCase)) &&
        !ExcludedKeys.Split(['+', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Contains(key, StringComparer.OrdinalIgnoreCase);
    public bool MatchesWindow(InputEvent input) => (string.IsNullOrWhiteSpace(Process) ||
        string.Equals(Process.Trim().Replace(".exe", "", StringComparison.OrdinalIgnoreCase), input.Process, StringComparison.OrdinalIgnoreCase)) &&
        (string.IsNullOrWhiteSpace(WindowTitleContains) || input.WindowTitle.Contains(WindowTitleContains.Trim(), StringComparison.OrdinalIgnoreCase));
}

public sealed record InputEvent(EventKind Kind, long AtMs, string Key = "", string Process = "", double Value = 0, string WindowTitle = "");
public sealed record RuleMatch(Rule Rule, double Value, bool RenewWheel = false)
{
    public ToyCommand Command()
    {
        var intensity = Rule.Action == ActionKind.RateMapped ? (int)Math.Clamp(Math.Round(Value / Rule.Threshold * Rule.Intensity), 0, 20) : Rule.Intensity;
        return new(Rule.Action, intensity, RenewWheel ? Rule.WheelIdleMs / 1000.0 : Rule.DurationSeconds, Rule.PulseMs, Rule.ToyId, RenewWheel ? Rule.Id.ToString() : "");
    }
}

// All calls are serialized by the desktop dispatcher. Time is monotonic, never wall-clock time.
public sealed class RuleEngine
{
    private readonly Dictionary<string, long> held = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, long> mouseHeld = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<(long At, string Key)> presses = [];
    private readonly List<(long At, string Key)> clicks = [];
    private readonly Dictionary<Guid, long> lastFired = [];
    private readonly HashSet<Guid> latched = [];
    private long lastInput;
    private long started;
    private int cleanStreak;
    public IReadOnlyCollection<string> HeldKeys => held.Keys;

    public void Reset(long now)
    {
        held.Clear(); mouseHeld.Clear(); presses.Clear(); clicks.Clear(); lastFired.Clear(); latched.Clear(); cleanStreak = 0;
        lastInput = started = now;
    }

    public RuleMatch? Process(InputEvent input, IEnumerable<Rule> rules)
    {
        if (input.Kind is EventKind.KeyDown)
        {
            // Windows autorepeat is not a new physical press.
            if (!held.TryAdd(input.Key, input.AtMs)) return null;
            presses.Add((input.AtMs, input.Key));
            if (input.Key.Equals("Back", StringComparison.OrdinalIgnoreCase) || input.Key.Equals("Delete", StringComparison.OrdinalIgnoreCase)) cleanStreak = 0;
            else if (IsTypingKey(input.Key)) cleanStreak++;
        }
        if (input.Kind == EventKind.MouseDown)
        {
            if (!mouseHeld.TryAdd(input.Key, input.AtMs)) return null;
            clicks.Add((input.AtMs, input.Key));
        }
        presses.RemoveAll(p => input.AtMs - p.At > 60000);
        clicks.RemoveAll(p => input.AtMs - p.At > 60000);
        var idleGap = input.AtMs - lastInput;
        var releaseDuration = input.Kind == EventKind.KeyUp && held.TryGetValue(input.Key, out var downAt) ? input.AtMs - downAt : -1;
        if (input.Kind is EventKind.KeyDown or EventKind.KeyUp or EventKind.MouseDown or EventKind.MouseUp or EventKind.MouseWheel)
            lastInput = input.AtMs;
        if (input.Kind == EventKind.KeyUp) held.Remove(input.Key);
        if (input.Kind == EventKind.MouseUp) mouseHeld.Remove(input.Key);

        RuleMatch? winner = null;
        // Resolve keyboard overrides BEFORE cooldown, so a cooling override cannot leak a base effect.
        var keyboardLayer = input.Kind == EventKind.KeyDown ? rules.Where(r => r.Enabled && r.KeyboardLayer && r.MatchesWindow(input) && r.MatchesKeyboardKey(input.Key))
            .OrderByDescending(r => r.Priority)
            .ThenByDescending(r => !string.IsNullOrWhiteSpace(r.Process))
            .ThenByDescending(r => !string.IsNullOrWhiteSpace(r.WindowTitleContains))
            .ThenBy(r => string.IsNullOrWhiteSpace(r.Keys) ? int.MaxValue : r.KeyList().Length)
            .ThenBy(r => r.Id).FirstOrDefault() : null;
        var mouseLayer = input.Kind is EventKind.MouseDown or EventKind.MouseWheel ? rules.Where(r => r.Enabled && r.MouseLayer && r.MatchesWindow(input) && r.MatchesKeyboardKey(input.Key))
            .OrderByDescending(r => r.Priority).ThenByDescending(r => !string.IsNullOrWhiteSpace(r.Process))
            .ThenByDescending(r => !string.IsNullOrWhiteSpace(r.WindowTitleContains))
            .ThenBy(r => string.IsNullOrWhiteSpace(r.Keys) ? int.MaxValue : r.KeyList().Length).ThenBy(r => r.Id).FirstOrDefault() : null;
        foreach (var rule in rules.Where(r => r.Enabled).OrderByDescending(r => r.Priority).ThenBy(r => r.Id))
        {
            if (rule.MouseLayer && rule.Id != mouseLayer?.Id) continue;
            if (mouseLayer is not null && !rule.MouseLayer && rule.Event == input.Kind) continue;
            if (rule.KeyboardLayer && rule.Id != keyboardLayer?.Id) continue;
            if (keyboardLayer is not null && rule.Event == EventKind.KeyDown && rule.Id != keyboardLayer.Id) continue;
            var processMatches = rule.MatchesWindow(input);
            var (matched, value, continuous) = Evaluate(rule, input, idleGap, releaseDuration);
            if (!matched || !processMatches) { latched.Remove(rule.Id); continue; }
            if (continuous && latched.Contains(rule.Id)) continue;
            var renewWheel = rule.MouseLayer && rule.WheelContinuous && input.Kind == EventKind.MouseWheel;
            if (!renewWheel && input.AtMs - lastFired.GetValueOrDefault(rule.Id, started - rule.CooldownMs) < rule.CooldownMs) continue;
            // Consume losing matches too; held/rate events must not fire late after a competing rule.
            if (continuous) latched.Add(rule.Id);
            lastFired[rule.Id] = input.AtMs;
            winner ??= new(rule.Copy(), value, renewWheel);
        }
        return winner;
    }

    private static bool IsTypingKey(string key) => key.Length == 1 && char.IsLetter(key[0]) ||
        key.Length == 2 && key[0] == 'D' && char.IsDigit(key[1]) ||
        key.StartsWith("NumPad", StringComparison.OrdinalIgnoreCase) || key.StartsWith("Oem", StringComparison.OrdinalIgnoreCase) ||
        key is "Space" or "Return" or "Enter" or "Tab" or "Decimal" or "Add" or "Subtract" or "Multiply" or "Divide";

    private (bool Match, double Value, bool Continuous) Evaluate(Rule r, InputEvent e, long idleGap, long releaseDuration)
    {
        if (r.MouseLayer) return (e.Kind is EventKind.MouseDown or EventKind.MouseWheel && r.MatchesKeyboardKey(e.Key), e.Value, false);
        var any = string.IsNullOrWhiteSpace(r.Keys);
        var key = r.KeyboardLayer ? r.MatchesKeyboardKey(e.Key) : any || string.Equals(r.Keys.Trim(), e.Key, StringComparison.OrdinalIgnoreCase);
        var recent = presses.Where(p => e.AtMs - p.At <= r.WindowMs).ToArray();
        var count = recent.Count(p => any || string.Equals(p.Key, r.Keys.Trim(), StringComparison.OrdinalIgnoreCase));
        var rate = recent.Length * 1000.0 / r.WindowMs;
        switch (r.Event)
        {
            case EventKind.KeyDown: case EventKind.KeyUp:
            case EventKind.MouseDown: case EventKind.MouseUp: case EventKind.MouseWheel:
                return (e.Kind == r.Event && key, e.Value, false);
            case EventKind.KeyHeld:
                var duration = held.Where(p => any || string.Equals(p.Key, r.Keys.Trim(), StringComparison.OrdinalIgnoreCase))
                    .Select(p => (double)(e.AtMs - p.Value)).DefaultIfEmpty(0).Max();
                return (duration >= r.Threshold, duration, true);
            case EventKind.DoublePress: case EventKind.TriplePress:
                var required = r.Event == EventKind.DoublePress ? 2 : 3;
                var pair = recent.Where(p => string.Equals(p.Key, e.Key, StringComparison.OrdinalIgnoreCase)).TakeLast(required).ToArray();
                return (e.Kind == EventKind.KeyDown && key && pair.Length == required && pair[^1].At - pair[0].At <= r.Threshold, pair.Length, false);
            case EventKind.KeyReleasedAfterHold:
                return (e.Kind == EventKind.KeyUp && key && releaseDuration >= r.Threshold, releaseDuration, false);
            case EventKind.CleanTypingStreak:
                return (cleanStreak >= r.Threshold, cleanStreak, true);
            case EventKind.ActivityResumed:
                return (e.Kind is EventKind.KeyDown or EventKind.MouseDown or EventKind.MouseWheel && idleGap >= r.Threshold, idleGap, false);
            case EventKind.MouseDoubleClick:
                var doubleClick = clicks.Where(p => e.AtMs - p.At <= r.WindowMs && string.Equals(p.Key, e.Key, StringComparison.OrdinalIgnoreCase)).TakeLast(2).ToArray();
                return (e.Kind == EventKind.MouseDown && key && doubleClick.Length == 2 && doubleClick[1].At - doubleClick[0].At <= r.Threshold, 2, false);
            case EventKind.MouseHeld:
                var mouseDuration = mouseHeld.Where(p => any || string.Equals(p.Key, r.Keys.Trim(), StringComparison.OrdinalIgnoreCase))
                    .Select(p => (double)(e.AtMs - p.Value)).DefaultIfEmpty(0).Max();
                return (mouseDuration >= r.Threshold, mouseDuration, true);
            case EventKind.Chord:
                var keys = r.KeyList();
                return (keys.Length >= 2 && keys.All(held.ContainsKey), keys.Length, true);
            case EventKind.Sequence:
                var seq = r.KeyList();
                return (e.Kind == EventKind.KeyDown && seq.Length >= 2 && recent.Length >= seq.Length &&
                    recent.TakeLast(seq.Length).Select(p => p.Key).SequenceEqual(seq, StringComparer.OrdinalIgnoreCase), seq.Length, false);
            case EventKind.PressCount:
                return (e.Kind == EventKind.KeyDown && key && count >= r.Threshold, count, false);
            case EventKind.TypingRateAbove: return (rate >= r.Threshold, rate, true);
            case EventKind.TypingRateBelow: return (e.AtMs - started >= r.WindowMs && rate < r.Threshold, rate, true);
            case EventKind.InputIdle: return (e.AtMs - lastInput >= r.Threshold, e.AtMs - lastInput, true);
            case EventKind.ForegroundChanged: return (e.Kind == r.Event, 0, false);
            case EventKind.Timer:
                return (e.Kind == EventKind.Timer && e.AtMs - lastFired.GetValueOrDefault(r.Id, started) >= r.Threshold, 0, false);
            default: return (false, 0, false);
        }
    }
}
