using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using LovenseIntegrator.Core;

namespace LovenseIntegrator.Desktop.Services;

public sealed class Profile
{
    public int Version { get; set; } = 3;
    public string ApiUrl { get; set; } = "https://127-0-0-1.lovense.club:30010/command";
    public List<Rule> Rules { get; set; } = [];
}

public static class ProfileStore
{
    internal static string? OverridePath { get; set; }
    public static string PathName => OverridePath ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LovenseIntegrator", "profile.json");
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true, Converters = { new JsonStringEnumConverter() } };
    public static Profile Load(string? path = null)
    {
        path ??= PathName;
        if (!File.Exists(path)) return Defaults();
        var profile = JsonSerializer.Deserialize<Profile>(File.ReadAllText(path), Options) ?? throw new InvalidDataException("The profile is empty.");
        if (profile.Version is not (1 or 2 or 3) || profile.Rules is null || profile.ApiUrl is null || profile.Rules.Any(r => r is null)) throw new InvalidDataException("Unsupported profile format.");
        if (profile.Rules.Select(r => r.Id).Distinct().Count() != profile.Rules.Count) throw new InvalidDataException("The profile contains duplicate rule IDs.");
        foreach (var rule in profile.Rules) if (rule.Validate() is { } error) throw new InvalidDataException($"{rule.Name}: {error}");
        profile.Version = 3;
        return profile;
    }
    public static void Save(Profile profile, string? path = null)
    {
        path ??= PathName;
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        var temp = path + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(profile, Options));
        File.Move(temp, path, overwrite: true);
    }
    public static Profile Defaults() => new()
    {
        Rules =
        [
            new() { Name = "Space → gentle pulse", Event = EventKind.KeyDown, Keys = "Space", Intensity = 5 },
            new() { Name = "Hold W → pulse", Event = EventKind.KeyHeld, Keys = "W", Threshold = 800, Action = ActionKind.Pulse, Intensity = 8 },
            new() { Name = "Fast typing → speed-based intensity", Event = EventKind.TypingRateAbove, Keys = "", Threshold = 4, Action = ActionKind.RateMapped, Intensity = 12 }
        ]
    };
}
