namespace LovenseIntegrator.Core;

public sealed record Toy(string Id, string Name, bool Connected, int? Battery = null)
{
    public override string ToString() => $"{Name} · {(Connected ? "connected" : "disconnected")}{(Battery is null ? "" : $" · {Battery}%")}";
}

public sealed record ToyCommand(ActionKind Kind, int Intensity, double DurationSeconds, int PulseMs = 500, string ToyId = "", string RenewalId = "")
{
    public static ToyCommand Stop => new(ActionKind.Stop, 0, 0);
    public void Validate()
    {
        if (!Enum.IsDefined(Kind)) throw new ArgumentException("Unknown action.");
        if (RenewalId.Length > 0 && Kind != ActionKind.Vibrate) throw new ArgumentException("Scroll renewal only supports vibration.");
        if (Kind == ActionKind.Stop) return;
        if (Intensity is < 0 or > 20 || !double.IsFinite(DurationSeconds) || DurationSeconds is < 0.1 or > 300)
            throw new ArgumentException("Intensity 0–20, duration 0.1–300 seconds.");
        if (Kind == ActionKind.Pulse && PulseMs is < 150 or > 10000) throw new ArgumentException("Pulse interval: 150–10000 ms.");
    }
}

public interface IToyTransport : IAsyncDisposable
{
    string Name { get; }
    Task<IReadOnlyList<Toy>> DiscoverAsync(CancellationToken ct);
    Task SendAsync(ToyCommand command, CancellationToken ct);
}
