using System.ComponentModel;
using LovenseIntegrator.Core;

namespace LovenseIntegrator.Desktop.ViewModels;

public sealed class RuleEditor : INotifyPropertyChanged
{
    private readonly Rule model;
    private double durationSliderMaximum;
    public event PropertyChangedEventHandler? PropertyChanged;
    public RuleEditor(Rule rule) { model = rule.Copy(); durationSliderMaximum = Math.Max(30, model.DurationSeconds); }
    public Rule Copy() => model.Copy();
    public string? Validate() => model.Validate();
    private void Notify() => PropertyChanged?.Invoke(this, new(""));
    public string Name { get => model.Name; set { model.Name = value; Notify(); } }
    public bool Enabled { get => model.Enabled; set { model.Enabled = value; Notify(); } }
    public EventKind Event { get => model.Event; set { model.Event = value; Notify(); } }
    public string Keys { get => model.Keys; set { if (value is null) return; model.Keys = value; Notify(); } }
    public double Threshold
    {
        get => model.Threshold;
        set
        {
            model.Threshold = value;
            if (Event is EventKind.DoublePress or EventKind.TriplePress or EventKind.MouseDoubleClick && double.IsFinite(value) && value > 0 && value <= 60000)
                model.WindowMs = Math.Max(model.WindowMs, (int)Math.Ceiling(value));
            Notify();
        }
    }
    public int WindowMs { get => model.WindowMs; set { model.WindowMs = value; Notify(); } }
    public int CooldownMs { get => model.CooldownMs; set { model.CooldownMs = value; Notify(); } }
    public bool WheelContinuous { get => model.WheelContinuous; set { model.WheelContinuous = value; if (value) model.Action = ActionKind.Vibrate; Notify(); } }
    public int WheelIdleMs { get => model.WheelIdleMs; set { model.WheelIdleMs = value; Notify(); } }
    public bool AllowMouseEffects => !WheelContinuous;
    public double MousePreviewDuration => WheelContinuous ? WheelIdleMs / 1000.0 : DurationSeconds;
    public string Process { get => model.Process; set { if (value is null) return; model.Process = value; Notify(); } }
    public string WindowTitleContains { get => model.WindowTitleContains; set { model.WindowTitleContains = value; Notify(); } }
    public double DurationMilliseconds { get => DurationSeconds * 1000; set => DurationSeconds = value / 1000; }
    public int Priority { get => model.Priority; set { model.Priority = value; Notify(); } }
    public ActionKind Action { get => model.Action; set { model.Action = value; Notify(); } }
    public int Intensity { get => model.Intensity; set { model.Intensity = value; Notify(); } }
    public double DurationSeconds { get => model.DurationSeconds; set { model.DurationSeconds = value; if (double.IsFinite(value)) durationSliderMaximum = Math.Max(durationSliderMaximum, value); Notify(); } }
    public double DurationSliderMaximum => durationSliderMaximum;
    public int PulseMs { get => model.PulseMs; set { model.PulseMs = value; Notify(); } }
    public string ToyId { get => model.ToyId; set { if (value is null) return; model.ToyId = value; Notify(); } }
    public double ThresholdDisplay { get => Option.TimeThreshold ? Threshold / 1000 : Threshold; set { Threshold = Option.TimeThreshold ? value * 1000 : value; } }
    public double WindowSeconds { get => WindowMs / 1000.0; set { WindowMs = ToMilliseconds(value); } }
    public double CooldownSeconds { get => CooldownMs / 1000.0; set { CooldownMs = ToMilliseconds(value); } }
    public double PulseSeconds { get => PulseMs / 1000.0; set { PulseMs = ToMilliseconds(value); } }
    private static int ToMilliseconds(double value)
    {
        if (!double.IsFinite(value) || value < 0 || value > int.MaxValue / 1000.0) throw new ArgumentException("Enter a valid duration in seconds.");
        return checked((int)Math.Round(value * 1000));
    }
    public EventOption Option => RulePresentation.Event(Event);
    public string EventTitle => Option.Label;
    public string EventCategory => Option.Category;
    public string EventHelp => Option.Help;
    public string Summary => RulePresentation.Describe(model);
    public bool ShowKeyboard => Option.UsesKeys && Option.Category == "Keyboard";
    public bool ShowMouse => Option.UsesKeys && Option.Category == "Mouse" && Event != EventKind.MouseWheel;
    public bool ShowWheel => Event == EventKind.MouseWheel;
    public bool ShowThreshold => Option.UsesThreshold;
    public bool ShowWindow => Option.UsesWindow;
    public bool AllowAnyKey => Event is not (EventKind.Chord or EventKind.Sequence);
    public bool ShowEffect => Action != ActionKind.Stop;
    public bool ShowPulse => Action == ActionKind.Pulse;
    public bool ShowMapping => Action == ActionKind.RateMapped;
    public bool SupportsSpeedMapping => Event is EventKind.TypingRateAbove or EventKind.TypingRateBelow;
    public bool ShowKeyDetails => ShowKeyboard;
    public string[] KeyCaps => Keys.Length == 0 ? ["Any key"] : model.KeyList().Select(RulePresentation.KeyName).ToArray();
    public string ThresholdLabel => Event switch
    {
        EventKind.KeyHeld or EventKind.MouseHeld => "Hold for at least · seconds",
        EventKind.KeyReleasedAfterHold => "Release after · seconds",
        EventKind.DoublePress or EventKind.TriplePress or EventKind.MouseDoubleClick => "Count presses over · seconds",
        EventKind.PressCount => "Number of presses", EventKind.CleanTypingStreak => "Consecutive typing keys",
        EventKind.TypingRateAbove or EventKind.TypingRateBelow => "Speed · presses per second",
        EventKind.Timer => "Repeat every · seconds", _ => "Minimum break · seconds"
    };
    public string IntensityText => $"{Intensity}/20";
    public string DurationText => RulePresentation.Duration(DurationSeconds);
    public string PulseText => $"{RulePresentation.Number(PulseSeconds)} s on / off";
    public string TargetText => ToyId.Length == 0 ? "All connected toys" : "Selected toy";

    public void ChooseEvent(EventKind value)
    {
        var wasMouse = Option.Category == "Mouse";
        model.Event = value;
        if (model.Action == ActionKind.RateMapped && !SupportsSpeedMapping) model.Action = ActionKind.Vibrate;
        if (Option.Category == "Mouse" && !wasMouse) model.Keys = value == EventKind.MouseWheel ? "" : "Left";
        else if (Option.Category == "Keyboard" && wasMouse) model.Keys = "Space";
        if (!Option.UsesKeys) model.Keys = "";
        if (value == EventKind.MouseWheel && model.Keys is not ("" or "Up" or "Down")) model.Keys = "";
        if (Option.Category == "Mouse" && value != EventKind.MouseWheel && model.Keys is not ("" or "Left" or "Right" or "Middle" or "X1" or "X2")) model.Keys = "Left";
        if (Option.Category == "Keyboard" && value is not (EventKind.Chord or EventKind.Sequence) && model.KeyList().Length > 1) model.Keys = "Space";
        if (value == EventKind.Chord && model.KeyList().Length < 2) model.Keys = "LeftCtrl+Space";
        if (value == EventKind.Sequence && model.KeyList().Length < 2) model.Keys = "A,B,C";
        model.Threshold = value switch
        {
            EventKind.DoublePress or EventKind.MouseDoubleClick => 400, EventKind.TriplePress => 600,
            EventKind.PressCount => 5, EventKind.CleanTypingStreak => 20,
            EventKind.TypingRateAbove or EventKind.TypingRateBelow => 4,
            EventKind.Timer => 30000, EventKind.InputIdle or EventKind.ActivityResumed => 5000, _ => 800
        };
        model.WindowMs = 2000;
        Notify();
    }
}
