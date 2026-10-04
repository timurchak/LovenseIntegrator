using LovenseIntegrator.Core;

namespace LovenseIntegrator.Desktop.Transports;

public sealed class DemoTransport : IToyTransport
{
    public string Name => "Demo mode";
    public Task<IReadOnlyList<Toy>> DiscoverAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<Toy>>(
        [new("demo-lush", "Lush (demo)", true, 86), new("demo-ferri", "Ferri (demo)", true, 72)]);
    public Task SendAsync(ToyCommand command, CancellationToken ct) { ct.ThrowIfCancellationRequested(); command.Validate(); return Task.CompletedTask; }
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
