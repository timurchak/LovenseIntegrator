using LovenseIntegrator.Desktop.Localization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;
using LovenseIntegrator.Desktop.ViewModels;
using Microsoft.Win32;
using LovenseIntegrator.Desktop.Services;

namespace LovenseIntegrator.Desktop;

public partial class KeyboardModeView : UserControl
{
    private ComboBox KeyboardApplication => AssignmentEditor.ApplicationSelector;
    private ComboBox KeyboardTarget => AssignmentEditor.TargetSelector;
    private Controls.EffectPreview KeyboardGraph => AssignmentEditor.EffectGraph;
    private ScrollViewer SettingsScroll => AssignmentEditor.EditorScroll;
    internal InputModeViewModel Model { get; private set; } = null!;
    private readonly List<(Button Button, KeyboardKey Key, TextBlock Marker)> keys = [];
    private bool refreshing;
    public KeyboardModeView() { InitializeComponent(); }
    public void Initialize(MainViewModel owner)
    {
        Model = new(owner); DataContext = Model;
        foreach (var key in KeyboardLayout.Keys)
        {
            var content = new Grid { Width = key.Width * 44 - 15, Height = key.Height * 46 - 15 };
            content.Children.Add(new TextBlock { Text = key.Label, TextWrapping = TextWrapping.NoWrap, FontSize = key.Label.Length > 3 ? 10 : key.Label.Length > 1 ? 11 : 14, FontWeight = FontWeights.SemiBold, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Margin = new(0, 0, 0, key.Secondary.Length > 0 ? 10 : 0) });
            if (key.Secondary.Length > 0) content.Children.Add(new TextBlock { Text = key.Secondary, FontSize = 9, Opacity = 0.6, VerticalAlignment = VerticalAlignment.Bottom, HorizontalAlignment = HorizontalAlignment.Center });
            var marker = new TextBlock { Text = "•", FontSize = 8, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top, Margin = new(0, -5, -2, 0) }; content.Children.Add(marker);
            var button = new Button { Content = content, Tag = key.Id, Width = key.Width * 44 - 5, Height = key.Height * 46 - 5, Style = (Style)Resources["KeyButton"], ToolTip = key.Id };
            System.Windows.Automation.AutomationProperties.SetName(button, key.Id);
            button.Click += (_, _) => Model.Toggle(key.Id);
            Canvas.SetLeft(button, key.X * 44); Canvas.SetTop(button, key.Y * 46);
            KeyboardCanvas.Children.Add(button); keys.Add((button, key, marker));
        }
        Model.SelectionChanged += PaintKeys;
        Model.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName is nameof(Model.Targets) or nameof(Model.Groups) or nameof(Model.Applications) or nameof(Model.SelectedId))
                Dispatcher.BeginInvoke(RefreshSelections, DispatcherPriority.DataBind);
        };
        PaintKeys();
    }
    private void PaintKeys()
    {
        foreach (var (button, key, marker) in keys)
        {
            var excluded = Model.IsExcluded(key.Id); var selected = Model.IsSelected(key.Id);
            button.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(excluded ? "#FCE9EE" : selected ? "#6D50D7" : "#FFFFFF"));
            button.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(excluded ? "#B75472" : selected ? "#FFFFFF" : "#444A62"));
            button.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(excluded ? "#E6ADC0" : selected ? "#5C40BE" : "#D8DCE8"));
            marker.Visibility = Model.HasAssignment(key.Id) ? Visibility.Visible : Visibility.Hidden;
        }
    }
    private void RefreshSelections()
    {
        refreshing = true;
        try { foreach (var selector in new Selector[] { KeyboardTarget, KeyboardApplication, Assignments }) selector.GetBindingExpression(Selector.SelectedValueProperty)?.UpdateTarget(); }
        finally { refreshing = false; }
    }
    private void SelectKeys(object sender, RoutedEventArgs e) => Model.Select((string)((Button)sender).Tag);
    private void NewAssignment(object sender, RoutedEventArgs e) => Model.New();
    private void DuplicateAssignment(object sender, RoutedEventArgs e) => Model.Duplicate();
    private void AssignmentSelected(object sender, SelectionChangedEventArgs e)
    {
        if (!refreshing && Model is not null && Assignments.SelectedItem is KeyboardAssignment row && row.Id != Model.SelectedId) Model.Load(row.Id);
    }
    private static bool HasErrors(DependencyObject node)
    {
        if (node is UIElement { Visibility: Visibility.Collapsed }) return false;
        if (Validation.GetHasError(node)) return true;
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++) if (HasErrors(VisualTreeHelper.GetChild(node, i))) return true;
        return false;
    }
    private void SaveAssignment(object sender, RoutedEventArgs e) { if (!HasErrors(AssignmentEditor)) Model.Save(); }
    private async void TestAssignment(object sender, RoutedEventArgs e) { if (!HasErrors(AssignmentEditor)) await Model.TestAsync(); }
    private void DeleteAssignment(object sender, RoutedEventArgs e) => Model.Delete();
    private async void ImportKeyboard(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "Keyboard assignments (*.json)|*.json", Title = L.T("Import keyboard assignments — add / update by Id") };
        if (dialog.ShowDialog(Window.GetWindow(this)) != true) return;
        try { await Model.ImportFileAsync(dialog.FileName); }
        catch (Exception ex) { Model.Report(L.F($"Keyboard import failed: {ex.Message}")); }
    }
    private void ExportKeyboard(object sender, RoutedEventArgs e)
    {
        ExportKeyboardButton.ContextMenu.PlacementTarget = ExportKeyboardButton;
        ExportKeyboardButton.ContextMenu.Placement = PlacementMode.Bottom;
        ExportKeyboardButton.ContextMenu.IsOpen = true;
    }
    private void ExportAllKeyboard(object sender, RoutedEventArgs e) => ExportKeyboardFile(false);
    private void ExportSelectedKeyboard(object sender, RoutedEventArgs e) => ExportKeyboardFile(true);
    private void ExportKeyboardFile(bool selectedOnly)
    {
        if (selectedOnly && Model.SelectedId is null) { Model.Report(L.T("Select a saved assignment first.")); return; }
        var dialog = new SaveFileDialog { Filter = "Keyboard assignments (*.json)|*.json", FileName = selectedOnly ? "keyboard-assignment.json" : "keyboard-assignments.json" };
        if (dialog.ShowDialog(Window.GetWindow(this)) != true) return;
        try { Model.ExportFile(dialog.FileName, selectedOnly); }
        catch (Exception ex) { Model.Report(L.F($"Keyboard export failed: {ex.Message}")); }
    }
    private void ShowAiInstructions(object sender, RoutedEventArgs e)
    {
        var text = KeyboardAiInstructions.Read();
        var window = new Window { Owner = Window.GetWindow(this), Title = L.T("AI instructions — keyboard assignments"), Width = 800, Height = 680, MinWidth = 560, MinHeight = 400, WindowStartupLocation = WindowStartupLocation.CenterOwner, Background = Brushes.White };
        var grid = new Grid { Margin = new Thickness(18) }; grid.RowDefinitions.Add(new RowDefinition()); grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        var editor = new TextBox { Text = text, IsReadOnly = true, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, FontFamily = new FontFamily("Consolas"), FontSize = 12, Padding = new Thickness(10) };
        grid.Children.Add(editor);
        var footer = new StackPanel { Margin = new Thickness(0, 12, 0, 0) }; Grid.SetRow(footer, 1);
        var status = new TextBlock { Text = L.T("Give these instructions to an AI along with the effects you want."), Margin = new Thickness(0, 0, 0, 8) }; footer.Children.Add(status);
        var copy = new Button { Content = L.T("Copy instructions"), HorizontalAlignment = HorizontalAlignment.Left };
        copy.Click += (_, _) => { try { Clipboard.SetText(text); status.Text = L.T("Copied. Paste it into your AI chat."); } catch (Exception ex) { status.Text = L.T("Could not copy: ") + ex.Message; } };
        footer.Children.Add(copy); grid.Children.Add(footer); window.Content = grid; window.ShowDialog();
    }
    internal void ScrollToSettings() => SettingsScroll.ScrollToEnd();
    internal async Task<int> VerifyHarnessAsync()
    {
        var checks = 0;
        void Check(bool condition, string name) { if (!condition) throw new InvalidOperationException("Keyboard mode harness: " + name); checks++; }
        async Task Settle() { await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle); UpdateLayout(); }
        void KeyClick(string id) => keys.First(k => k.Key.Id == id).Button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        IEnumerable<TextBox> Inputs(DependencyObject root)
        {
            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                if (child is TextBox box) yield return box;
                foreach (var descendant in Inputs(child)) yield return descendant;
            }
        }
        Model.New(); Check(!Model.Save(), "empty selection is not saved");
        Check(keys.Count == 104, "full keyboard geometry has 104 key positions");
        foreach (var key in keys)
        {
            Check(key.Key.X * 44 + key.Button.Width <= KeyboardCanvas.Width && key.Key.Y * 46 + key.Button.Height <= KeyboardCanvas.Height, "key within canvas " + key.Key.Id);
        }
        SelectKeys(new Button { Tag = "all" }, new());
        KeyClick("Escape"); KeyClick("LeftCtrl"); await Settle();
        Check(!Model.IsSelected("Escape") && Model.IsExcluded("Escape") && Model.IsSelected("A"), "all minus exclusions");
        var legacySelection = Model.BuildRule();
        Check(legacySelection.Keys == "" && legacySelection.ExcludedKeys.Contains("Escape"), "all keys stays extensible with exclusions");
        Model.Draft.Name = "General feedback without Esc and Ctrl"; Check(Model.Save(), "save all minus exclusions");
        var baseId = Model.SelectedId!.Value;
        Model.New(); Model.Load(baseId); await Settle();
        Check(Model.IsExcluded("Escape") && Model.IsExcluded("LeftCtrl"), "exclusions survive reload");
        Check(keys.Single(k => k.Key.Id == "Escape").Button.Background.ToString() == "#FFFCE9EE", "excluded key is visibly pink");
        Model.Duplicate(); Check(Model.SelectedId is null && Model.IsExcluded("Escape"), "duplicate preserves selection and gets new identity");
        SelectKeys(new Button { Tag = "wasd" }, new());
        Check(Model.SelectionCount == 4 && Model.IsSelected("W") && !Model.IsSelected("Escape"), "WASD replaces all selection");
        KeyClick("D"); KeyClick("Space");
        Check(Model.SelectionCount == 4 && !Model.IsSelected("D") && Model.IsSelected("Space"), "individual toggles preserve other selected keys");
        Model.Select("letters"); Check(Model.SelectionCount == 26, "letters preset selects exactly 26 physical keys");
        Model.Select("arrows"); Check(Model.SelectionCount == 4 && Model.IsSelected("Left"), "arrow preset");
        Model.Select("none"); KeyClick("Return"); await Settle();
        Check(keys.Count(k => k.Key.Id == "Return" && k.Button.Background.ToString() == "#FF6D50D7") == 2, "both Enter positions share selection");
        Model.Select("wasd"); Model.Preset("typewriter");
        Check(Model.Draft.DurationSeconds == 0.12 && Model.Draft.Intensity == 8 && Model.SelectionCount == 4, "typewriter preset keeps key selection");
        Model.Draft.Name = "WASD in game"; Model.Draft.Process = "harness-game"; Model.Draft.WindowTitleContains = "Raid";
        Model.Draft.Intensity = 14; Model.Draft.ToyId = "missing-keyboard-toy";
        Model.RefreshApplications(); await Settle(); RefreshSelections();
        // Recreate target list after an external model edit, as loading an imported assignment does.
        Check(Model.Save(), "save per-app override"); var gameId = Model.SelectedId!.Value;
        Model.New(); Model.Load(gameId); await Settle(); RefreshSelections();
        Check((string?)KeyboardApplication.SelectedValue == "harness-game" && (string?)KeyboardTarget.SelectedValue == "missing-keyboard-toy", "missing app and device display from saved identity");
        Check(Model.BuildRule().WindowTitleContains == "Raid" && Model.BuildRule().MatchesKeyboardKey("A"), "window condition and OR selection preserved");
        var duration = Inputs(AssignmentEditor).Single(b => b.GetBindingExpression(TextBox.TextProperty)?.ParentBinding.Path.Path == "Draft.DurationMilliseconds");
        duration.Text = "invalid"; var groupsBefore = Model.Groups.Count;
        SaveAssignment(this, new());
        Check(Validation.GetHasError(duration) && Model.Groups.Count == groupsBefore, "invalid numeric binding blocks save");
        duration.Text = "150"; await Settle(); Check(Model.Draft.DurationSeconds == 0.15 && !Validation.GetHasError(duration), "milliseconds binding recovers");
        Model.Delete(); Check(Model.Groups.All(g => g.Id != gameId) && Model.Groups.Any(g => g.Id == baseId), "delete override preserves base assignment");
        Model.Load(baseId); Model.Duplicate(); Model.Select("wasd"); Model.Draft.Name = "WASD · stronger in game";
        Model.Draft.Process = "harness-game"; Model.Draft.WindowTitleContains = "Raid"; Model.Draft.Intensity = 12; Model.Save();
        await Settle();
        Check(KeyboardGraph.Intensity == 12 && KeyboardGraph.Duration == 0.15, "live graphical preview follows effect");
        return checks;
    }
}
