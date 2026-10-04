namespace LovenseIntegrator.Core;

public sealed record KeyRecordingResult(string Keys, bool Complete = false, bool Cancelled = false);

// Records key identities only; does not translate input into text.
public sealed class KeyRecording(EventKind mode)
{
    private readonly List<string> order = [];
    private readonly HashSet<string> held = new(StringComparer.OrdinalIgnoreCase);
    private string Keys => string.Join(mode == EventKind.Sequence ? "," : "+", order);
    public KeyRecordingResult Down(string key, bool repeat = false)
    {
        if (repeat || held.Contains(key)) return new(Keys);
        if (mode is not (EventKind.Chord or EventKind.Sequence)) return new(key, Complete: true);
        if (key == "Escape") return new(Keys, Cancelled: true);
        if (mode == EventKind.Sequence && key is "Return" or "Enter") return order.Count >= 2 ? new(Keys, Complete: true) : new(Keys);
        held.Add(key);
        if (mode == EventKind.Sequence || !order.Contains(key, StringComparer.OrdinalIgnoreCase)) order.Add(key);
        return new(Keys, Complete: mode == EventKind.Sequence && order.Count >= 12);
    }
    public KeyRecordingResult Up(string key)
    {
        held.Remove(key);
        if (mode != EventKind.Chord || held.Count != 0) return new(Keys);
        return order.Count >= 2 ? new(Keys, Complete: true) : new(Keys, Cancelled: true);
    }
}
