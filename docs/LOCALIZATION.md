# Interface languages

Version 0.2.0 adds English and Russian in Settings → App language. Save writes `%LOCALAPPDATA%/LovenseIntegrator/settings.json` atomically. Normal startup loads the preference before constructing any views or view models. Missing, malformed or unsupported preferences fall back to English. Changes apply at the next normal launch; saving does not interrupt devices, recreate windows, enable rules or save the profile. Settings are outside the installation folder and survive updates/uninstall.

`Localization/L.cs` implements lookup and formatted messages; `Localization/ru.json` is an embedded English-to-Russian catalog. English source text is the key and fallback. XAML uses `{loc:Tr Text='English label'}`. C# uses `L.T("English label")` or `L.F($"Version {version} is ready. Restart to update.")`. The latter translates the format before substituting arguments. Keep placeholders, alignment and format specifications identical in each translation. Read language once per process; do not offer live switching without rebuilding cached event catalogs and bindings safely.

Translate only developer-owned display text. Never translate enum names, key/button IDs, Tag values, bindings, process filters, device IDs, filenames, JSON fields or user-authored rule names. Keycaps retain physical keyboard labels. Existing/imported names and embedded AI instructions remain unchanged. Low-level SDK, transport and validation diagnostics may remain English; main views, event descriptions and app/update statuses are localized. Numeric input culture remains independent from UI language.

Verification:

- Console checks cover persistence, corrupt/unknown settings, fallback, placeholder integrity, format parsing and untouched interpolated user content.
- `Test-UiHarness.ps1` runs the full WPF harness for both `en`/`ru` UI languages under both `en-US`/`ru-RU` numeric cultures. Artifacts live in `artifacts/ui-harness/<language>/<culture>/`.
- Settings checks use real controls and an isolated settings file next to the test profile. They compare profile bytes and rule snapshots before/after saving and verify paused rules. Never write tests to the user's real settings or profile.
