using System.Globalization;
using System.IO;
using System.Text.Json;

namespace LovenseIntegrator.Desktop.Localization;

// Only developer-owned display strings pass through this catalog. Never translate IDs or user content.
public static class L
{
    private static readonly Dictionary<string, string> Russian = LoadCatalog();
    public static string Language { get; private set; } = "en";
    public static IReadOnlyDictionary<string, string> Catalog => Russian;
    public static void SetLanguage(string language) => Language = language == "ru" ? "ru" : "en";
    public static string T(string english) => Language == "ru" && Russian.TryGetValue(english, out var translated) ? translated : english;
    public static string F(FormattableString text) => string.Format(CultureInfo.CurrentCulture, T(text.Format), text.GetArguments());
    private static Dictionary<string, string> LoadCatalog()
    {
        using var stream = typeof(L).Assembly.GetManifestResourceStream("Localization.ru.json")
            ?? throw new InvalidOperationException("Missing embedded Russian translation catalog.");
        return JsonSerializer.Deserialize<Dictionary<string, string>>(stream)!;
    }
}

public static class LanguagePreferences
{
    private sealed record Preferences(string Language);
    public static string Read(string path)
    {
        try
        {
            var value = JsonSerializer.Deserialize<Preferences>(File.ReadAllText(path));
            return value?.Language == "ru" ? "ru" : "en";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException) { return "en"; }
    }
    public static void Save(string path, string language)
    {
        if (language is not ("en" or "ru")) throw new ArgumentException("Unsupported interface language.", nameof(language));
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        // Replace atomically; an interrupted save must not truncate existing preferences.
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try { File.WriteAllText(temporary, JsonSerializer.Serialize(new Preferences(language))); File.Move(temporary, path, true); }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}
