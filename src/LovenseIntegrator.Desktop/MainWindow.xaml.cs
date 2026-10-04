using LovenseIntegrator.Desktop.Localization;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Data;
using Microsoft.Win32;
using LovenseIntegrator.Core;
using LovenseIntegrator.Desktop.Services;
using LovenseIntegrator.Desktop.ViewModels;

namespace LovenseIntegrator.Desktop;

public partial class MainWindow : Window
{
    private TextBox RuleName => RuleAssignmentEditor.RuleName;
    private ComboBox TargetSelector => RuleAssignmentEditor.TargetSelector;
    private ComboBox ApplicationSelector => RuleAssignmentEditor.ApplicationSelector;
    private Slider IntensitySlider => RuleAssignmentEditor.IntensitySlider;
    private Slider DurationSlider => RuleAssignmentEditor.DurationSlider;
    private StackPanel EffectSettings => RuleAssignmentEditor.EffectSettings;
    private StackPanel PulseSettings => RuleAssignmentEditor.PulseSettings;
    private Expander AdvancedSettings => RuleAssignmentEditor.AdvancedSettings;
    private ScrollViewer EditorScroll => RuleAssignmentEditor.EditorScroll;
    private StackPanel Editor => RuleAssignmentEditor.Editor;
    private Controls.EffectPreview EffectGraph => RuleAssignmentEditor.EffectGraph;
    private readonly MainViewModel vm;
    private EmergencyHotkey? hotkey;
    private bool capturing, closing, closed;
    private KeyRecording? recorder;
    private RuleEditor? recordingDraft;
    private string originalKeys = "";
    private readonly UpdateService updates;
    private bool restartForUpdate;
    public MainWindow() : this(0) { }
    public MainWindow(int initialTransport)
    {
        InitializeComponent(); vm = new(initialTransport); DataContext = vm;
        LanguageSelector.SelectedValue = LanguagePreferences.Read(LanguageSettingsPath);
        updates = new(new GithubUpdateBackend(), Path.Combine(Path.GetDirectoryName(ProfileStore.PathName)!, "updates.json"));
        UpdatesPanel.DataContext = updates;
        if (ProfileStore.OverridePath is null) Loaded += (_, _) => updates.Start();
        KeyboardMode.Initialize(vm);
        MouseMode.Initialize(vm);
        ScreenMode.Initialize(vm);
        if (Environment.GetCommandLineArgs().Contains("--screen")) Modes.SelectedItem = ScreenTab;
        if (Environment.GetCommandLineArgs().Contains("--mouse")) Modes.SelectedItem = MouseTab;
        vm.PropertyChanged += (_, args) => { if (args.PropertyName == nameof(MainViewModel.Draft) && capturing && !ReferenceEquals(recordingDraft, vm.Draft)) CancelRecording(); };
        vm.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(MainViewModel.Targets))
                Dispatcher.BeginInvoke(() => TargetSelector.GetBindingExpression(System.Windows.Controls.Primitives.Selector.SelectedValueProperty)?.UpdateTarget(), System.Windows.Threading.DispatcherPriority.DataBind);
        };
        CaptureButton.LostKeyboardFocus += (_, _) => { if (capturing) CancelRecording(); };
        if (Environment.GetCommandLineArgs().Contains("--connection-status"))
        {
            vm.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName is not (nameof(MainViewModel.Status) or nameof(MainViewModel.Busy))) return;
                try
                {
                    Directory.CreateDirectory("artifacts");
                    File.WriteAllText("artifacts/connection-status.json", System.Text.Json.JsonSerializer.Serialize(new
                    {
                        Transport = vm.ConnectionName, vm.Busy, vm.Running, Toys = vm.Toys.ToArray(), vm.Status
                    }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }));
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            };
        }
        SourceInitialized += (_, _) =>
        {
            try { hotkey = new(new WindowInteropHelper(this).Handle, () => _ = vm.StopAsync()); }
            catch (Exception ex) { vm.HotkeyStatus = L.T("Hotkey unavailable — use the stop button"); vm.Log(ex.Message); }
        };
        Closing += OnClosing;
        PreviewKeyDown += OnKeyDown;
        PreviewKeyUp += OnKeyUp;
    }
    private async void Toggle(object sender, RoutedEventArgs e) => await vm.ToggleAsync();
    private string LanguageSettingsPath => Path.Combine(Path.GetDirectoryName(ProfileStore.PathName)!, "settings.json");
    private void SaveLanguage(object sender, RoutedEventArgs e)
    {
        if (LanguageSelector.SelectedValue is not string language) return;
        try
        {
            LanguagePreferences.Save(LanguageSettingsPath, language);
            LanguageSaveStatus.Text = L.T("Language saved. Restart the app when you are ready.");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        { LanguageSaveStatus.Text = L.T("Could not save language: ") + ex.Message; }
    }
    private async void CheckUpdates(object sender, RoutedEventArgs e) => await updates.CheckAsync();
    private void RestartToUpdate(object sender, RoutedEventArgs e)
    {
        if (!updates.CanRestart || closing) return;
        restartForUpdate = true;
        Close();
    }
    private void OpenReleases(object sender, RoutedEventArgs e)
    {
        try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(UpdateService.RepositoryUrl + "/releases") { UseShellExecute = true }); }
        catch (Exception ex) { vm.Log(L.T("Could not open releases: ") + ex.Message); }
    }
    private async void StopAll(object sender, RoutedEventArgs e) => await vm.StopAsync();
    private async void Discover(object sender, RoutedEventArgs e) => await vm.DiscoverAsync();
    private async void Manual(object sender, RoutedEventArgs e) { if (Valid(ManualSeconds)) await vm.ManualAsync(false); }
    private async void Pulse(object sender, RoutedEventArgs e) { if (Valid(ManualSeconds)) await vm.ManualAsync(true); }
    private void NewRule(object sender, RoutedEventArgs e) { CancelRecording(); vm.NewRule(); }
    private void SaveRule(object sender, RoutedEventArgs e) { if (Valid(Editor) && Valid(RuleName)) vm.SaveRule(); }
    private async void TestRule(object sender, RoutedEventArgs e) { if (Valid(Editor) && Valid(RuleName)) await vm.TestRuleAsync(); }
    private void DeleteRule(object sender, RoutedEventArgs e) => vm.DeleteRule();
    private void PickEvent(object sender, RoutedEventArgs e)
    {
        CancelRecording();
        var picker = new EventPickerWindow(vm.Draft.Event) { Owner = this };
        if (picker.ShowDialog() == true && picker.SelectedEvent is { } option) vm.Draft.ChooseEvent(option.Value);
    }
    private void Recipe(object sender, RoutedEventArgs e) { CancelRecording(); if (sender is Button { Tag: RuleRecipe recipe }) vm.UseRecipe(recipe); }
    private void AnyKey(object sender, RoutedEventArgs e) { CancelRecording(); vm.Draft.Keys = ""; }
    private void RefreshApplications(object sender, EventArgs e)
    {
        vm.RefreshApplications();
        ApplicationSelector.GetBindingExpression(ComboBox.SelectedValueProperty)?.UpdateTarget();
    }
    private bool Valid(DependencyObject parent)
    {
        if (HasErrors(parent)) { vm.Log(L.T("Check the numeric fields outlined in red.")); return false; }
        return true;
    }
    private static bool HasErrors(DependencyObject parent)
    {
        if (parent is UIElement { Visibility: Visibility.Collapsed }) return false;
        if (Validation.GetHasError(parent)) return true;
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++) if (HasErrors(VisualTreeHelper.GetChild(parent, i))) return true;
        return false;
    }
    private async void Capture(object sender, RoutedEventArgs e)
    {
        if (capturing) { CancelRecording(); return; }
        await vm.StopAsync();
        originalKeys = vm.Draft.Keys; recordingDraft = vm.Draft; recorder = new(vm.Draft.Event); capturing = true;
        CaptureButton.Content = L.T("Cancel recording"); CaptureButton.Focus();
        vm.Log(vm.Draft.Event switch
        {
            EventKind.Chord => L.T("Press the combination, then release all keys. Esc to cancel."),
            EventKind.Sequence => L.T("Press keys in order. Enter to finish, Esc to cancel (up to 12 keys)."),
            _ => L.T("Press the key to record.")
        });
    }
    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (!capturing) return;
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        ApplyRecording(recorder!.Down(key.ToString(), e.IsRepeat)); e.Handled = true;
    }
    private void OnKeyUp(object sender, KeyEventArgs e)
    {
        if (!capturing) return;
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        ApplyRecording(recorder!.Up(key.ToString())); e.Handled = true;
    }
    private void ApplyRecording(KeyRecordingResult result)
    {
        if (result.Cancelled) { CancelRecording(); vm.Log(L.T("Recording cancelled. Previous keys preserved.")); return; }
        if (result.Keys.Length > 0) vm.Draft.Keys = result.Keys;
        if (result.Complete) { capturing = false; recorder = null; recordingDraft = null; CaptureButton.Content = L.T("Record"); vm.Log(L.T("Keys recorded. Save the rule.")); }
    }
    private void CancelRecording()
    {
        if (!capturing) return;
        if (recordingDraft is not null) recordingDraft.Keys = originalKeys;
        capturing = false; recorder = null; recordingDraft = null; CaptureButton.Content = L.T("Record");
    }
    private async void Import(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "Rule profile (*.json)|*.json" };
        if (dialog.ShowDialog(this) != true) return;
        try { var profile = ProfileStore.Load(dialog.FileName); await vm.ImportProfileAsync(profile); }
        catch (Exception ex) { vm.Log(L.F($"Import failed: {ex.Message}")); }
    }
    private void Export(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog { Filter = "Rule profile (*.json)|*.json", FileName = "lovense-profile.json" };
        if (dialog.ShowDialog(this) != true) return;
        try { ProfileStore.Save(vm.GetProfile(), dialog.FileName); vm.Log(L.T("Profile exported.")); }
        catch (Exception ex) { vm.Log(L.F($"Export failed: {ex.Message}")); }
    }
    private async void OnClosing(object? sender, CancelEventArgs e)
    {
        if (closed) return;
        e.Cancel = true;
        if (closing) return;
        closing = true;
        try
        {
            IsEnabled = false;
            hotkey?.Dispose();
            // Stop physical actions before waiting on any download cancellation.
            await vm.DisposeAsync();
            await updates.DisposeAsync();
            if (restartForUpdate) updates.RestartAfterShutdown();
        }
        catch (Exception ex)
        {
            vm.Log($"Shutdown: {ex.Message}");
            if (restartForUpdate) MessageBox.Show(this, L.T("The update could not be started. Reopen the app to retry.\n\n") + ex.Message, L.T("App update"), MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally { closed = true; Close(); }
    }
    internal async Task RunSmokeAsync(string directory)
    {
        Modes.SelectedItem = RulesTab;
        static void Assert(bool condition, string name) { if (!condition) throw new InvalidOperationException("UI smoke: " + name); }
        IEnumerable<T> Descendants<T>(DependencyObject root) where T : DependencyObject
        {
            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                if (child is T match) yield return match;
                foreach (var descendant in Descendants<T>(child)) yield return descendant;
            }
        }
        var content = (FrameworkElement)Content;
        void Layout(double width, double height)
        {
            content.Measure(new Size(width, height)); content.Arrange(new Rect(0, 0, width, height)); content.UpdateLayout();
        }
        void Render(string name, int width, int height)
        {
            Layout(width, height);
            var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            var background = new DrawingVisual(); using (var dc = background.RenderOpen()) dc.DrawRectangle(Background, null, new Rect(0, 0, width, height));
            bitmap.Render(background); bitmap.Render(content);
            var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using var file = File.Create(Path.Combine(directory, name)); encoder.Save(file);
        }
        await vm.DiscoverAsync(); Layout(1360, 900);
        Assert(vm.Toys.Count == 2, "demo devices");
        Assert(vm.Events.Count == Enum.GetValues<EventKind>().Length, "every event has a catalog entry");
        var legacy = new Rule { Name = "Existing rule", Event = EventKind.KeyHeld, Keys = "W", Threshold = 1250, WindowMs = 3750, CooldownMs = 2750, Action = ActionKind.Pulse, Intensity = 11, DurationSeconds = 73.5, PulseMs = 515, Process = "notepad", ToyId = "unavailable-id" };
        vm.SetProfile(new Profile { Rules = [legacy] }); Layout(1360, 900);
        Assert(System.Text.Json.JsonSerializer.Serialize(vm.Draft.Copy()) == System.Text.Json.JsonSerializer.Serialize(legacy), "opening editor preserves legacy values, fractional durations and unavailable targets");
        vm.SetProfile(ProfileStore.Defaults());
        vm.Draft.ChooseEvent(EventKind.DoublePress); vm.Draft.ThresholdDisplay = 3;
        Assert(vm.Draft.Threshold == 3000 && vm.Draft.WindowMs >= 3000 && vm.Draft.Validate() is null, "repeat interval automatically expands hidden history window");
        new EventPickerWindow(EventKind.Timer).VerifySmoke(directory);
        vm.UseRecipe(vm.Recipes[0]); RuleName.Text = "Hold Space → pulse";
        Assert(vm.Draft.Name == RuleName.Text && vm.Draft.Event == EventKind.KeyHeld, "recipe creates an editable draft");
        vm.Draft.ChooseEvent(EventKind.KeyDown); Layout(1360, 900);
        Assert(ThresholdSettings.Visibility == Visibility.Collapsed && WindowSettings.Visibility == Visibility.Collapsed && KeyboardSettings.Visibility == Visibility.Visible, "irrelevant event fields hidden");
        vm.Draft.ChooseEvent(EventKind.KeyHeld); Layout(1360, 900);
        Assert(ThresholdSettings.Visibility == Visibility.Visible && vm.Draft.KeyCaps.SequenceEqual(new[] { "Space" }), "hold controls and keycaps");
        ThresholdInput.Text = "oops";
        Assert(Validation.GetHasError(ThresholdInput), "numeric validation");
        var oldCount = vm.Rules.Count; SaveRule(this, new());
        Assert(vm.Rules.Count == oldCount, "invalid editor cannot save");
        ThresholdInput.Text = 0.8.ToString(ThresholdInput.Language.GetSpecificCulture());
        Assert(!Validation.GetHasError(ThresholdInput) && Math.Abs(vm.Draft.Threshold - 800) < 0.001, "seconds convert to legacy milliseconds");
        IntensitySlider.Value = 9; Layout(1360, 900);
        Assert(vm.Draft.Intensity == 9 && EffectGraph.Intensity == 9 && vm.Draft.Summary.Contains("9/20"), "slider updates rule, graph, summary");
        Assert(vm.Recipes[0].Rule.Intensity == 5, "recipe source is not mutated");
        vm.Draft.Action = ActionKind.Pulse; Layout(1360, 900);
        Assert(PulseSettings.Visibility == Visibility.Visible && EffectGraph.Kind == ActionKind.Pulse, "pulse controls and waveform");
        vm.Draft.Action = ActionKind.Stop; Layout(1360, 900);
        Assert(EffectSettings.Visibility == Visibility.Collapsed, "stop hides vibration settings");
        vm.Draft.Action = ActionKind.Pulse;
        TargetSelector.SelectedValue = "demo-ferri";
        Assert(vm.Draft.ToyId == "demo-ferri", "toy target uses ID");
        vm.Draft.Process = "notepad"; RefreshApplications(this, EventArgs.Empty); Layout(1360, 900);
        Assert(vm.Draft.Process == "notepad" && (string?)ApplicationSelector.SelectedValue == "notepad", "application filter survives refresh");
        vm.Draft.Process = "";
        SaveRule(this, new()); Assert(vm.SelectedRule?.ToyId == "demo-ferri" && vm.Rules.Count == oldCount + 1, "save draft with target");
        vm.ObserveOnly = true;
        await vm.FireAsync(new(vm.Draft.Copy(), 800));
        Assert(vm.Journal[0].Contains("Observation:"), "observe-only suppresses automatic command");
        await vm.TestRuleAsync(); Assert(vm.Journal.Any(j => j.Contains("[demo]")), "explicit action test still works");
        vm.ObserveOnly = false;
        originalKeys = vm.Draft.Keys; recordingDraft = vm.Draft; capturing = true; recorder = new(EventKind.Chord);
        ApplyRecording(recorder.Down("LeftCtrl")); ApplyRecording(recorder.Down("Space"));
        ApplyRecording(recorder.Up("Space")); ApplyRecording(recorder.Up("LeftCtrl"));
        Assert(vm.Draft.Keys == "LeftCtrl+Space" && !capturing, "chord recording updates keycaps");
        vm.SelectedRule = vm.Rules.Last();
        await vm.StopAsync(); Assert(!vm.Running, "stop leaves rules paused");
        Render("desktop-normal.png", 1360, 900);
        Render("desktop-minimum.png", 1120, 720);
        AdvancedSettings.IsExpanded = true; Layout(1360, 900); EditorScroll.ScrollToEnd();
        Render("desktop-editor-action.png", 1360, 900);
        await vm.DisposeAsync();
    }
}
