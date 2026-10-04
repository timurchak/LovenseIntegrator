using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using LovenseIntegrator.Core;

namespace LovenseIntegrator.Desktop.Services;

public sealed class ScreenPreset
{
    [JsonRequired] public string Format { get; set; } = "LovenseIntegrator.Screen";
    [JsonRequired] public int Version { get; set; } = 1;
    [JsonRequired] public List<Rule> Rules { get; set; } = [];
}
public static class ScreenPresetStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true, UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        Converters = { new JsonStringEnumConverter(allowIntegerValues: false) }
    };
    public static ScreenPreset Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        Duplicates(document.RootElement);
        var preset = JsonSerializer.Deserialize<ScreenPreset>(json, Options) ?? throw new InvalidDataException("Empty Screen bundle.");
        if (preset.Format != "LovenseIntegrator.Screen" || preset.Version != 1 || preset.Rules is null) throw new InvalidDataException("Expected LovenseIntegrator.Screen version 1.");
        foreach (var rule in document.RootElement.GetProperty("Rules").EnumerateArray())
            foreach (var field in new[] { "Id", "Name", "Enabled", "Event", "ScreenEventId", "Action", "Intensity", "DurationSeconds", "PulseMs", "CooldownMs", "Priority", "ToyId", "Process", "WindowTitleContains" })
                if (rule.ValueKind != JsonValueKind.Object || !rule.TryGetProperty(field, out _)) throw new InvalidDataException($"Missing required field {field}.");
        Validate(preset.Rules); return preset;
    }
    private static void Duplicates(JsonElement node)
    {
        if (node.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in node.EnumerateObject()) { if (!names.Add(item.Name)) throw new InvalidDataException("Duplicate field: " + item.Name); Duplicates(item.Value); }
        }
        if (node.ValueKind == JsonValueKind.Array) foreach (var item in node.EnumerateArray()) Duplicates(item);
    }
    public static void Validate(IReadOnlyCollection<Rule> rules)
    {
        if (rules.Count == 0) throw new InvalidDataException("No Screen assignments.");
        var ids = new HashSet<Guid>();
        foreach (var rule in rules)
        {
            if (rule is null || rule.Id == Guid.Empty || !ids.Add(rule.Id)) throw new InvalidDataException("Invalid or duplicate rule ID.");
            if (rule.Event != EventKind.ScreenEvent || rule.KeyboardLayer || rule.MouseLayer || rule.WheelContinuous) throw new InvalidDataException("Expected Screen event assignments only.");
            if (rule.Validate() is { } error) throw new InvalidDataException(error);
        }
    }
    public static void Save(string path, IEnumerable<Rule> rules)
    {
        var preset = new ScreenPreset { Rules = rules.Select(r => r.Copy()).ToList() };
        Validate(preset.Rules); File.WriteAllText(path, JsonSerializer.Serialize(preset, Options));
    }
    public static List<Rule> Merge(IEnumerable<Rule> existing, IEnumerable<Rule> incoming)
    {
        var merged = existing.Select(r => r.Copy()).ToList();
        foreach (var rule in incoming)
        {
            var index = merged.FindIndex(r => r.Id == rule.Id);
            if (index >= 0 && merged[index].Event != EventKind.ScreenEvent) throw new InvalidDataException("The rule ID belongs to another mode.");
            if (index < 0) merged.Add(rule.Copy()); else merged[index] = rule.Copy();
        }
        return merged;
    }
}
