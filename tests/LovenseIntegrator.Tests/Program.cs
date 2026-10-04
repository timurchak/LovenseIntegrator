using System.Net;
using System.Text;
using System.Text.Json;
using LovenseIntegrator.Core;
using LovenseIntegrator.Desktop.Services;
using LovenseIntegrator.Desktop.Transports;

var passed = 0;
void Check(bool condition, string description)
{
    if (!condition) throw new Exception("FAIL: " + description);
    Console.WriteLine("PASS: " + description); passed++;
}
(RuleEngine Engine, Rule Rule) Scenario(EventKind kind, string keys = "A", double threshold = 500)
{
    var engine = new RuleEngine(); engine.Reset(0);
    return (engine, new() { Event = kind, Keys = keys, Threshold = threshold, WindowMs = 2000, CooldownMs = 100 });
}
RuleMatch? Step(RuleEngine e, Rule r, EventKind kind, long time, string key = "A", string process = "") => e.Process(new(kind, time, key, process), [r]);

var (engine, rule) = Scenario(EventKind.KeyDown);
Check(Step(engine, rule, EventKind.KeyDown, 10) is not null, "physical key down fires");
Check(Step(engine, rule, EventKind.KeyDown, 500) is null, "OS autorepeat suppressed");
Step(engine, rule, EventKind.KeyUp, 600);
Check(Step(engine, rule, EventKind.KeyDown, 700) is not null, "release allows next physical press");
(engine, rule) = Scenario(EventKind.KeyUp);
Step(engine, rule, EventKind.KeyDown, 10);
Check(Step(engine, rule, EventKind.KeyUp, 20) is not null && engine.HeldKeys.Count == 0, "key up fires and clears held state");
(engine, rule) = Scenario(EventKind.KeyHeld);
Step(engine, rule, EventKind.KeyDown, 10);
Check(Step(engine, rule, EventKind.Timer, 509) is null, "hold does not fire early");
Check(Step(engine, rule, EventKind.Timer, 510) is not null, "hold fires at boundary");
Check(Step(engine, rule, EventKind.Timer, 900) is null, "hold fires only once until release");
Step(engine, rule, EventKind.KeyUp, 1000); Step(engine, rule, EventKind.KeyDown, 1100);
Check(Step(engine, rule, EventKind.Timer, 1600) is not null, "hold rearms after release");
(engine, rule) = Scenario(EventKind.DoublePress);
Step(engine, rule, EventKind.KeyDown, 0); Step(engine, rule, EventKind.KeyUp, 20);
Check(Step(engine, rule, EventKind.KeyDown, 500) is not null, "double press includes time boundary");
(engine, rule) = Scenario(EventKind.DoublePress);
Step(engine, rule, EventKind.KeyDown, 10); Step(engine, rule, EventKind.KeyUp, 20);
Check(Step(engine, rule, EventKind.KeyDown, 511) is null, "slow double press rejected");
(engine, rule) = Scenario(EventKind.DoublePress);
Step(engine, rule, EventKind.KeyDown, 0); Step(engine, rule, EventKind.KeyUp, 0);
Check(Step(engine, rule, EventKind.KeyDown, 0) is not null, "same millisecond double press does not throw");
(engine, rule) = Scenario(EventKind.Chord, "LeftCtrl+Space");
Step(engine, rule, EventKind.KeyDown, 0, "LeftCtrl");
Check(Step(engine, rule, EventKind.KeyDown, 10, "Space") is not null, "chord fires when final key pressed");
Check(Step(engine, rule, EventKind.Timer, 500) is null, "chord stays latched");
Step(engine, rule, EventKind.KeyUp, 600, "Space");
Check(Step(engine, rule, EventKind.KeyDown, 700, "Space") is not null, "chord rearms");
(engine, rule) = Scenario(EventKind.Sequence, "A,B,C");
Step(engine, rule, EventKind.KeyDown, 0, "A"); Step(engine, rule, EventKind.KeyDown, 100, "B");
Check(Step(engine, rule, EventKind.KeyDown, 200, "C") is not null, "ordered sequence fires");
(engine, rule) = Scenario(EventKind.Sequence, "A,B,C");
Step(engine, rule, EventKind.KeyDown, 0, "A"); Step(engine, rule, EventKind.KeyDown, 100, "B");
Check(Step(engine, rule, EventKind.KeyDown, 2100, "C") is null, "sequence expires outside window");
(engine, rule) = Scenario(EventKind.PressCount, "", 3);
Step(engine, rule, EventKind.KeyDown, 0, "A"); Step(engine, rule, EventKind.KeyDown, 100, "B");
Check(Step(engine, rule, EventKind.KeyDown, 200, "C") is not null, "any-key count counts physical presses");
(engine, rule) = Scenario(EventKind.TypingRateAbove, "", 1);
Step(engine, rule, EventKind.KeyDown, 0, "A");
Check(Step(engine, rule, EventKind.KeyDown, 100, "B") is { Value: 1 }, "rate uses configured rolling window");
Check(Step(engine, rule, EventKind.Timer, 1000) is null, "rate fires only on threshold crossing");
Step(engine, rule, EventKind.Timer, 2200); Step(engine, rule, EventKind.KeyUp, 2300, "A"); Step(engine, rule, EventKind.KeyUp, 2300, "B");
Step(engine, rule, EventKind.KeyDown, 2400, "A");
Check(Step(engine, rule, EventKind.KeyDown, 2500, "B") is not null, "rate rearms after falling below threshold");
(engine, rule) = Scenario(EventKind.TypingRateBelow, "", 1);
Check(Step(engine, rule, EventKind.Timer, 1000) is null && Step(engine, rule, EventKind.Timer, 2000) is not null, "low-rate waits for full initial window");
(engine, rule) = Scenario(EventKind.InputIdle);
Step(engine, rule, EventKind.MouseDown, 400, "Left");
Check(Step(engine, rule, EventKind.Timer, 899) is null && Step(engine, rule, EventKind.Timer, 900) is not null, "mouse input resets idle timer");
(engine, rule) = Scenario(EventKind.Timer);
Check(Step(engine, rule, EventKind.Timer, 499) is null && Step(engine, rule, EventKind.Timer, 500) is not null && Step(engine, rule, EventKind.Timer, 999) is null && Step(engine, rule, EventKind.Timer, 1000) is not null, "timer repeats with configured interval");
foreach (var kind in new[] { EventKind.MouseDown, EventKind.MouseUp, EventKind.MouseWheel, EventKind.ForegroundChanged })
{
    (engine, rule) = Scenario(kind, "");
    Check(Step(engine, rule, kind, 10, "Left") is not null, kind + " routes correctly");
}
(engine, rule) = Scenario(EventKind.KeyDown);
rule.Process = "notepad.exe";
Check(Step(engine, rule, EventKind.KeyDown, 0, process: "chrome") is null, "foreground condition rejects wrong application");
Step(engine, rule, EventKind.KeyUp, 20);
Check(Step(engine, rule, EventKind.KeyDown, 100, process: "notepad") is not null, "foreground condition accepts process name");
(engine, rule) = Scenario(EventKind.KeyDown);
rule.CooldownMs = 1000; Step(engine, rule, EventKind.KeyDown, 0); Step(engine, rule, EventKind.KeyUp, 20);
Check(Step(engine, rule, EventKind.KeyDown, 500) is null, "cooldown rejects burst");
engine.Reset(600);
Check(Step(engine, rule, EventKind.KeyDown, 600) is not null, "pause resets held keys and cooldowns");
(engine, rule) = Scenario(EventKind.KeyDown);
var higher = rule.Copy(); higher.Id = Guid.NewGuid(); higher.Priority = 10;
Check(engine.Process(new(EventKind.KeyDown, 0, "A"), [rule, higher])?.Rule.Id == higher.Id, "highest priority wins conflicting event");
(engine, rule) = Scenario(EventKind.KeyDown); rule.Enabled = false;
Check(Step(engine, rule, EventKind.KeyDown, 0) is null, "disabled rule never fires");
Check(new Rule { DurationSeconds = double.NaN }.Validate() is not null && new Rule { Intensity = 21 }.Validate() is not null, "invalid numeric settings rejected");

(engine, rule) = Scenario(EventKind.KeyReleasedAfterHold);
Step(engine, rule, EventKind.KeyDown, 10);
Check(Step(engine, rule, EventKind.KeyUp, 509) is null, "release after short hold rejected");
Step(engine, rule, EventKind.KeyDown, 600);
Check(Step(engine, rule, EventKind.KeyUp, 1100) is { Value: 500 }, "release after hold fires at boundary with measured duration");
Check(Step(engine, rule, EventKind.KeyUp, 1300) is null, "unmatched release cannot trigger held-release rule");
(engine, rule) = Scenario(EventKind.TriplePress, "A", 600);
Step(engine, rule, EventKind.KeyDown, 0); Step(engine, rule, EventKind.KeyUp, 10);
Check(Step(engine, rule, EventKind.KeyDown, 300) is null, "triple press waits for third physical press");
Step(engine, rule, EventKind.KeyUp, 310);
Check(Step(engine, rule, EventKind.KeyDown, 600) is not null, "triple press includes total interval boundary");
(engine, rule) = Scenario(EventKind.TriplePress, "A", 600);
Step(engine, rule, EventKind.KeyDown, 0); Step(engine, rule, EventKind.KeyUp, 10); Step(engine, rule, EventKind.KeyDown, 100); Step(engine, rule, EventKind.KeyUp, 110);
Check(Step(engine, rule, EventKind.KeyDown, 601) is null, "slow triple press rejected");
(engine, rule) = Scenario(EventKind.CleanTypingStreak, "", 3);
Step(engine, rule, EventKind.KeyDown, 0, "LeftShift"); Step(engine, rule, EventKind.KeyDown, 10, "A"); Step(engine, rule, EventKind.KeyDown, 20, "B");
Check(Step(engine, rule, EventKind.Timer, 30) is null, "clean streak excludes modifiers");
Check(Step(engine, rule, EventKind.KeyDown, 40, "C") is { Value: 3 }, "clean streak fires at configured count");
Check(Step(engine, rule, EventKind.KeyDown, 150, "D") is null, "clean streak stays latched until correction");
Step(engine, rule, EventKind.KeyDown, 160, "Back"); Step(engine, rule, EventKind.KeyDown, 200, "E"); Step(engine, rule, EventKind.KeyDown, 210, "F");
Check(Step(engine, rule, EventKind.KeyDown, 220, "G") is not null, "Backspace resets clean streak");
engine.Reset(300);
Check(Step(engine, rule, EventKind.KeyDown, 310, "A") is null, "pause resets clean streak");
(engine, rule) = Scenario(EventKind.ActivityResumed, "", 500);
Check(Step(engine, rule, EventKind.Timer, 500) is null, "resume event does not fire on idle timer");
Check(Step(engine, rule, EventKind.KeyDown, 500) is { Value: 500 }, "first input after idle fires with idle duration");
Step(engine, rule, EventKind.KeyUp, 510);
Check(Step(engine, rule, EventKind.MouseDown, 1010, "Left") is not null, "mouse button resumes activity");
Check(Step(engine, rule, EventKind.KeyDown, 1100, "B") is null, "subsequent active input does not count as resume");
(engine, rule) = Scenario(EventKind.MouseHeld, "Left", 500);
Step(engine, rule, EventKind.MouseDown, 10, "Left");
Check(Step(engine, rule, EventKind.Timer, 509) is null && Step(engine, rule, EventKind.Timer, 510) is not null, "mouse hold timing boundary");
Check(Step(engine, rule, EventKind.Timer, 700) is null, "mouse hold is latched");
Step(engine, rule, EventKind.MouseUp, 800, "Left"); Step(engine, rule, EventKind.MouseDown, 900, "Left");
Check(Step(engine, rule, EventKind.Timer, 1400) is not null, "mouse hold rearms on release");
(engine, rule) = Scenario(EventKind.MouseDoubleClick, "Left", 400);
Step(engine, rule, EventKind.MouseDown, 10, "Left"); Step(engine, rule, EventKind.MouseUp, 20, "Left");
Step(engine, rule, EventKind.MouseDown, 30, "Right"); Step(engine, rule, EventKind.MouseUp, 40, "Right");
Check(Step(engine, rule, EventKind.MouseDown, 410, "Left") is not null, "double click tracks selected button independently");
Check(Step(engine, rule, EventKind.MouseDown, 700, "Left") is null, "duplicate mouse-down without release ignored");
Check(new Rule { Event = EventKind.TriplePress, Threshold = 3000, WindowMs = 2000 }.Validate() is not null, "repeat interval cannot exceed history window");

var recording = new KeyRecording(EventKind.Chord);
Check(!recording.Down("LeftCtrl").Complete && !recording.Down("Space").Complete, "chord recorder waits for release");
Check(!recording.Up("Space").Complete && recording.Up("LeftCtrl") is { Complete: true, Keys: "LeftCtrl+Space" }, "chord recording preserves order and waits for all keys");
recording = new(EventKind.Chord);
recording.Down("LeftCtrl");
Check(recording.Up("LeftCtrl").Cancelled, "one-key chord recording is cancelled");
recording = new(EventKind.Sequence);
recording.Down("A"); recording.Down("A", repeat: true); recording.Up("A"); recording.Down("B"); recording.Up("B");
Check(recording.Down("Return") is { Complete: true, Keys: "A,B" }, "sequence recorder ignores repeats and Enter completes");
recording = new(EventKind.Sequence);
recording.Down("A"); recording.Up("A");
Check(!recording.Down("Return").Complete && recording.Down("Escape").Cancelled, "short sequence waits and Escape cancels");
Check(new KeyRecording(EventKind.KeyDown).Down("Escape") is { Complete: true, Keys: "Escape" }, "Escape remains recordable for single-key events");

var handler = new FakeHandler();
foreach (var duration in new[] { 0.1, 0.15, 0.25, 1.5, 300.0 })
{
    new ToyCommand(ActionKind.Vibrate, 5, duration).Validate();
    Check(new Rule { DurationSeconds = duration }.Validate() is null, $"rule and command accept {duration} second effect");
}
foreach (var duration in new[] { 0, 0.099, -1, double.NaN, double.PositiveInfinity, 300.01 })
{
    var invalid = false;
    try { new ToyCommand(ActionKind.Vibrate, 5, duration).Validate(); } catch (ArgumentException) { invalid = true; }
    Check(invalid && new Rule { DurationSeconds = duration }.Validate() is not null, $"rule and command reject invalid duration {duration}");
}
(engine, rule) = Scenario(EventKind.KeyDown, ""); rule.DurationSeconds = 0.15; rule.CooldownMs = 200;
Check(Step(engine, rule, EventKind.KeyDown, 0, "A") is not null, "haptic rule fires on first key");
Check(Step(engine, rule, EventKind.KeyDown, 20, "A") is null, "haptic ignores OS autorepeat");
Step(engine, rule, EventKind.KeyUp, 25, "A");
Check(Step(engine, rule, EventKind.KeyDown, 100, "B") is null, "haptic cooldown suppresses burst without backlog");
Check(Step(engine, rule, EventKind.KeyDown, 200, "C") is not null, "haptic reopens at 200 ms boundary");
var levels = new List<(string Id, int Level)>(); var waits = new List<(int Milliseconds, TaskCompletionSource Completion)>();
var changed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
await using (var replay = new BleTransport([new("lush", "Lush replay", true), new("ferri", "Ferri replay", true)],
    (id, level) => { levels.Add((id, level)); changed.TrySetResult(); },
    (ms, ct) => { var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); waits.Add((ms, completion)); return completion.Task.WaitAsync(ct); }))
{
    await replay.SendAsync(new(ActionKind.Vibrate, 5, 0.15, ToyId: "lush"), CancellationToken.None);
    Check(levels.SequenceEqual(new[] { ("lush", 5) }) && waits[0].Milliseconds == 150, "BLE starts selected toy immediately and schedules exactly 150 ms");
    changed = new(TaskCreationOptions.RunContinuationsAsynchronously); waits[0].Completion.SetResult();
    await changed.Task.WaitAsync(TimeSpan.FromSeconds(2));
    Check(levels.Last() == ("lush", 0), "BLE stops short impulse after scheduled duration");
    await replay.SendAsync(new(ActionKind.Vibrate, 5, 0.1, ToyId: "lush"), CancellationToken.None);
    var replaced = waits[^1];
    await replay.SendAsync(new(ActionKind.Vibrate, 9, 0.25, ToyId: "lush"), CancellationToken.None);
    var current = waits[^1]; var count = levels.Count;
    replaced.Completion.SetResult(); await Task.Yield();
    Check(levels.Count == count && levels.Last() == ("lush", 9), "cancelled old impulse cannot stop replacement");
    await replay.SendAsync(ToyCommand.Stop, CancellationToken.None);
    count = levels.Count; current.Completion.SetResult(); await Task.Yield();
    Check(levels.Count == count && levels.TakeLast(2).All(p => p.Level == 0), "emergency Stop cancels pending short effect for all toys");
}
await using var api = new LocalApiTransport("https://127-0-0-1.lovense.club:30010/command", handler);
handler.Response = """{"code":200,"data":{"toys":"{\"abc\":{\"name\":\"lush\",\"status\":\"1\",\"battery\":86}}"}}""";
var toys = await api.DiscoverAsync(CancellationToken.None);
Check(toys is [{ Name: "lush", Connected: true, Battery: 86 }], "API parses string-encoded toy dictionary");
Check(handler.Platform == "LovenseIntegrator", "API includes required platform header");
handler.Response = """{"code":200,"data":{"toys":{"def":{"name":"ferri","status":0,"battery":"72"}}}}""";
Check((await api.DiscoverAsync(CancellationToken.None))[0] is { Name: "ferri", Connected: false, Battery: 72 }, "API accepts object toy dictionary and disconnected status");
handler.Response = """{"code":200} """;
var untouchedBody = handler.Body;
foreach (var duration in new[] { 0.1, 0.15, 1.0 })
{
    var shortRejected = false;
    try { await api.SendAsync(new(ActionKind.Vibrate, 5, duration), CancellationToken.None); } catch (ArgumentException) { shortRejected = true; }
    Check(shortRejected && handler.Body == untouchedBody, $"REST rejects {duration} seconds before sending");
}
await api.SendAsync(new(ActionKind.Vibrate, 5, 1.5), CancellationToken.None);
using (var sent = JsonDocument.Parse(handler.Body)) Check(sent.RootElement.GetProperty("timeSec").GetDouble() == 1.5, "REST preserves allowed fractional duration above one second");
await api.SendAsync(new(ActionKind.Vibrate, 7, 3, ToyId: "abc"), CancellationToken.None);
using (var sent = JsonDocument.Parse(handler.Body)) Check(sent.RootElement.GetProperty("action").GetString() == "Vibrate:7" && sent.RootElement.GetProperty("toy").GetString() == "abc", "API targets chosen toy and sends vibration payload");
await api.SendAsync(new(ActionKind.Pulse, 8, 4, 250), CancellationToken.None);
using (var sent = JsonDocument.Parse(handler.Body)) Check(sent.RootElement.GetProperty("command").GetString() == "Pattern" && sent.RootElement.GetProperty("rule").GetString() == "V:1;F:v;S:250#" && sent.RootElement.GetProperty("apiVer").GetInt32() == 2, "pulse uses native pattern protocol");
await api.SendAsync(ToyCommand.Stop, CancellationToken.None);
using (var sent = JsonDocument.Parse(handler.Body)) Check(sent.RootElement.GetProperty("action").GetString() == "Stop" && !sent.RootElement.TryGetProperty("toy", out _), "emergency stop targets all toys");
handler.Response = """{"code":400} """;
var rejected = false;
try { await api.SendAsync(ToyCommand.Stop, CancellationToken.None); } catch (InvalidOperationException) { rejected = true; }
Check(rejected, "API rejection is surfaced");
var invalidEndpoint = false;
try { await using var invalid = new LocalApiTransport("http://example.com/command"); } catch (ArgumentException) { invalidEndpoint = true; }
Check(invalidEndpoint, "remote plaintext endpoint rejected");

var profilePath = Path.Combine(Path.GetTempPath(), "lovense-tests-" + Guid.NewGuid() + ".json");
try
{
    ProfileStore.Save(ProfileStore.Defaults(), profilePath);
    Check(ProfileStore.Load(profilePath).Rules.Count == 3, "profiles round-trip with enum names");
    File.WriteAllText(profilePath, """{"Version":99,"Rules":[]} """);
    var bad = false; try { ProfileStore.Load(profilePath); } catch (InvalidDataException) { bad = true; }
    Check(bad, "unsupported profile schema rejected");
    File.WriteAllText(profilePath, """{"Version":1,"Rules":[{"Name":"Legacy","Keys":"A"}]}""");
    var migrated = ProfileStore.Load(profilePath);
    Check(migrated.Version == 3 && migrated.Rules[0].Keys == "A" && !migrated.Rules[0].KeyboardLayer, "v1 profile migrates without changing existing rule behavior");
    migrated.Rules[0].KeyboardLayer = true; migrated.Rules[0].Keys = ""; migrated.Rules[0].ExcludedKeys = "Escape,LeftCtrl"; migrated.Rules[0].WindowTitleContains = "game";
    ProfileStore.Save(migrated, profilePath);
    var restored = ProfileStore.Load(profilePath).Rules[0];
    Check(restored.KeyboardLayer && restored.ExcludedKeys == "Escape,LeftCtrl" && restored.WindowTitleContains == "game", "keyboard exclusions and window filters roundtrip in v2");
}
finally { File.Delete(profilePath); }
var baseLayer = new Rule { KeyboardLayer = true, Keys = "", ExcludedKeys = "Escape,LeftCtrl", DurationSeconds = 0.15, CooldownMs = 200, Intensity = 3 };
var gameLayer = new Rule { KeyboardLayer = true, Keys = "W,A,S,D", Process = "game.exe", DurationSeconds = 0.15, CooldownMs = 200, Intensity = 12 };
var oneKeyLayer = new Rule { KeyboardLayer = true, Keys = "W", Process = "game", DurationSeconds = 0.15, CooldownMs = 300, Intensity = 16 };
var layerEngine = new RuleEngine(); layerEngine.Reset(0);
RuleMatch? LayerPress(long at, string key, string app = "game", string title = "")
{
    layerEngine.Process(new(EventKind.KeyUp, at - 1, key, app, WindowTitle: title), [baseLayer, gameLayer, oneKeyLayer]);
    return layerEngine.Process(new(EventKind.KeyDown, at, key, app, WindowTitle: title), [baseLayer, gameLayer, oneKeyLayer]);
}
Check(LayerPress(0, "W")?.Rule.Id == oneKeyLayer.Id, "specific key wins over WASD and all keys");
Check(LayerPress(100, "W") is null, "cooling override does not fall back to broader group");
Check(LayerPress(300, "A")?.Rule.Id == gameLayer.Id, "keyboard group matches any included key, not a chord");
Check(LayerPress(600, "S", "editor")?.Rule.Id == baseLayer.Id, "application override falls back outside its application");
Check(LayerPress(900, "Escape") is null && LayerPress(1000, "LeftCtrl") is null, "all-keys exclusions reject selected modifiers and Escape");
oneKeyLayer.WindowTitleContains = "raid";
Check(LayerPress(1200, "W", "game", "Game — Menu")?.Rule.Id == gameLayer.Id, "window title mismatch leaves application group active");
Check(LayerPress(1500, "W", "game", "Game — RAID")?.Rule.Id == oneKeyLayer.Id, "window title contains is case insensitive");
gameLayer.Priority = 10;
Check(LayerPress(1800, "W", "game", "RAID")?.Rule.Id == gameLayer.Id, "explicit keyboard priority overrides narrower key group");
gameLayer.Enabled = false; oneKeyLayer.Enabled = false;
Check(LayerPress(2100, "W")?.Rule.Id == baseLayer.Id, "disabled overrides allow base layer");
var legacyPress = new Rule { Keys = "Space", Intensity = 20, CooldownMs = 100 };
layerEngine.Reset(0);
Check(layerEngine.Process(new(EventKind.KeyDown, 0, "Space"), [legacyPress, baseLayer])?.Rule.Id == baseLayer.Id, "visual assignment takes precedence over legacy key-down rule");
layerEngine.Process(new(EventKind.KeyUp, 10, "Space"), [legacyPress, baseLayer]);
Check(layerEngine.Process(new(EventKind.KeyDown, 100, "Space"), [legacyPress, baseLayer]) is null, "legacy key-down cannot leak through assignment cooldown");
Check(new Rule { KeyboardLayer = true, Event = EventKind.Chord }.Validate() is not null, "keyboard layer cannot silently change to chord semantics");
var presetPath = Path.Combine(Path.GetTempPath(), "lovense-keyboard-" + Guid.NewGuid() + ".json");
try
{
    string[] presetKeys = ["A", "W", "S", "D", "Escape", "LeftCtrl", "RightCtrl"];
    var original = new Rule { Keys = "A", DurationSeconds = 0.15, CooldownMs = 200 };
    KeyboardPresetStore.Save(presetPath, [original], presetKeys);
    var json = File.ReadAllText(presetPath);
    var preset = KeyboardPresetStore.Parse(json, presetKeys);
    Check(preset.Rules.Single().Id == original.Id && preset.Rules.Single().KeyboardLayer && !original.KeyboardLayer, "keyboard export normalizes a copy and preserves identity");
    Check(preset.Rules.Single().DurationSeconds == 0.15, "keyboard JSON keeps fractional seconds across locales");
    void RejectPreset(string invalidJson, string name)
    {
        var rejected = false;
        try { KeyboardPresetStore.Parse(invalidJson, presetKeys); }
        catch (Exception e) when (e is System.Text.Json.JsonException or InvalidDataException) { rejected = true; }
        Check(rejected, name);
    }
    RejectPreset(json.Replace("\"Version\": 1", "\"Version\": 99"), "keyboard rejects unknown format version");
    RejectPreset(json.Replace("LovenseIntegrator.Keyboard", "Other"), "keyboard rejects wrong format");
    RejectPreset(json.Replace("\"Keys\": \"A\"", "\"Keys\": \"Ctrl\""), "keyboard rejects unknown key spelling");
    RejectPreset(json.Replace("\"Keys\": \"A\"", "\"Keys\": \",,+\""), "keyboard rejects empty explicit key selection");
    RejectPreset(json.Replace("\"ExcludedKeys\": \"\"", "\"ExcludedKeys\": \"A\""), "keyboard rejects exclusions combined with explicit subset");
    RejectPreset(json.Replace("\"Event\": \"KeyDown\"", "\"Event\": \"KeyHeld\""), "keyboard rejects unsupported event");
    RejectPreset(json.Replace("\"KeyboardLayer\": true", "\"KeyboardLayer\": false"), "keyboard rejects disabled layer semantics");
    RejectPreset(json.Replace("\"DurationSeconds\": 0.15", "\"DurationSeconds\": 0.01"), "keyboard rejects unsupported duration");
    RejectPreset(json.Replace("\"Keys\":", "\"Keyz\":"), "keyboard rejects unknown fields instead of defaulting to Space");
    RejectPreset(json.Replace("\"Keys\": \"A\",", ""), "keyboard rejects missing required Keys");
    RejectPreset(json.Replace("\"Intensity\": 7", "\"Intensity\": 7, \"Intensity\": 20"), "keyboard rejects duplicate JSON properties");
    RejectPreset(json.Replace("\"Vibrate\"", "0"), "keyboard requires named actions");
    RejectPreset("""{"Format":"LovenseIntegrator.Keyboard","Version":1,"Rules":[]}""", "keyboard rejects empty bundle");
    var otherEvent = new Rule { Event = EventKind.KeyHeld, Name = "Keep me" };
    var changedPreset = preset.Rules.Single().Copy(); changedPreset.Intensity = 9;
    var merged = KeyboardPresetStore.Merge([original, otherEvent], [changedPreset]);
    Check(merged.Count == 2 && merged[0].Intensity == 9 && merged[1].Id == otherEvent.Id && original.Intensity == 7, "keyboard merge updates same ID without mutating existing rules");
    Check(KeyboardPresetStore.Merge(merged, [changedPreset]).Count == 2, "repeated keyboard import is idempotent");
    changedPreset.Id = otherEvent.Id;
    var collision = false; try { KeyboardPresetStore.Merge([original, otherEvent], [changedPreset]); } catch (InvalidDataException) { collision = true; }
    Check(collision && otherEvent.Event == EventKind.KeyHeld, "keyboard merge rejects collision with other events");
    var duplicate = false; try { KeyboardPresetStore.Validate([preset.Rules[0], preset.Rules[0]], presetKeys); } catch (InvalidDataException) { duplicate = true; }
    Check(duplicate, "keyboard rejects duplicate IDs");
    var sample = KeyboardPresetStore.Load(Path.Combine("examples", "keyboard-assignments.json"), presetKeys);
    Check(sample.Rules.Count == 2 && sample.Rules[0].ExcludedKeys == "Escape,LeftCtrl,RightCtrl", "distributed AI example parses with exclusions and application override");
}
finally { File.Delete(presetPath); }
var mouseBase = new Rule { MouseLayer = true, Event = EventKind.MouseDown, Keys = "", ExcludedKeys = "Right", CooldownMs = 0, DurationSeconds = 0.1 };
var mouseEngine = new RuleEngine(); mouseEngine.Reset(0);
foreach (var button in MouseInputs.Buttons)
{
    var pressed = mouseEngine.Process(new(EventKind.MouseDown, 0, button), [mouseBase]);
    Check((pressed is not null) == (button != "Right"), "visual mouse routes button " + button);
    Check(mouseEngine.Process(new(EventKind.MouseDown, 1, button), [mouseBase]) is null, "held mouse button does not repeat " + button);
    mouseEngine.Process(new(EventKind.MouseUp, 2, button), [mouseBase]);
}
Check(mouseEngine.Process(new(EventKind.MouseDown, 3, "Left"), [mouseBase])?.Command().DurationSeconds == 0.1, "mouse can retrigger immediately after release with zero cooldown");
Check(mouseEngine.Process(new(EventKind.KeyDown, 4, "Left"), [mouseBase]) is null, "mouse assignment cannot match keyboard arrow");
var wheel = new Rule { MouseLayer = true, Event = EventKind.MouseDown, Keys = "Up,Down", WheelContinuous = true, WheelIdleMs = 150, CooldownMs = 1000, DurationSeconds = 0.1 };
var upMatch = mouseEngine.Process(new(EventKind.MouseWheel, 5, "Up"), [mouseBase, wheel]);
Check(upMatch is { RenewWheel: true } && upMatch.Command().DurationSeconds == 0.15 && upMatch.Command().RenewalId == wheel.Id.ToString(), "continuous wheel builds a renewable deadline from idle timeout");
Check(mouseEngine.Process(new(EventKind.MouseWheel, 6, "Down"), [mouseBase, wheel]) is { RenewWheel: true }, "continuous wheel renews on reverse direction despite cooldown");
Check(mouseEngine.Process(new(EventKind.Timer, 100), [mouseBase, wheel]) is null, "continuous wheel is never renewed by timer ticks");
var wheelOverride = wheel.Copy(); wheelOverride.Id = Guid.NewGuid(); wheelOverride.Keys = "Up"; wheelOverride.Process = "game"; wheelOverride.WindowTitleContains = "Raid"; wheelOverride.Intensity = 12; wheelOverride.WheelContinuous = false; wheelOverride.CooldownMs = 100;
Check(mouseEngine.Process(new(EventKind.MouseWheel, 10, "Up", "game", WindowTitle: "RAID"), [mouseBase, wheel, wheelOverride]) is { RenewWheel: false, Rule.Intensity: 12 }, "window override can use single wheel impulses");
Check(mouseEngine.Process(new(EventKind.MouseWheel, 20, "Up", "game", WindowTitle: "RAID"), [mouseBase, wheel, wheelOverride]) is null, "cooling mouse override cannot leak base wheel effect");
Check(mouseEngine.Process(new(EventKind.MouseWheel, 30, "Up", "editor"), [mouseBase, wheel, wheelOverride])?.Rule.Id == wheel.Id, "wheel falls back outside override application");
var legacyWheel = new Rule { Event = EventKind.MouseWheel, Keys = "Up", Priority = 999 };
Check(mouseEngine.Process(new(EventKind.MouseWheel, 40, "Up"), [legacyWheel, wheel])?.Rule.Id == wheel.Id, "visual mouse takes precedence over legacy wheel rule");
Check(MousePresetStore.Normalize(new Rule { Event = EventKind.MouseDown, Keys = "" }).Keys == "Left,Right,Middle,X1,X2", "legacy any mouse button does not gain wheel when converted");
Check(MousePresetStore.Normalize(new Rule { Event = EventKind.MouseWheel, Keys = "" }).Keys == "Up,Down", "legacy any wheel does not gain buttons when converted");
Check(new Rule { MouseLayer = true, KeyboardLayer = true }.Validate() is not null && new Rule { WheelContinuous = true }.Validate() is not null, "mouse and keyboard mode semantics cannot be mixed");
var invalidMouse = wheel.Copy(); invalidMouse.WheelIdleMs = 99;
Check(invalidMouse.Validate() is not null, "wheel idle timeout validates lower bound");
invalidMouse.WheelIdleMs = 1001; Check(invalidMouse.Validate() is not null, "wheel idle timeout validates upper bound");
var mouseFile = Path.Combine(Path.GetTempPath(), "lovense-mouse-" + Guid.NewGuid() + ".json");
try
{
    MousePresetStore.Save(mouseFile, [mouseBase, wheel], MouseInputs.All);
    var bundle = MousePresetStore.Load(mouseFile, MouseInputs.All);
    Check(bundle.Rules[1].WheelContinuous && bundle.Rules[0].CooldownMs == 0, "mouse export/import preserves live settings");
    var mouseJson = File.ReadAllText(mouseFile);
    foreach (var invalidJson in new[] { mouseJson.Replace("LovenseIntegrator.Mouse", "LovenseIntegrator.Keyboard"), mouseJson.Replace("\"Up,Down\"", "\"WheelUp\""), mouseJson.Replace("\"WheelIdleMs\": 150", "\"WheelIdleMs\": 1"), mouseJson.Replace("\"MouseLayer\": true", "\"MouseLayer\": false") })
    {
        var mouseRejected = false; try { MousePresetStore.Parse(invalidJson, MouseInputs.All); } catch (InvalidDataException) { mouseRejected = true; }
        Check(mouseRejected, "invalid mouse bundle is rejected");
    }
    var collideKeyboard = baseLayer.Copy(); collideKeyboard.Id = wheel.Id;
    var rejectedCollision = false; try { MousePresetStore.Merge([collideKeyboard], [wheel]); } catch (InvalidDataException) { rejectedCollision = true; }
    Check(rejectedCollision, "mouse import cannot overwrite keyboard by ID");
    Check(MousePresetStore.Merge([baseLayer, mouseBase], [wheel]).Count == 3 && MousePresetStore.Merge([baseLayer, mouseBase, wheel], [wheel]).Count == 3, "mouse merge preserves keyboard and reimports by ID");
    Check(MousePresetStore.Load(Path.Combine("examples", "mouse-assignments.json"), MouseInputs.All).Rules.Count == 2, "mouse AI example imports");
    ProfileStore.Save(new Profile { Version = 2, Rules = [baseLayer] }, mouseFile);
    Check(ProfileStore.Load(mouseFile) is { Version: 3 } p && p.Rules[0].KeyboardLayer && !p.Rules[0].MouseLayer, "v2 profile upgrades without changing keyboard mode");
    ProfileStore.Save(new Profile { Rules = [wheel] }, mouseFile);
    Check(ProfileStore.Load(mouseFile).Rules[0].WheelContinuous, "v3 profile roundtrips continuous wheel");
}
finally { File.Delete(mouseFile); }

var liveLevels = new System.Collections.Concurrent.ConcurrentQueue<(string Id, int Level)>();
var liveWaits = new List<TaskCompletionSource>();
var liveStop = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
await using (var liveReplay = new BleTransport([new("lush", "Replay", true)],
    (id, level) => { liveLevels.Enqueue((id, level)); if (level == 0) liveStop.TrySetResult(); },
    (ms, ct) => { var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); liveWaits.Add(tcs); return tcs.Task; }))
{
    var liveCommand = new ToyCommand(ActionKind.Vibrate, 5, 0.15, ToyId: "lush", RenewalId: "wheel");
    await liveReplay.SendAsync(liveCommand, CancellationToken.None);
    for (var i = 0; i < 100; i++) await liveReplay.SendAsync(liveCommand, CancellationToken.None);
    Check(liveLevels.Count == 1 && liveWaits.Count == 101, "100 wheel renewals extend timers without duplicate BLE writes");
    foreach (var oldWait in liveWaits.Take(100)) oldWait.TrySetResult();
    await Task.Delay(20);
    Check(liveLevels.Count == 1, "old wheel timers cannot stop renewed vibration");
    liveWaits[^1].SetResult(); await liveStop.Task.WaitAsync(TimeSpan.FromSeconds(2));
    Check(liveLevels.Last().Level == 0, "wheel silence ends with stop");
    await liveReplay.SendAsync(liveCommand, CancellationToken.None);
    Check(liveLevels.Last().Level == 5 && liveLevels.Count == 3, "wheel restarts after idle stop");
    var oldWheelWait = liveWaits[^1];
    await liveReplay.SendAsync(new(ActionKind.Vibrate, 12, 0.1, ToyId: "lush"), CancellationToken.None);
    oldWheelWait.SetResult(); await Task.Delay(20);
    Check(liveLevels.Last().Level == 12, "manual replacement survives old wheel stop");
    await liveReplay.SendAsync(liveCommand, CancellationToken.None);
    Check(liveLevels.Last().Level == 5, "wheel resumes correct level after another effect");
    await liveReplay.SendAsync(liveCommand with { Intensity = 9 }, CancellationToken.None);
    Check(liveLevels.Last().Level == 9, "changed wheel intensity sends updated BLE level");
    await liveReplay.SendAsync(ToyCommand.Stop, CancellationToken.None);
    var countAtStop = liveLevels.Count;
    foreach (var wait in liveWaits) wait.TrySetResult(); await Task.Delay(20);
    Check(liveLevels.Count == countAtStop && liveLevels.Last().Level == 0, "emergency stop cancels all pending wheel timers");
}
var wheelRestHandler = new FakeHandler { Response = "{\"code\":200}" };
await using (var wheelRest = new LocalApiTransport("https://example.com/command", wheelRestHandler))
{
    var mouseRejected = false; try { await wheelRest.SendAsync(new(ActionKind.Vibrate, 5, 2, RenewalId: "wheel"), CancellationToken.None); } catch (ArgumentException) { mouseRejected = true; }
    Check(mouseRejected && wheelRestHandler.Body == "", "REST rejects live wheel before sending even for longer duration");
}
var connectOrder = new List<string>();
var firstConnection = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
var secondConnection = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
BleTransport? multiConnect = null;
multiConnect = new BleTransport([new("a", "Lush", false), new("b", "Ferri", false)], (_, _) => { }, (ms, ct) => Task.Delay(ms, ct),
    scan: () => connectOrder.Add("scan"), stop: () => { connectOrder.Add("scan-stopped"); multiConnect!.ReportScanStopped(); },
    connect: id => { connectOrder.Add("connect:" + id); if (id == "a") firstConnection.TrySetResult(); else secondConnection.TrySetResult(); },
    batteryQuery: id => { connectOrder.Add("battery:" + id); multiConnect!.ReportBattery(id, "75"); });
await using (multiConnect)
{
    var discovery = multiConnect.DiscoverAsync(CancellationToken.None, 0, 2000);
    await firstConnection.Task.WaitAsync(TimeSpan.FromSeconds(2));
    Check(!connectOrder.Contains("connect:b") && !connectOrder.Contains("battery:a"), "second connection and battery query wait for first connection acknowledgement");
    multiConnect.ReportConnection("a", true);
    await secondConnection.Task.WaitAsync(TimeSpan.FromSeconds(2));
    Check(connectOrder.SequenceEqual(new[] { "scan", "scan-stopped", "connect:a", "battery:a", "connect:b" }), "scan, two connects and battery requests execute in order");
    multiConnect.ReportConnection("b", true);
    var connected = await discovery;
    Check(connected.Count == 2 && connected.All(t => t.Connected && t.Battery == 75), "both connected devices retain independent battery and connection state");
    var connectCount = connectOrder.Count(x => x.StartsWith("connect:"));
    await multiConnect.DiscoverAsync(CancellationToken.None, 0, 2000);
    Check(connectOrder.Count(x => x.StartsWith("connect:")) == connectCount, "refresh does not reconnect already connected devices");
    multiConnect.ReportConnection("b", false);
    var reconnect = multiConnect.DiscoverAsync(CancellationToken.None, 0, 2000);
    while (connectOrder.Count(x => x == "connect:b") < 2) await Task.Delay(1);
    multiConnect.ReportConnection("b", true);
    Check((await reconnect).All(t => t.Connected) && connectOrder.Count(x => x == "connect:a") == 1, "refresh reconnects only missing second device");
}
await UpdateTests.RunAsync(Check);
LocalizationTests.Run(Check);
Console.WriteLine($"{passed} checks passed.");

sealed class FakeHandler : HttpMessageHandler
{
    public string Response = "", Body = "", Platform = "";
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        Body = await request.Content!.ReadAsStringAsync(ct);
        Platform = request.Headers.GetValues("X-platform").Single();
        return new(HttpStatusCode.OK) { Content = new StringContent(Response, Encoding.UTF8, "application/json") };
    }
}
