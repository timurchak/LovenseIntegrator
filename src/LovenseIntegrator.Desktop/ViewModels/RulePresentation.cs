using LovenseIntegrator.Desktop.Localization;
using System.Globalization;
using System.Windows.Data;
using LovenseIntegrator.Core;

namespace LovenseIntegrator.Desktop.ViewModels;

public sealed record EventOption(EventKind Value, string Label, string Category, string Help,
    bool UsesKeys = false, bool UsesThreshold = false, bool UsesWindow = false, bool TimeThreshold = false)
{
    public string Marker => Category == L.T("Keyboard") ? "K" : Category == L.T("Mouse") ? "M" : Category == L.T("Applications") ? "P" : "V";
}
public sealed record RuleRecipe(string Name, string Description, Rule Rule);

public static class RulePresentation
{
    public static IReadOnlyList<EventOption> Events { get; } =
    [
        new(EventKind.ScreenEvent, L.T("Game event from screen"), L.T("Screen"), L.T("A named event from WowScreenEvents. Start reading in Screen mode first.")),
        new(EventKind.KeyDown, L.T("Key press"), L.T("Keyboard"), L.T("Triggers on a physical key press. Holding a key does not count as repeated presses."), true),
        new(EventKind.KeyUp, L.T("Key release"), L.T("Keyboard"), L.T("Triggers when you release the selected key."), true),
        new(EventKind.KeyHeld, L.T("Key held"), L.T("Keyboard"), L.T("Triggers once when a key has been held long enough. Release it to trigger again."), true, true, false, true),
        new(EventKind.KeyReleasedAfterHold, L.T("Release after hold"), L.T("Keyboard"), L.T("Triggers on release if the key was held for at least the selected duration."), true, true, false, true),
        new(EventKind.DoublePress, L.T("Double press"), L.T("Keyboard"), L.T("Two presses of the same key within the selected interval."), true, true, false, true),
        new(EventKind.TriplePress, L.T("Triple press"), L.T("Keyboard"), L.T("Three presses of the same key. The interval runs from the first to the third."), true, true, false, true),
        new(EventKind.Chord, L.T("Key combination"), L.T("Keyboard"), L.T("Press the keys together. Triggers once until the combination is released."), true),
        new(EventKind.Sequence, L.T("Sequence"), L.T("Keyboard"), L.T("Press keys in order within the time window. An unrelated key breaks the sequence."), true, false, true),
        new(EventKind.PressCount, L.T("Press count over time"), L.T("Keyboard"), L.T("Counts physical presses of a selected key or any key within a sliding window."), true, true, true),
        new(EventKind.CleanTypingStreak, L.T("Typing without corrections"), L.T("Keyboard"), L.T("A series of typing keys without Backspace or Delete. Modifiers and arrows do not count; corrections reset the series."), false, true),
        new(EventKind.TypingRateAbove, L.T("Fast typing"), L.T("Keyboard"), L.T("Triggers once when speed crosses the threshold upwards. Speed must fall below it before another trigger."), false, true, true),
        new(EventKind.TypingRateBelow, L.T("Slow typing"), L.T("Keyboard"), L.T("Triggers when typing speed falls below the threshold. The first measurement waits for a full window."), false, true, true),
        new(EventKind.MouseDown, L.T("Mouse button press"), L.T("Mouse"), L.T("Left, right, middle or side button."), true),
        new(EventKind.MouseUp, L.T("Mouse button release"), L.T("Mouse"), L.T("Triggers when the selected button is released."), true),
        new(EventKind.MouseDoubleClick, L.T("Double click"), L.T("Mouse"), L.T("Two clicks of the same mouse button within the selected interval."), true, true, false, true),
        new(EventKind.MouseHeld, L.T("Mouse button held"), L.T("Mouse"), L.T("Triggers once on a long press. Release the button to trigger again."), true, true, false, true),
        new(EventKind.MouseWheel, L.T("Mouse wheel"), L.T("Mouse"), L.T("Scroll up, down or in either direction."), true),
        new(EventKind.ForegroundChanged, L.T("Application changed"), L.T("Applications"), L.T("Triggers when the active application changes. Choose an application in the conditions.")),
        new(EventKind.InputIdle, L.T("No input"), L.T("Time"), L.T("Triggers once after inactivity. Counts keys, buttons and wheel events, but not mouse movement."), false, true, false, true),
        new(EventKind.ActivityResumed, L.T("Activity resumed"), L.T("Time"), L.T("First key press, button press or wheel event after a break."), false, true, false, true),
        new(EventKind.Timer, L.T("Timer"), L.T("Time"), L.T("Repeats at the selected interval while rules are enabled."), false, true, false, true)
    ];
    public static EventOption Event(EventKind value) => Events.Single(e => e.Value == value);
    public static string KeyName(string key) => key switch
    {
        "Space" => "Space", "Return" or "Enter" => "Enter", "Back" => "Backspace", "LeftCtrl" => "Left Ctrl", "RightCtrl" => "Right Ctrl",
        "LeftShift" => "Left Shift", "RightShift" => "Right Shift", "LeftAlt" => "Left Alt", "RightAlt" => "Right Alt", "Escape" => "Esc",
        "Left" => "Left arrow", "Right" => "Right arrow", "Up" => "Up arrow", "Down" => "Down arrow", _ => key
    };
    public static string MouseName(string key) => key switch { "Left" => L.T("left"), "Right" => L.T("right"), "Middle" => L.T("middle"), "X1" => L.T("side 1"), "X2" => L.T("side 2"), "Up" => L.T("up"), "Down" => L.T("down"), _ => L.T("any") };
    public static string Number(double value) => value.ToString("0.##", CultureInfo.GetCultureInfo("en-US"));
    public static string Duration(double seconds) => seconds < 1 ? L.F($"{Number(seconds * 1000)} ms") : L.F($"{Number(seconds)} s");
    public static string Describe(Rule r)
    {
        var keys = r.Keys.Length == 0 ? L.T("any key") : string.Join(r.Event == EventKind.Sequence ? " → " : " + ", r.KeyList().Select(KeyName));
        var time = Number(r.Threshold / 1000);
        var when = r.Event switch
        {
            EventKind.ScreenEvent => L.T(ScreenEvents.Find(r.ScreenEventId)?.Name ?? r.ScreenEventId),
            EventKind.KeyDown => L.F($"pressed: {keys}"), EventKind.KeyUp => L.F($"released: {keys}"), EventKind.KeyHeld => L.F($"{keys} held for {time} s"),
            EventKind.KeyReleasedAfterHold => L.F($"{keys} released after {time} s"),
            EventKind.DoublePress => L.F($"double press of {keys} within {time} s"), EventKind.TriplePress => L.F($"triple press of {keys} within {time} s"),
            EventKind.Chord => L.F($"held: {keys}"), EventKind.Sequence => L.F($"pressed: {keys} within {Number(r.WindowMs / 1000.0)} s"),
            EventKind.PressCount => L.F($"{keys}: {Number(r.Threshold)} presses in {Number(r.WindowMs / 1000.0)} s"),
            EventKind.CleanTypingStreak => L.F($"{Number(r.Threshold)} typing keys without corrections"),
            EventKind.TypingRateAbove => L.F($"speed above {Number(r.Threshold)} presses/s"), EventKind.TypingRateBelow => L.F($"speed below {Number(r.Threshold)} presses/s"),
            EventKind.MouseDown => L.F($"mouse button pressed: {MouseName(r.Keys)}"),
            EventKind.MouseUp => L.F($"mouse button released: {MouseName(r.Keys)}"),
            EventKind.MouseDoubleClick => L.F($"double click ({MouseName(r.Keys)}) within {time} s"), EventKind.MouseHeld => L.F($"mouse button ({MouseName(r.Keys)}) held for {time} s"),
            EventKind.MouseWheel => L.F($"scroll {(r.Keys.Length == 0 ? L.T("in either direction") : MouseName(r.Keys))}"),
            EventKind.InputIdle => L.F($"no input for {time} s"), EventKind.ActivityResumed => L.F($"input resumes after {time} s"), EventKind.Timer => L.F($"elapsed: {time} s"),
            _ => L.T("the active application changed")
        };
        var action = r.Action switch
        {
            ActionKind.Stop => L.T("stop"), ActionKind.Pulse => L.F($"pulse {r.Intensity}/20 for {Duration(r.DurationSeconds)}"),
            ActionKind.RateMapped => L.F($"speed-based intensity for {Duration(r.DurationSeconds)}"), _ => L.F($"vibration {r.Intensity}/20 for {Duration(r.DurationSeconds)}")
        };
        return L.F($"When {when}{(r.Process.Length == 0 ? "" : L.F($" in {r.Process}"))} → {action}.");
    }
    public static IReadOnlyList<RuleRecipe> Recipes { get; } =
    [
        new(L.T("Hold Space"), L.T("After 0.8 s — gentle vibration"), new() { Name = L.T("Hold Space"), Event = EventKind.KeyHeld, Keys = "Space", Threshold = 800, Intensity = 5 }),
        new(L.T("Double press"), L.T("Two Space presses — pulse"), new() { Name = L.T("Double Space"), Event = EventKind.DoublePress, Keys = "Space", Threshold = 400, Action = ActionKind.Pulse, Intensity = 7 }),
        new(L.T("Typing rhythm"), L.T("Fast typing — speed-based intensity"), new() { Name = L.T("Typing rhythm"), Event = EventKind.TypingRateAbove, Keys = "", Threshold = 4, WindowMs = 3000, Action = ActionKind.RateMapped, Intensity = 8 }),
        new(L.T("No corrections"), L.T("20 typing keys — pulse"), new() { Name = L.T("No corrections"), Event = EventKind.CleanTypingStreak, Keys = "", Threshold = 20, Action = ActionKind.Pulse, Intensity = 6 }),
        new(L.T("Back to work"), L.T("First input after 30 seconds of inactivity"), new() { Name = L.T("Back to work"), Event = EventKind.ActivityResumed, Keys = "", Threshold = 30000, Intensity = 4 }),
        new(L.T("Every 30 seconds"), L.T("Periodic gentle feedback"), new() { Name = L.T("Every 30 seconds"), Event = EventKind.Timer, Keys = "", Threshold = 30000, Intensity = 4 }),
        new(L.T("Keyboard feedback"), L.T("Any key → 150 ms · up to 5/s · BLE"), new() { Name = L.T("Keyboard feedback"), Event = EventKind.KeyDown, Keys = "", DurationSeconds = 0.15, CooldownMs = 200, Intensity = 5 })
    ];
}

public sealed class RuleSummaryConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value is Rule rule ? RulePresentation.Describe(rule) : "";
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
}
