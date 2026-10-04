using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using LovenseIntegrator.Core;

namespace LovenseIntegrator.Desktop.Services;

public sealed class KeyboardPreset
{
    [JsonRequired] public string Format { get; set; } = "LovenseIntegrator.Keyboard";
    [JsonRequired] public int Version { get; set; } = 1;
    [JsonRequired] public List<Rule> Rules { get; set; } = [];
}

public static class KeyboardPresetStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true, UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter(allowIntegerValues: false) }
    };
    private static readonly string[] RequiredRuleFields = ["Id", "Name", "Enabled", "Event", "Keys", "ExcludedKeys", "KeyboardLayer", "Action", "Intensity", "DurationSeconds", "CooldownMs", "Process", "WindowTitleContains", "ToyId", "Priority", "PulseMs"];
    public static bool CanEdit(Rule r) => r.Event == EventKind.KeyDown && r.Action != ActionKind.RateMapped;
    public static KeyboardPreset Load(string path, IEnumerable<string> knownKeys) => Parse(File.ReadAllText(path), knownKeys);
    public static KeyboardPreset Parse(string json, IEnumerable<string> knownKeys)
    {
        using var document = JsonDocument.Parse(json);
        RejectDuplicateProperties(document.RootElement);
        var preset = JsonSerializer.Deserialize<KeyboardPreset>(json, Options) ?? throw new InvalidDataException("The assignment file is empty.");
        if (preset.Format != "LovenseIntegrator.Keyboard" || preset.Version != 1 || preset.Rules is null || preset.Rules.Count == 0)
            throw new InvalidDataException("Expected a non-empty LovenseIntegrator.Keyboard version 1 bundle.");
        foreach (var element in document.RootElement.GetProperty("Rules").EnumerateArray())
            foreach (var field in RequiredRuleFields)
                if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(field, out _)) throw new InvalidDataException($"Assignment is missing the required field {field}.");
        Validate(preset.Rules, knownKeys);
        return preset;
    }
    private static void RejectDuplicateProperties(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in element.EnumerateObject())
            {
                if (!names.Add(property.Name)) throw new InvalidDataException($"Field {property.Name} is specified more than once.");
                RejectDuplicateProperties(property.Value);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array) foreach (var item in element.EnumerateArray()) RejectDuplicateProperties(item);
    }
    public static void Validate(IEnumerable<Rule> rules, IEnumerable<string> knownKeys)
    {
        var ids = new HashSet<Guid>(); var keys = new HashSet<string>(knownKeys, StringComparer.OrdinalIgnoreCase);
        foreach (var r in rules)
        {
            if (r is null) throw new InvalidDataException("Empty assignment.");
            if (r.Id == Guid.Empty || !ids.Add(r.Id)) throw new InvalidDataException("Assignment IDs must be unique and non-empty.");
            if (!r.KeyboardLayer || !CanEdit(r)) throw new InvalidDataException($"{r.Name}: requires KeyboardLayer=true, Event=KeyDown and Vibrate, Pulse or Stop.");
            if (r.Validate() is { } error) throw new InvalidDataException($"{r.Name}: {error}");
            foreach (var key in r.KeyList().Concat(r.ExcludedKeys.Split([',', '+'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)))
                if (!keys.Contains(key)) throw new InvalidDataException($"{r.Name}: unknown key «{key}». Use the codes from the AI instructions.");
            if (!string.IsNullOrWhiteSpace(r.Keys) && !string.IsNullOrWhiteSpace(r.ExcludedKeys)) throw new InvalidDataException($"{r.Name}: exclusions require Keys=\"\" (all keys).");
            if (r.KeyList().Length == 0 && !string.IsNullOrWhiteSpace(r.Keys) || !keys.Any(r.MatchesKeyboardKey)) throw new InvalidDataException($"{r.Name}: no keys selected.");
        }
    }
    public static void Save(string path, IEnumerable<Rule> rules, IEnumerable<string> knownKeys)
    {
        var preset = new KeyboardPreset { Rules = rules.Select(r => { var copy = r.Copy(); copy.KeyboardLayer = true; return copy; }).ToList() };
        if (preset.Rules.Count == 0) throw new InvalidDataException("No saved assignments to export.");
        Validate(preset.Rules, knownKeys);
        var json = JsonSerializer.Serialize(preset, Options);
        File.WriteAllText(path, json);
    }
    public static List<Rule> Merge(IEnumerable<Rule> existing, IEnumerable<Rule> incoming)
    {
        var merged = existing.Select(r => r.Copy()).ToList();
        foreach (var rule in incoming)
        {
            var index = merged.FindIndex(r => r.Id == rule.Id);
            if (index >= 0 && !CanEdit(merged[index])) throw new InvalidDataException($"Assignment Id «{rule.Name}» belongs to another event type. Use a new Id.");
            if (index < 0) merged.Add(rule.Copy()); else merged[index] = rule.Copy();
        }
        return merged;
    }
}
