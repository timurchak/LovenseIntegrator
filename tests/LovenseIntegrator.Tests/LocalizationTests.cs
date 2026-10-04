using System.Globalization;
using System.Text.RegularExpressions;
using LovenseIntegrator.Desktop.Localization;

static class LocalizationTests
{
    public static void Run(Action<bool, string> check)
    {
        var directory = Path.Combine(Path.GetTempPath(), "Lovense-language-" + Guid.NewGuid());
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "settings.json");
        try
        {
            check(LanguagePreferences.Read(path) == "en", "missing language preference defaults to English");
            LanguagePreferences.Save(path, "ru");
            check(LanguagePreferences.Read(path) == "ru", "Russian language preference persists");
            LanguagePreferences.Save(path, "en");
            check(LanguagePreferences.Read(path) == "en", "switching back to English persists");
            File.WriteAllText(path, "{invalid");
            check(LanguagePreferences.Read(path) == "en", "corrupt language settings do not block startup");
            File.WriteAllText(path, "{\"Language\":\"unsupported\"}");
            check(LanguagePreferences.Read(path) == "en", "unknown saved language falls back to English");
            var before = File.ReadAllText(path);
            try { LanguagePreferences.Save(path, "de"); check(false, "unsupported language must be rejected"); }
            catch (ArgumentException) { check(File.ReadAllText(path) == before, "unsupported language cannot overwrite preferences"); }
            L.SetLanguage("ru");
            check(L.T("Save assignment") == "Сохранить назначение", "Russian catalog is embedded");
            var userName = "My rule {0} клавиша";
            check(L.F($"Rule «{userName}» saved.") == "Правило «My rule {0} клавиша» сохранено.", "formatted translations preserve user text and braces");
            check(L.T("Future untranslated message") == "Future untranslated message", "missing catalog entries fall back to English");
            check(L.Catalog.All(pair => !string.IsNullOrWhiteSpace(pair.Value)), "catalog translations are nonempty");
            static string Fields(string text) => string.Join(";", Regex.Matches(text, @"\{\d+(?:[^}]*)\}").Select(m => m.Value).Order());
            check(L.Catalog.All(pair => Fields(pair.Key) == Fields(pair.Value)), "every translation preserves format placeholders");
            foreach (var pair in L.Catalog) _ = string.Format(CultureInfo.InvariantCulture, pair.Value, Enumerable.Repeat<object>(1, 8).ToArray());
            check(true, "every translated format parses successfully");
            L.SetLanguage("en");
            check(L.T("Save assignment") == "Save assignment", "English catalog leaves display strings unchanged");
        }
        finally { L.SetLanguage("en"); Directory.Delete(directory, true); }
    }
}
