using System.Globalization;
using System.Windows.Data;
using LovenseIntegrator.Core;

namespace LovenseIntegrator.Desktop.ViewModels;

public sealed record EventOption(EventKind Value, string Label, string Category, string Help,
    bool UsesKeys = false, bool UsesThreshold = false, bool UsesWindow = false, bool TimeThreshold = false)
{
    public string Marker => Category switch { "Keyboard" => "K", "Mouse" => "M", "Applications" => "P", _ => "V" };
}
public sealed record RuleRecipe(string Name, string Description, Rule Rule);

public static class RulePresentation
{
    public static IReadOnlyList<EventOption> Events { get; } =
    [
        new(EventKind.KeyDown, "Key press", "Keyboard", "Triggers on a physical key press. Holding a key does not count as repeated presses.", true),
        new(EventKind.KeyUp, "Key release", "Keyboard", "Triggers when you release the selected key.", true),
        new(EventKind.KeyHeld, "Key held", "Keyboard", "Triggers once when a key has been held long enough. Release it to trigger again.", true, true, false, true),
        new(EventKind.KeyReleasedAfterHold, "Release after hold", "Keyboard", "Triggers on release if the key was held for at least the selected duration.", true, true, false, true),
        new(EventKind.DoublePress, "Double press", "Keyboard", "Two presses of the same key within the selected interval.", true, true, false, true),
        new(EventKind.TriplePress, "Triple press", "Keyboard", "Three presses of the same key. The interval runs from the first to the third.", true, true, false, true),
        new(EventKind.Chord, "Key combination", "Keyboard", "Press the keys together. Triggers once until the combination is released.", true),
        new(EventKind.Sequence, "Sequence", "Keyboard", "Press keys in order within the time window. An unrelated key breaks the sequence.", true, false, true),
        new(EventKind.PressCount, "Press count over time", "Keyboard", "Counts physical presses of a selected key or any key within a sliding window.", true, true, true),
        new(EventKind.CleanTypingStreak, "Typing without corrections", "Keyboard", "A series of typing keys without Backspace or Delete. Modifiers and arrows do not count; corrections reset the series.", false, true),
        new(EventKind.TypingRateAbove, "Fast typing", "Keyboard", "Triggers once when speed crosses the threshold upwards. Speed must fall below it before another trigger.", false, true, true),
        new(EventKind.TypingRateBelow, "Slow typing", "Keyboard", "Triggers when typing speed falls below the threshold. The first measurement waits for a full window.", false, true, true),
        new(EventKind.MouseDown, "Mouse button press", "Mouse", "Left, right, middle or side button.", true),
        new(EventKind.MouseUp, "Mouse button release", "Mouse", "Triggers when the selected button is released.", true),
        new(EventKind.MouseDoubleClick, "Double click", "Mouse", "Two clicks of the same mouse button within the selected interval.", true, true, false, true),
        new(EventKind.MouseHeld, "Mouse button held", "Mouse", "Triggers once on a long press. Release the button to trigger again.", true, true, false, true),
        new(EventKind.MouseWheel, "Mouse wheel", "Mouse", "Scroll up, down or in either direction.", true),
        new(EventKind.ForegroundChanged, "Application changed", "Applications", "Triggers when the active application changes. Choose an application in the conditions."),
        new(EventKind.InputIdle, "No input", "Time", "Triggers once after inactivity. Counts keys, buttons and wheel events, but not mouse movement.", false, true, false, true),
        new(EventKind.ActivityResumed, "Activity resumed", "Time", "First key press, button press or wheel event after a break.", false, true, false, true),
        new(EventKind.Timer, "Timer", "Time", "Repeats at the selected interval while rules are enabled.", false, true, false, true)
    ];
    public static EventOption Event(EventKind value) => Events.Single(e => e.Value == value);
    public static string KeyName(string key) => key switch
    {
        "Space" => "Space", "Return" or "Enter" => "Enter", "Back" => "Backspace", "LeftCtrl" => "Left Ctrl", "RightCtrl" => "Right Ctrl",
        "LeftShift" => "Left Shift", "RightShift" => "Right Shift", "LeftAlt" => "Left Alt", "RightAlt" => "Right Alt", "Escape" => "Esc",
        "Left" => "Left arrow", "Right" => "Right arrow", "Up" => "Up arrow", "Down" => "Down arrow", _ => key
    };
    public static string MouseName(string key) => key switch { "Left" => "left", "Right" => "right", "Middle" => "middle", "X1" => "side 1", "X2" => "side 2", "Up" => "up", "Down" => "down", _ => "any" };
    public static string Number(double value) => value.ToString("0.##", CultureInfo.GetCultureInfo("en-US"));
    public static string Duration(double seconds) => seconds < 1 ? $"{Number(seconds * 1000)} ms" : $"{Number(seconds)} s";
    public static string Describe(Rule r)
    {
        var keys = r.Keys.Length == 0 ? "any key" : string.Join(r.Event == EventKind.Sequence ? " → " : " + ", r.KeyList().Select(KeyName));
        var time = Number(r.Threshold / 1000);
        var when = r.Event switch
        {
            EventKind.KeyDown => $"pressed: {keys}", EventKind.KeyUp => $"released: {keys}", EventKind.KeyHeld => $"{keys} held for {time} s",
            EventKind.KeyReleasedAfterHold => $"{keys} released after {time} s",
            EventKind.DoublePress => $"double press of {keys} within {time} s", EventKind.TriplePress => $"triple press of {keys} within {time} s",
            EventKind.Chord => $"held: {keys}", EventKind.Sequence => $"pressed: {keys} within {Number(r.WindowMs / 1000.0)} s",
            EventKind.PressCount => $"{keys}: {Number(r.Threshold)} presses in {Number(r.WindowMs / 1000.0)} s",
            EventKind.CleanTypingStreak => $"{Number(r.Threshold)} typing keys without corrections",
            EventKind.TypingRateAbove => $"speed above {Number(r.Threshold)} presses/s", EventKind.TypingRateBelow => $"speed below {Number(r.Threshold)} presses/s",
            EventKind.MouseDown => $"mouse button pressed: {MouseName(r.Keys)}",
            EventKind.MouseUp => $"mouse button released: {MouseName(r.Keys)}",
            EventKind.MouseDoubleClick => $"double click ({MouseName(r.Keys)}) within {time} s", EventKind.MouseHeld => $"mouse button ({MouseName(r.Keys)}) held for {time} s",
            EventKind.MouseWheel => $"scroll {(r.Keys.Length == 0 ? "in either direction" : MouseName(r.Keys))}",
            EventKind.InputIdle => $"no input for {time} s", EventKind.ActivityResumed => $"input resumes after {time} s", EventKind.Timer => $"elapsed: {time} s",
            _ => "the active application changed"
        };
        var action = r.Action switch
        {
            ActionKind.Stop => "stop", ActionKind.Pulse => $"pulse {r.Intensity}/20 for {Duration(r.DurationSeconds)}",
            ActionKind.RateMapped => $"speed-based intensity for {Duration(r.DurationSeconds)}", _ => $"vibration {r.Intensity}/20 for {Duration(r.DurationSeconds)}"
        };
        return $"When {when}{(r.Process.Length == 0 ? "" : $" in {r.Process}")} → {action}.";
    }
    public static IReadOnlyList<RuleRecipe> Recipes { get; } =
    [
        new("Hold Space", "After 0.8 s — gentle vibration", new() { Name = "Hold Space", Event = EventKind.KeyHeld, Keys = "Space", Threshold = 800, Intensity = 5 }),
        new("Double press", "Two Space presses — pulse", new() { Name = "Double Space", Event = EventKind.DoublePress, Keys = "Space", Threshold = 400, Action = ActionKind.Pulse, Intensity = 7 }),
        new("Typing rhythm", "Fast typing — speed-based intensity", new() { Name = "Typing rhythm", Event = EventKind.TypingRateAbove, Keys = "", Threshold = 4, WindowMs = 3000, Action = ActionKind.RateMapped, Intensity = 8 }),
        new("No corrections", "20 typing keys — pulse", new() { Name = "No corrections", Event = EventKind.CleanTypingStreak, Keys = "", Threshold = 20, Action = ActionKind.Pulse, Intensity = 6 }),
        new("Back to work", "First input after 30 seconds of inactivity", new() { Name = "Back to work", Event = EventKind.ActivityResumed, Keys = "", Threshold = 30000, Intensity = 4 }),
        new("Every 30 seconds", "Periodic gentle feedback", new() { Name = "Every 30 seconds", Event = EventKind.Timer, Keys = "", Threshold = 30000, Intensity = 4 }),
        new("Keyboard feedback", "Any key → 150 ms · up to 5/s · BLE", new() { Name = "Keyboard feedback", Event = EventKind.KeyDown, Keys = "", DurationSeconds = 0.15, CooldownMs = 200, Intensity = 5 })
    ];
}

public sealed class RuleSummaryConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value is Rule rule ? RulePresentation.Describe(rule) : "";
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
}
