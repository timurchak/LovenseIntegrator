using LovenseIntegrator.Desktop.Localization;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using LovenseIntegrator.Core;
using LovenseIntegrator.Desktop.Services;
using LovenseIntegrator.Desktop.ViewModels;

namespace LovenseIntegrator.Desktop;

public partial class MainWindow
{
    // Real WPF controls/bindings and routed events, isolated profile and demo transport only.
    // No global hooks, Bluetooth discovery, desktop input injection, or file dialogs.
    internal async Task RunHarnessAsync(string directory)
    {
        Modes.SelectedItem = RulesTab;
        var checks = 0;
        var groups = new List<object>();
        var content = (FrameworkElement)Content;
        void Check(bool passed, string name)
        {
            if (!passed) throw new InvalidOperationException($"UI harness [{CultureInfo.CurrentCulture.Name}]: {name}");
            checks++;
        }
        async Task Group(string name, Func<Task> run)
        {
            var before = checks;
            await run();
            groups.Add(new { Name = name, Checks = checks - before, Result = "PASS" });
        }
        async Task Layout(double width = 1360, double height = 900)
        {
            await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
            content.Measure(new Size(width, height)); content.Arrange(new Rect(0, 0, width, height)); content.UpdateLayout();
        }
        static string Snapshot(Rule rule) => JsonSerializer.Serialize(rule);
        static IEnumerable<T> Descendants<T>(DependencyObject root) where T : DependencyObject
        {
            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                if (child is T match) yield return match;
                foreach (var item in Descendants<T>(child)) yield return item;
            }
        }
        Button Button(string caption) => Descendants<Button>(content).Single(b => b.Content as string == L.T(caption));
        void Click(string caption) => Button(caption).RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
        async Task StartRecording()
        {
            CaptureButton.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
            await Layout();
            Check(capturing && !vm.Running && CaptureButton.Content as string == L.T("Cancel recording"), "recording starts with rules paused");
        }
        using var keySource = new HwndSource(new HwndSourceParameters("Lovense UI harness keys") { Width = 1, Height = 1, PositionX = -32000, PositionY = -32000, WindowStyle = 0 });
        void SendKey(Key key, bool down = true)
        {
            var args = new KeyEventArgs(Keyboard.PrimaryDevice, keySource, Environment.TickCount, key)
            { RoutedEvent = down ? Keyboard.PreviewKeyDownEvent : Keyboard.PreviewKeyUpEvent };
            RaiseEvent(args);
            Check(args.Handled, $"recorded routed {key} {(down ? "down" : "up")}");
        }
        var bindingOutput = new StringWriter();
        using var listener = new TextWriterTraceListener(bindingOutput);
        var trace = PresentationTraceSources.DataBindingSource;
        var oldLevel = trace.Switch.Level; trace.Switch.Level = SourceLevels.Error;
        trace.Listeners.Add(listener);
        try
        {
            await vm.DiscoverAsync(); await Layout();
            Check(vm.ConnectionName == L.T("Demo mode") && vm.Toys.Count == 2, "demo transport only");
            await Group("Language settings and profile preservation", async () =>
            {
                var profileBefore = File.ReadAllBytes(ProfileStore.PathName);
                var rulesBefore = vm.Rules.Select(Snapshot).ToArray();
                Modes.SelectedItem = SettingsTab; await Layout(1120, 720);
                Check((string)SettingsTab.Header == L.T("  Settings  "), "settings tab uses chosen interface language");
                Check((string)KeyboardTab.Header == L.T("  Keyboard  "), "keyboard tab uses chosen interface language");
                Check(RulePresentation.Event(EventKind.MouseDown).Category == L.T("Mouse"), "localized catalog categories retain event identities");
                LanguageSelector.SelectedValue = L.Language == "ru" ? "en" : "ru";
                SaveLanguageButton.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
                Check(LanguagePreferences.Read(LanguageSettingsPath) == (string)LanguageSelector.SelectedValue, "language saved through real settings controls");
                Check(LanguageSaveStatus.Text == L.T("Language saved. Restart the app when you are ready."), "restart requirement is visible");
                Check(File.ReadAllBytes(ProfileStore.PathName).SequenceEqual(profileBefore) && vm.Rules.Select(Snapshot).SequenceEqual(rulesBefore), "language changes preserve profile bytes and live rules");
                Check(!vm.Running, "saving language does not enable rules");
                LanguageSelector.SelectedValue = L.Language;
                SaveLanguageButton.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
                await Layout(1120, 720);
                var bitmap = new RenderTargetBitmap(1120, 720, 96, 96, PixelFormats.Pbgra32);
                var background = new DrawingVisual(); using (var dc = background.RenderOpen()) dc.DrawRectangle(Background, null, new Rect(0, 0, 1120, 720));
                bitmap.Render(background);
                bitmap.Render(content);
                var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
                using var output = File.Create(Path.Combine(directory, "settings-1120.png")); encoder.Save(output);
                Modes.SelectedItem = RulesTab; await Layout();
            });
            await Group("Updates tab and development-build guard", async () =>
            {
                Modes.SelectedItem = UpdatesTab; await Layout(1120, 720);
                Check(ReferenceEquals(UpdatesPanel.DataContext, updates), "updates has its own binding context");
                Check(!updates.CanCheck && !CheckUpdatesButton.IsEnabled, "development builds cannot download updates");
                Check(!updates.CanRestart && !RestartUpdateButton.IsEnabled, "restart requires a verified package");
                Check(UpdateStatus.Text.Contains(L.T("Development build")), "uninstalled state is explained");
                Check(!vm.Running, "updates view keeps rules paused");
                var bitmap = new RenderTargetBitmap(1120, 720, 96, 96, PixelFormats.Pbgra32);
                var background = new DrawingVisual(); using (var dc = background.RenderOpen()) dc.DrawRectangle(Background, null, new Rect(0, 0, 1120, 720));
                bitmap.Render(background);
                bitmap.Render(content);
                var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
                using var image = File.Create(Path.Combine(directory, "updates-1120.png")); encoder.Save(image);
                Modes.SelectedItem = RulesTab; await Layout();
            });
            await Group("Event catalog categories, search and empty results", () =>
            {
                checks += new EventPickerWindow(EventKind.KeyDown).VerifyHarness();
                return Task.CompletedTask;
            });
            await Group("All event transitions and action bindings", async () =>
            {
                foreach (var source in Enum.GetValues<EventKind>())
                    foreach (var target in Enum.GetValues<EventKind>())
                    {
                        vm.NewRule(); vm.Draft.ChooseEvent(source);
                        vm.Draft.ChooseEvent(target); await Layout();
                        var label = $"{source} → {target}";
                        Check(vm.Draft.Validate() is null, label + " produces valid settings");
                        Check((KeyboardSettings.Visibility == Visibility.Visible) == vm.Draft.ShowKeyboard &&
                            (ThresholdSettings.Visibility == Visibility.Visible) == vm.Draft.ShowThreshold &&
                            (WindowSettings.Visibility == Visibility.Visible) == vm.Draft.ShowWindow, label + " visible fields");
                        var effects = Descendants<RadioButton>(Editor).ToArray();
                        var mapped = effects.Single(b => b.Tag as string == "RateMapped");
                        Check(mapped.IsEnabled == vm.Draft.SupportsSpeedMapping, label + " speed action eligibility");
                        foreach (var effect in effects.Where(b => b.IsEnabled))
                        {
                            effect.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
                            await Layout();
                            var action = Enum.Parse<ActionKind>((string)effect.Tag);
                            Check(vm.Draft.Action == action && EffectGraph.Kind == action && effect.IsChecked == true &&
                                effects.Count(b => b.IsChecked == true) == 1 &&
                                (PulseSettings.Visibility == Visibility.Visible) == (action == ActionKind.Pulse) &&
                                (EffectSettings.Visibility == Visibility.Visible) == (action != ActionKind.Stop), label + " " + action + " bindings");
                        }
                    }
                vm.Draft.ChooseEvent(EventKind.TypingRateAbove); vm.Draft.Action = ActionKind.RateMapped;
                vm.Draft.ChooseEvent(EventKind.MouseDown); await Layout();
                Check(vm.Draft.Action == ActionKind.Vibrate, "changing source normalizes unsupported speed mapping");
            });
            await Group("Recipes and stable rule identity", async () =>
            {
                vm.SetProfile(ProfileStore.Defaults());
                foreach (var recipe in vm.Recipes)
                {
                    var original = Snapshot(recipe.Rule); var count = vm.Rules.Count;
                    vm.UseRecipe(recipe); await Layout();
                    Check(vm.Draft.Validate() is null && vm.Draft.Copy().Id != recipe.Rule.Id, recipe.Name + " independent draft");
                    RuleName.Text = recipe.Name + " — test"; IntensitySlider.Value = 13;
                    Click(L.T("Save")); await Layout();
                    Check(vm.Rules.Count == count + 1 && vm.SelectedRule?.Intensity == 13 && Snapshot(recipe.Rule) == original, recipe.Name + " save without changing template");
                    var identity = vm.SelectedRule!.Id;
                    RuleName.Text += " changed"; Click(L.T("Save")); await Layout();
                    Check(vm.Rules.Count == count + 1 && vm.SelectedRule!.Id == identity, "edit replaces same ID");
                }
                var removed = vm.Rules[2].Id; var survivors = vm.Rules.Where(r => r.Id != removed).Select(Snapshot).ToArray();
                vm.SelectedRule = vm.Rules[2]; Click(L.T("Delete")); await Layout();
                Check(vm.Rules.Select(Snapshot).SequenceEqual(survivors), "deleting middle row preserves every other rule");
                var persisted = ProfileStore.Load();
                Check(persisted.Rules.Select(Snapshot).SequenceEqual(survivors), "persisted deletion keeps same IDs and settings");
                vm.SelectedRule = vm.Rules.Last(); var saved = Snapshot(vm.SelectedRule);
                RuleName.Text = "Unsaved edit"; vm.SelectedRule = vm.Rules.First(); await Layout();
                Check(Snapshot(vm.Rules.Last()) == saved && RuleName.Text == vm.SelectedRule!.Name, "switch discards only isolated unsaved draft");
            });
            await Group("Routed keyboard capture and cancellation", async () =>
            {
                vm.NewRule(); vm.Draft.ChooseEvent(EventKind.Chord); var original = vm.Draft.Keys;
                await StartRecording(); SendKey(Key.LeftCtrl); SendKey(Key.Space); SendKey(Key.Space, false);
                Check(capturing, "chord waits for all releases"); SendKey(Key.LeftCtrl, false);
                Check(!capturing && vm.Draft.Keys == "LeftCtrl+Space", "chord completed through routed events");
                await StartRecording(); SendKey(Key.A); SendKey(Key.Escape);
                Check(!capturing && vm.Draft.Keys == original, "Escape restores original chord");
                await StartRecording(); SendKey(Key.B);
                CaptureButton.RaiseEvent(new KeyboardFocusChangedEventArgs(Keyboard.PrimaryDevice, 0, CaptureButton, RuleName) { RoutedEvent = Keyboard.LostKeyboardFocusEvent });
                Check(!capturing && vm.Draft.Keys == original, "losing focus restores original keys");
                await StartRecording(); SendKey(Key.C); var oldDraft = vm.Draft;
                vm.SelectedRule = vm.Rules.First(); await Layout();
                Check(!capturing && oldDraft.Keys == original && vm.Draft.Keys == vm.SelectedRule!.Keys, "changing draft cancels without altering new rule");
                vm.Draft.ChooseEvent(EventKind.Sequence); await Layout();
                await StartRecording(); SendKey(Key.A); SendKey(Key.A, false); SendKey(Key.B); SendKey(Key.B, false); SendKey(Key.A); SendKey(Key.Return);
                Check(!capturing && vm.Draft.Keys == "A,B,A", "sequence keeps order and repeated released keys");
                vm.Draft.ChooseEvent(EventKind.KeyDown); await Layout(); await StartRecording(); SendKey(Key.Escape);
                Check(!capturing && vm.Draft.Keys == "Escape", "single Escape can be a trigger");
            });
            await Group("Targets, application refresh, unavailable devices", async () =>
            {
                vm.NewRule(); vm.Draft.ToyId = "disconnected-test-id"; vm.Draft.Process = "not-running-harness-app";
                for (var i = 0; i < 3; i++) { RefreshApplications(this, EventArgs.Empty); await vm.DiscoverAsync(); await Layout(); }
                Check(vm.Draft.ToyId == "disconnected-test-id" && (string?)TargetSelector.SelectedValue == vm.Draft.ToyId, $"missing target survives repeated discovery (draft={vm.Draft.ToyId}, selector={TargetSelector.SelectedValue})");
                Check(vm.Draft.Process == "not-running-harness-app" && (string?)ApplicationSelector.SelectedValue == vm.Draft.Process, "closed application survives refresh");
                TargetSelector.SelectedValue = "demo-lush"; await Layout(); Check(vm.Draft.ToyId == "demo-lush", "target selection by stable ID");
                await vm.DiscoverAsync(); await Layout(); Check(vm.Draft.ToyId == "demo-lush", "selected available target survives discovery");
                vm.Draft.ToyId = "disconnected-test-id"; await vm.TestRuleAsync();
                Check(vm.Journal[0].Contains(L.T("The selected toy is not connected.")), "explicit test reports missing target");
                vm.ObserveOnly = true; await vm.FireAsync(new(vm.Draft.Copy(), 1));
                Check(vm.Journal[0].Contains(L.Language == "ru" ? "Наблюдение:" : "Observation:"), "observation works without connected target"); vm.ObserveOnly = false;
            });
            Check(bindingOutput.ToString().Length == 0, "no binding errors during transitions, refresh and recording: " + bindingOutput);
            // Conversion errors here are deliberate; test UI error recovery and block invalid saves.
            await Group("Numeric validation and locale recovery", async () =>
            {
                vm.NewRule(); vm.Draft.ChooseEvent(EventKind.KeyHeld); await Layout();
                foreach (var invalid in new[] { "abc", "", "NaN", "Infinity", "-1", "0" })
                {
                    ThresholdInput.Text = invalid; var before = vm.Rules.Count;
                    Click(L.T("Save")); await Layout(); Check(vm.Rules.Count == before, "cannot save threshold " + invalid);
                }
                ThresholdInput.Text = (0.85).ToString(CultureInfo.CurrentCulture); await Layout();
                Check(!Validation.GetHasError(ThresholdInput) && vm.Draft.Threshold == 850, "fractional seconds use selected language");
                ThresholdInput.Text = "invalid"; vm.Draft.ChooseEvent(EventKind.KeyDown); await Layout();
                var count = vm.Rules.Count; Click(L.T("Save")); await Layout();
                Check(vm.Rules.Count == count + 1, "irrelevant hidden invalid field does not block valid rule");
                RuleName.Text = "   "; Click(L.T("Save")); await Layout();
                Check(vm.SelectedRule!.Name != "   " && vm.Journal[0].Contains("name"), "blank name rejected");
                vm.SelectedRule = vm.Rules.First(); await Layout();
                Check(!HasErrors(Editor) && RuleName.Text == vm.SelectedRule!.Name, "switching draft clears stale validation");
                AdvancedSettings.IsExpanded = true; await Layout();
                var duration = Descendants<TextBox>(AdvancedSettings).Single(b => b.GetBindingExpression(TextBox.TextProperty)?.ParentBinding.Path.Path == "Draft.DurationSeconds");
                duration.Text = (73.5).ToString(CultureInfo.CurrentCulture); await Layout();
                Check(vm.Draft.DurationSeconds == 73.5 && vm.Draft.DurationSliderMaximum >= 73.5, "exact long fractional duration retained");
                var pulse = Descendants<Slider>(PulseSettings).Single();
                vm.Draft.Action = ActionKind.Pulse; pulse.Value = 0.35; await Layout();
                Check(vm.Draft.PulseMs == 350, "pulse interval slider uses milliseconds without drift");
                var cooldown = Descendants<TextBox>(AdvancedSettings).Single(b => b.GetBindingExpression(TextBox.TextProperty)?.ParentBinding.Path.Path == "Draft.CooldownSeconds");
                cooldown.Text = (2.75).ToString(CultureInfo.CurrentCulture); await Layout();
                Check(vm.Draft.CooldownMs == 2750, "cooldown fraction retained");
            });
            await Group("Profile roundtrip and empty rule list", async () =>
            {
                var profile = new Profile { Rules = Enum.GetValues<EventKind>().Select(kind =>
                { var editor = new RuleEditor(new Rule { Name = kind.ToString() }); editor.ChooseEvent(kind); return editor.Copy(); }).ToList() };
                var path = Path.Combine(directory, "roundtrip.json"); ProfileStore.Save(profile, path);
                await vm.ImportProfileAsync(ProfileStore.Load(path)); await Layout();
                Check(!vm.Running && vm.Rules.Select(Snapshot).SequenceEqual(profile.Rules.Select(Snapshot)), "all events roundtrip and import leaves rules paused");
                Check(ProfileStore.Load().Rules.Select(Snapshot).SequenceEqual(profile.Rules.Select(Snapshot)), "import persisted every rule");
                while (vm.Rules.Count > 0) { vm.SelectedRule = vm.Rules.Last(); Click(L.T("Delete")); }
                await Layout(); Check(vm.SelectedRule is null && ProfileStore.Load().Rules.Count == 0 && !HasErrors(Editor), "empty rule list remains editable and persists");
                Click(L.T("+ New rule")); RuleName.Text = "After deleting all"; Click(L.T("Save")); await Layout();
                Check(vm.Rules.Count == 1 && vm.Rules[0].Name == RuleName.Text, "create first rule again");
            });
            await Group("Short keyboard feedback presets and persistence", async () =>
            {
                vm.UseRecipe(vm.Recipes.Single(r => r.Name == L.T("Keyboard feedback"))); await Layout();
                Check(vm.Draft.Event == EventKind.KeyDown && vm.Draft.Keys == "" && vm.Draft.DurationSeconds == 0.15 && vm.Draft.CooldownMs == 200,
                    "feedback recipe selects physical keys, 150 ms duration and 200 ms cooldown");
                foreach (var (caption, seconds) in new[] { ("100 ms", 0.1), ("150 ms", 0.15), ("250 ms", 0.25) })
                {
                    Click(caption); await Layout();
                    Check(vm.Draft.DurationSeconds == seconds && DurationSlider.Value == seconds && EffectGraph.Duration == seconds && vm.Draft.DurationText == L.T(caption) && vm.Draft.Summary.Contains(L.T(caption)),
                        caption + " preset preserves fractional seconds and updates visible bindings");
                }
                DurationSlider.Value = 0.15; await Layout();
                Check(vm.Draft.DurationSeconds == 0.15, "duration slider retains 150 ms");
                Click(L.T("Save")); await Layout(); var savedId = vm.SelectedRule!.Id;
                Check(ProfileStore.Load().Rules.Single(r => r.Id == savedId).DurationSeconds == 0.15, "subsecond duration persisted exactly");
                await vm.TestRuleAsync();
                Check(vm.Journal[0].Contains(L.T("150 ms")) && vm.Journal[0].Contains("[demo]"), "test feedback uses demo and logs milliseconds");
                await Layout(1120, 720); EditorScroll.ScrollToTop(); await Layout(1120, 720);
                var bitmap = new RenderTargetBitmap(1120, 720, 96, 96, PixelFormats.Pbgra32);
                var background = new DrawingVisual(); using (var dc = background.RenderOpen()) dc.DrawRectangle(Background, null, new Rect(0, 0, 1120, 720));
                bitmap.Render(background); bitmap.Render(content);
                var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
                using var file = File.Create(Path.Combine(directory, "keyboard-feedback.png")); encoder.Save(file);
            });
            await Group("Minimum layout, long text and DPI raster matrix", async () =>
            {
                vm.Draft.Name = string.Join(" ", Enumerable.Repeat("Long rule name", 10));
                vm.Draft.Process = "very-long-application-name-for-layout-testing";
                vm.Draft.ChooseEvent(EventKind.Sequence); vm.Draft.Keys = string.Join(",", Enumerable.Repeat("RightShift", 12));
                vm.Draft.Action = ActionKind.Pulse; AdvancedSettings.IsExpanded = true;
                foreach (var (width, height) in new[] { (1120, 720), (1360, 900) })
                {
                    await Layout(width, height);
                    foreach (var caption in new[] { L.T("Save"), L.T("Test action"), L.T("Delete"), L.T("STOP ALL") })
                    {
                        var button = Button(caption); var bounds = button.TransformToAncestor(content).TransformBounds(new Rect(button.RenderSize));
                        Check(button.ActualWidth > 0 && bounds.Left >= 0 && bounds.Top >= 0 && bounds.Right <= width && bounds.Bottom <= height, caption + " reachable at " + width);
                    }
                    Check(EffectGraph.ActualWidth > 0 && EffectGraph.ActualHeight == 85, "preview stays visible at " + width);
                    EditorScroll.ScrollToEnd(); await Layout(width, height);
                    Check(EditorScroll.ScrollableHeight > 0 && Math.Abs(EditorScroll.VerticalOffset - EditorScroll.ScrollableHeight) < 1, "advanced settings scroll to end");
                    foreach (var scale in new[] { 1.0, 1.25, 1.5, 2.0 })
                    {
                        var bitmap = new RenderTargetBitmap((int)(width * scale), (int)(height * scale), 96 * scale, 96 * scale, PixelFormats.Pbgra32);
                        var background = new DrawingVisual(); using (var dc = background.RenderOpen()) dc.DrawRectangle(Background, null, new Rect(0, 0, width, height));
                        bitmap.Render(background); bitmap.Render(content);
                        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
                        using var file = File.Create(Path.Combine(directory, $"layout-{width}-{(int)(scale * 100)}.png")); encoder.Save(file);
                        Check(bitmap.PixelWidth == (int)(width * scale), "render at DPI " + 96 * scale);
                    }
                }
            });
            await Group("Visual keyboard assignments, exclusions and per-window overrides", async () =>
            {
                Modes.SelectedItem = KeyboardTab; KeyboardMode.Language = Language; await Layout();
                // Binding conversion errors in the preceding invalid-input suite were expected.
                bindingOutput.GetStringBuilder().Clear();
                var savedProfile = vm.GetProfile();
                var layerOnly = new Rule { Name = "Keyboard only", KeyboardLayer = true };
                vm.SetProfile(new Profile { Rules = [layerOnly] }); await Layout();
                Check(vm.SelectedRule is null && vm.AutomationRules.Count == 0, "keyboard-only profile does not populate other event editor");
                var legacyKey = new Rule { Name = "Legacy key", Event = EventKind.KeyDown, Keys = "A" };
                vm.SetProfile(new Profile { Rules = [legacyKey] }); await Layout();
                KeyboardMode.Model.Load(legacyKey.Id); KeyboardMode.Model.Save(); await Layout();
                Check(vm.Rules.Single().Id == legacyKey.Id && vm.Rules.Single().KeyboardLayer && vm.SelectedRule is null, "converting legacy key retains ID and clears other event selection");
                vm.SetProfile(savedProfile); await Layout();
                checks += await KeyboardMode.VerifyHarnessAsync();
                var keyboardBefore = vm.GetProfile();
                var allPresetPath = Path.Combine(directory, "keyboard-export.json");
                var onePresetPath = Path.Combine(directory, "keyboard-export-selected.json");
                KeyboardMode.Model.ExportFile(allPresetPath, false);
                KeyboardMode.Model.ExportFile(onePresetPath, true);
                var knownKeys = KeyboardLayout.Keys.Select(k => k.Id).ToArray();
                var bundle = KeyboardPresetStore.Load(allPresetPath, knownKeys);
                Check(bundle.Rules.Count == KeyboardMode.Model.Groups.Count, "export includes saved keyboard groups only");
                var selectedBundle = KeyboardPresetStore.Load(onePresetPath, knownKeys);
                Check(selectedBundle.Rules.Count == 1 && selectedBundle.Rules[0].Id == KeyboardMode.Model.SelectedId, "selected export preserves selected identity");
                var originalName = selectedBundle.Rules[0].Name;
                KeyboardMode.Model.Draft.Name = "Unsaved change";
                KeyboardMode.Model.ExportFile(onePresetPath, true);
                Check(KeyboardPresetStore.Load(onePresetPath, knownKeys).Rules[0].Name == originalName, "export excludes unsaved draft changes");
                var beforeRules = vm.Rules.Select(r => r.Copy()).ToArray();
                await KeyboardMode.Model.ImportFileAsync(allPresetPath); await Layout();
                Check(vm.Rules.Count == beforeRules.Length && !vm.Running, "import merges without duplicates and pauses rules");
                Check(vm.ApiUrl == keyboardBefore.ApiUrl && beforeRules.Where(r => !KeyboardPresetStore.CanEdit(r)).All(r => JsonSerializer.Serialize(r) == JsonSerializer.Serialize(vm.Rules.Single(x => x.Id == r.Id))), "keyboard import preserves all other events and API URL");
                Check(File.Exists(ProfileStore.PathName + ".before-keyboard-import.bak"), "keyboard import backs up previous profile");
                selectedBundle.Rules[0].Intensity = 17;
                KeyboardPresetStore.Save(onePresetPath, selectedBundle.Rules, knownKeys);
                await KeyboardMode.Model.ImportFileAsync(onePresetPath); await Layout();
                Check(KeyboardMode.Model.Draft.Intensity == 17 && vm.Rules.Count == beforeRules.Length, "reimport updates same ID and refreshes keyboard draft");
                Check(ProfileStore.Load().Rules.Single(r => r.Id == selectedBundle.Rules[0].Id).Intensity == 17, "merged keyboard import persists");
                var beforeInvalid = JsonSerializer.Serialize(vm.GetProfile());
                var beforeInvalidFile = File.ReadAllText(ProfileStore.PathName);
                File.WriteAllText(onePresetPath, File.ReadAllText(onePresetPath).Replace("\"Intensity\": 17", "\"Intensity\": 99"));
                var rejected = false; try { await KeyboardMode.Model.ImportFileAsync(onePresetPath); } catch (InvalidDataException) { rejected = true; }
                Check(rejected && beforeInvalid == JsonSerializer.Serialize(vm.GetProfile()) && beforeInvalidFile == File.ReadAllText(ProfileStore.PathName), "invalid import leaves profile and live assignments unchanged");
                var aiText = KeyboardAiInstructions.Read();
                var aiJson = System.Text.RegularExpressions.Regex.Match(aiText, "```json\\s*([\\s\\S]*?)\\s*```").Groups[1].Value;
                Check(KeyboardPresetStore.Parse(aiJson, knownKeys).Rules.Count == 2 && knownKeys.All(k => aiText.Contains(k, StringComparison.Ordinal)), "embedded AI guide has a valid example and all current key codes");
                var examplePath = Path.Combine(AppContext.BaseDirectory, "examples", "keyboard-assignments.json");
                await KeyboardMode.Model.ImportFileAsync(examplePath); await Layout();
                Check(vm.Rules.Count == beforeRules.Length + 2 && KeyboardMode.Model.Groups.Any(g => g.Name == "Stronger WASD in Notepad"), "AI example imports as new visible groups");
                vm.SetProfile(keyboardBefore); KeyboardMode.Model.Load(selectedBundle.Rules[0].Id); await Layout();
                foreach (var (width, height) in new[] { (1360, 900), (1120, 720) })
                {
                    await Layout(width, height);
                    var save = Descendants<Button>(KeyboardMode).Single(b => b.Content as string == L.T("Save assignment"));
                    var bounds = save.TransformToAncestor(content).TransformBounds(new Rect(save.RenderSize));
                    Check(bounds.Top >= 0 && bounds.Bottom <= height && bounds.Right <= width, "keyboard save stays reachable at " + width);
                    var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
                    var background = new DrawingVisual(); using (var dc = background.RenderOpen()) dc.DrawRectangle(Background, null, new Rect(0, 0, width, height));
                    bitmap.Render(background); bitmap.Render(content);
                    var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
                    using var file = File.Create(Path.Combine(directory, $"keyboard-mode-{width}.png")); encoder.Save(file);
                }
                KeyboardMode.ScrollToSettings(); await Layout(1360, 900);
                var settingsBitmap = new RenderTargetBitmap(1360, 900, 96, 96, PixelFormats.Pbgra32);
                var settingsBackground = new DrawingVisual(); using (var dc = settingsBackground.RenderOpen()) dc.DrawRectangle(Background, null, new Rect(0, 0, 1360, 900));
                settingsBitmap.Render(settingsBackground);
                settingsBitmap.Render(content);
                var settingsEncoder = new PngBitmapEncoder(); settingsEncoder.Frames.Add(BitmapFrame.Create(settingsBitmap));
                using var settingsFile = File.Create(Path.Combine(directory, "keyboard-mode-settings.png")); settingsEncoder.Save(settingsFile);
            });
            await Group("Visual mouse, live wheel, import/export and AI guide", async () =>
            {
                Modes.SelectedItem = MouseTab; MouseMode.Language = Language; await Layout();
                var beforeMouse = vm.GetProfile();
                checks += await MouseMode.VerifyHarnessAsync();
                var filePath = Path.Combine(directory, "mouse-export.json");
                var onePath = Path.Combine(directory, "mouse-selected.json");
                MouseMode.Model.ExportFile(filePath, false); MouseMode.Model.ExportFile(onePath, true);
                var preset = MousePresetStore.Load(filePath, MouseInputs.All);
                Check(preset.Rules.Count == MouseMode.Model.Groups.Count && preset.Rules.All(r => r.MouseLayer), "mouse export contains mouse assignments only");
                Check(MousePresetStore.Load(onePath, MouseInputs.All).Rules.Single().Id == MouseMode.Model.SelectedId, "single mouse export keeps identity");
                var count = vm.Rules.Count;
                await MouseMode.Model.ImportFileAsync(filePath); await Layout();
                Check(vm.Rules.Count == count && !vm.Running, "mouse merge does not duplicate and keeps rules paused");
                Check(beforeMouse.Rules.All(r => JsonSerializer.Serialize(r) == JsonSerializer.Serialize(vm.Rules.Single(x => x.Id == r.Id))), "mouse import preserves keyboard and other events");
                Check(File.Exists(ProfileStore.PathName + ".before-mouse-import.bak"), "mouse import backs up profile");
                var guide = MouseAiInstructions.Read(); var exampleJson = System.Text.RegularExpressions.Regex.Match(guide, "```json\\s*([\\s\\S]*?)\\s*```").Groups[1].Value;
                Check(MousePresetStore.Parse(exampleJson, MouseInputs.All).Rules.Count == 2, "embedded mouse AI example validates");
                await MouseMode.Model.ImportFileAsync(Path.Combine(AppContext.BaseDirectory, "examples", "mouse-assignments.json")); await Layout();
                Check(vm.Rules.Count == count + 2 && MouseMode.Model.Groups.Any(g => g.Name.Contains("Five buttons")), "AI mouse example imports into visible groups");
                var state = JsonSerializer.Serialize(vm.GetProfile());
                File.WriteAllText(onePath, exampleJson.Replace("\"WheelIdleMs\": 150", "\"WheelIdleMs\": 0"));
                var rejectedMouse = false; try { await MouseMode.Model.ImportFileAsync(onePath); } catch (InvalidDataException) { rejectedMouse = true; }
                Check(rejectedMouse && state == JsonSerializer.Serialize(vm.GetProfile()), "bad wheel timeout cannot mutate profile");
                MouseMode.Model.Load(vm.Rules.Single(r => r.Name == "Wheel — vibration while scrolling").Id); await Layout();
                foreach (var (width, height) in new[] { (1360, 900), (1120, 720) })
                {
                    await Layout(width, height);
                    var save = Descendants<Button>(MouseMode).Single(b => b.Content as string == L.T("Save assignment"));
                    var bounds = save.TransformToAncestor(content).TransformBounds(new Rect(save.RenderSize));
                    Check(bounds.Top >= 0 && bounds.Bottom <= height && bounds.Right <= width, "mouse save reachable at " + width);
                    var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
                    var background = new DrawingVisual(); using (var dc = background.RenderOpen()) dc.DrawRectangle(Background, null, new Rect(0, 0, width, height));
                    bitmap.Render(background); bitmap.Render(content); var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
                    using var imageFile = File.Create(Path.Combine(directory, $"mouse-mode-{width}.png")); encoder.Save(imageFile);
                }
                MouseMode.ScrollToSettings(); await Layout(1360, 900);
                var settingsBitmap = new RenderTargetBitmap(1360, 900, 96, 96, PixelFormats.Pbgra32);
                var bg = new DrawingVisual(); using (var dc = bg.RenderOpen()) dc.DrawRectangle(Background, null, new Rect(0, 0, 1360, 900));
                settingsBitmap.Render(bg); settingsBitmap.Render(content);
                var settingsEncoder = new PngBitmapEncoder(); settingsEncoder.Frames.Add(BitmapFrame.Create(settingsBitmap));
                using var settingsFile = File.Create(Path.Combine(directory, "mouse-mode-settings.png")); settingsEncoder.Save(settingsFile);
            });
            await Group("Screen assignments, imports, preview and localization", async () =>
            {
                var beforeScreen = vm.GetProfile();
                Modes.SelectedItem = ScreenTab; ScreenMode.Language = Language; await Layout();
                Check(!vm.Screen.Monitoring && !vm.Running, "Screen startup does not capture or enable rules");
                Check(vm.Screen.Events.Count == 13 && vm.Screen.Events.All(e => e.Value != "wow.test"), "Screen semantic catalog excludes diagnostic test");
                var eventSelector = (ComboBox)ScreenMode.FindName("EventSelector");
                var save = (Button)ScreenMode.FindName("SaveAssignmentButton");
                vm.Screen.New(); vm.Screen.Draft.Name = "Harness Screen assignment";
                eventSelector.SelectedValue = "wow.player.damage"; await Layout();
                Check(vm.Screen.Draft.ScreenEventId == "wow.player.damage" && vm.Screen.Draft.ScreenHelp.Length > 0, "Screen localized selector stores stable semantic ID");
                vm.Screen.Draft.DurationMilliseconds = 150; vm.Screen.Draft.CooldownSeconds = 0.3;
                save.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent)); await Layout();
                var saved = vm.Screen.Assignments.Single();
                Check(saved.DurationSeconds == 0.15 && saved.CooldownMs == 300 && !vm.AutomationRules.Contains(saved), "Screen save uses correct units and dedicated list");
                vm.Screen.Draft.Name = "Edited Screen name"; save.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent)); await Layout();
                Check(vm.Screen.Assignments.Count == 1 && vm.Screen.Assignments[0].Id == saved.Id, "Screen edit preserves stable ID");
                var path = Path.Combine(directory, "screen-export.json"); ScreenPresetStore.Save(path, vm.Screen.Assignments);
                var preset = ScreenPresetStore.Parse(File.ReadAllText(path));
                await vm.ImportScreenAsync(preset); await vm.ImportScreenAsync(preset); await Layout();
                Check(vm.Screen.Assignments.Count == 1 && !vm.Running && File.Exists(ProfileStore.PathName + ".before-screen-import.bak"), "Screen import is idempotent, backed up and paused");
                Check(beforeScreen.Rules.All(r => Snapshot(r) == Snapshot(vm.Rules.Single(x => x.Id == r.Id))), "Screen import preserves all other modes");
                vm.Screen.Selected = vm.Screen.Assignments[0];
                var packets = new[] { new ScreenPacket(1, 0, 0, 0, 0, 0), new ScreenPacket(1, 0, 0, 0, 0, 1), new ScreenPacket(1, 1, 14, 1, 0, 2), new ScreenPacket(1, 1, 14, 1, 0, 2) };
                var time = 0L;
                foreach (var packet in packets)
                {
                    var bytes = ScreenProtocol.Encode(packet); var pixels = new byte[32 * 32 * 4];
                    for (var y = 0; y < 32; y++) for (var x = 0; x < 32; x++) for (var c = 0; c < 3; c++)
                    {
                        var bit = (y / 4 * 8 + x / 4) * 3 + c;
                        pixels[(y * 32 + x) * 4 + 2 - c] = (bytes[bit / 8] & (1 << (7 - bit % 8))) != 0 ? (byte)255 : (byte)0;
                    }
                    vm.Screen.ApplySample(new(pixels, "", "Wow"), packet, time += 100, 4);
                }
                Check(vm.Screen.Preview is not null && vm.Screen.RecentEvents.Count == 1 && !vm.Running, "Screen test packet updates preview without an action");
                Check(vm.Screen.RecentEvents[0].Contains(L.T("Test signal")) && vm.Screen.Status == L.T("Connected to WowScreenEvents."), "Screen status and observed event localized");
                // Arm only the demo VM, bypassing global input hooks for this source lifecycle test.
                typeof(MainViewModel).GetProperty(nameof(MainViewModel.Running))!.SetValue(vm, true);
                var focus = WindowsInput.ForegroundProcess();
                var stamp = Environment.TickCount64;
                var journalCount = vm.Journal.Count;
                var test = new ScreenPacket(1, 2, 3, 1, 0, 3);
                vm.Screen.ApplySample(new(new byte[4096], "", focus), test, stamp, 4);
                vm.Screen.ApplySample(new(new byte[4096], "", focus), test, stamp + 1, 4);
                Check(vm.Running && vm.Journal.Count == journalCount, "Screen test-flagged real event cannot dispatch while rules are enabled");
                vm.Screen.ApplySample(new(null, "Waiting for World of Warcraft in the foreground."), null, stamp + 2, 4);
                await Layout();
                Check(!vm.Running && vm.Journal.Any(j => j.Contains(L.T("Screen signal lost. Rules are paused; enable them manually after recovery."))), "Screen focus loss stops and pauses the active demo session");
                vm.Screen.ApplySample(new(new byte[4096], "", focus), test, stamp + 3, 4);
                vm.Screen.ApplySample(new(new byte[4096], "", focus), test with { Heartbeat = 4 }, stamp + 103, 4);
                Check(!vm.Running, "Screen recovery never reenables rules");
                await using (var simulatedReader = new ScreenModeViewModel(vm, (_, _, _) => new(null, "Screen capture failed.")))
                {
                    for (var cycle = 0; cycle < 5; cycle++)
                    {
                        await simulatedReader.ToggleMonitoringAsync();
                        var stopping = simulatedReader.StopMonitoringAsync();
                        if (!stopping.IsCompleted) await simulatedReader.ToggleMonitoringAsync();
                        await stopping;
                        Check(!simulatedReader.Monitoring && simulatedReader.CanToggle, "Screen reader cancellation/reentry cycle " + cycle);
                    }
                }
                using (var guide = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("ScreenAiInstructions.md")) Check(guide is not null, "Screen AI guide embedded");
                Check(ScreenPresetStore.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "examples/screen-assignments.json"))).Rules.Count == 1, "Screen example shipped and valid");
                foreach (var (width, height) in new[] { (1360, 900), (1120, 720) })
                {
                    await Layout(width, height);
                    var scroll = Descendants<ScrollViewer>(ScreenMode).First(); scroll.ScrollToTop(); await Layout(width, height);
                    var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
                    var bg = new DrawingVisual(); using (var dc = bg.RenderOpen()) dc.DrawRectangle(Background, null, new Rect(0, 0, width, height));
                    bitmap.Render(bg); bitmap.Render(content); var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
                    using (var file = File.Create(Path.Combine(directory, $"screen-mode-{width}.png"))) encoder.Save(file);
                    scroll.ScrollToEnd(); await Layout(width, height);
                    var bounds = save.TransformToAncestor(content).TransformBounds(new Rect(save.RenderSize));
                    Check(bounds.Top >= 0 && bounds.Bottom <= height && bounds.Right <= width, "Screen save reachable by scrolling at " + width);
                    Check(scroll.ScrollableWidth == 0, "Screen has no horizontal overflow at " + width);
                    var settings = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
                    settings.Render(bg); settings.Render(content); var settingsEncoder = new PngBitmapEncoder(); settingsEncoder.Frames.Add(BitmapFrame.Create(settings));
                    using (var file = File.Create(Path.Combine(directory, $"screen-settings-{width}.png"))) settingsEncoder.Save(file);
                }
                vm.SetProfile(beforeScreen); vm.Screen.New();
            });
            File.WriteAllText(Path.Combine(directory, "report.json"), JsonSerializer.Serialize(new { Result = "PASS", Language = L.Language, Culture = CultureInfo.CurrentCulture.Name, Checks = checks, Groups = groups,
                Scope = "Real offscreen WPF controls/bindings/routed events; demo transport; no global hooks or live hardware; scaled raster output, not monitor DPI switching." }, new JsonSerializerOptions { WriteIndented = true }));
        }
        finally
        {
            trace.Listeners.Remove(listener); trace.Switch.Level = oldLevel;
            await vm.DisposeAsync();
            await updates.DisposeAsync();
        }
    }
}
