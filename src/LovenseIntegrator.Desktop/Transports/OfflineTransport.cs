using LovenseIntegrator.Core;

namespace LovenseIntegrator.Desktop.Transports;

public sealed class OfflineTransport : IToyTransport
{
    public string Name => "Not connected";
    public Task<IReadOnlyList<Toy>> DiscoverAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<Toy>>([]);
    public Task SendAsync(ToyCommand command, CancellationToken ct) => command.Kind == ActionKind.Stop
        ? Task.CompletedTask : Task.FromException(new InvalidOperationException("Connect devices first."));
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
