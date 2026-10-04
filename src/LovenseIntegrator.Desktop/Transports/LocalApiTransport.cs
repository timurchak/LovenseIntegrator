using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using LovenseIntegrator.Core;

namespace LovenseIntegrator.Desktop.Transports;

public sealed class LocalApiTransport : IToyTransport
{
    private readonly HttpClient client;
    private readonly Uri endpoint;
    public string Name => "Lovense Remote · Local API";
    public LocalApiTransport(string url, HttpMessageHandler? handler = null)
    {
        endpoint = new Uri(url);
        if (endpoint.Scheme != "https" && !(endpoint.Scheme == "http" && endpoint.IsLoopback))
            throw new ArgumentException("Use HTTPS or HTTP on localhost.");
        if (!string.IsNullOrEmpty(endpoint.UserInfo)) throw new ArgumentException("The URL must not contain a username or password.");
        client = handler is null ? new() : new(handler);
        client.Timeout = TimeSpan.FromSeconds(3);
        client.DefaultRequestHeaders.Add("X-platform", "LovenseIntegrator");
    }
    private async Task<JsonDocument> PostAsync(object body, CancellationToken ct)
    {
        using var response = await client.PostAsJsonAsync(endpoint, body, ct);
        response.EnsureSuccessStatusCode();
        var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        if (!document.RootElement.TryGetProperty("code", out var code) || code.GetInt32() != 200)
        {
            var resultCode = code.ValueKind == JsonValueKind.Number ? code.GetInt32().ToString() : "missing";
            document.Dispose();
            throw new InvalidOperationException($"Lovense Remote rejected the command (code {resultCode}).");
        }
        return document;
    }
    public async Task<IReadOnlyList<Toy>> DiscoverAsync(CancellationToken ct)
    {
        using var response = await PostAsync(new { command = "GetToys" }, ct);
        var element = response.RootElement.GetProperty("data").GetProperty("toys");
        using var toys = JsonDocument.Parse(element.ValueKind == JsonValueKind.String ? element.GetString()! : element.GetRawText());
        return toys.RootElement.EnumerateObject().Select(p =>
        {
            var t = p.Value;
            string Text(string name) => t.TryGetProperty(name, out var v) ? v.ToString() : "";
            return new Toy(p.Name, string.IsNullOrWhiteSpace(Text("nickName")) ? Text("name") : Text("nickName"),
                Text("status") == "1", int.TryParse(Text("battery"), out var battery) ? battery : null);
        }).ToArray();
    }
    public async Task SendAsync(ToyCommand command, CancellationToken ct)
    {
        command.Validate();
        if (command.RenewalId.Length > 0) throw new ArgumentException("While scrolling requires direct Bluetooth. For REST, choose a single effect longer than 1 second.");
        if (command.Kind != ActionKind.Stop && command.DurationSeconds <= 1)
            throw new ArgumentException("Lovense Remote Local API requires a duration longer than 1 second. Use direct Bluetooth for short feedback.");
        var body = new Dictionary<string, object>
        {
            ["command"] = "Function", ["action"] = command.Kind == ActionKind.Stop ? "Stop" : $"Vibrate:{command.Intensity}",
            ["timeSec"] = command.Kind == ActionKind.Stop ? 0 : command.DurationSeconds,
            ["apiVer"] = 1, ["stopPrevious"] = 1
        };
        if (!string.IsNullOrEmpty(command.ToyId)) body["toy"] = command.ToyId;
        if (command.Kind == ActionKind.Pulse)
        {
            body["command"] = "Pattern"; body["rule"] = $"V:1;F:v;S:{command.PulseMs}#";
            body["strength"] = $"{command.Intensity};0"; body["apiVer"] = 2;
            body.Remove("action"); body.Remove("stopPrevious");
        }
        using var response = await PostAsync(body, ct);
    }
    public ValueTask DisposeAsync() { client.Dispose(); return ValueTask.CompletedTask; }
}
