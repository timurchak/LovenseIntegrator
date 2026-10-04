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

public partial class MouseModeView : UserControl
{
    internal InputModeViewModel Model { get; private set; } = null!;
    private readonly List<(Button Button, string Key, TextBlock Marker)> keys = [];
    private bool refreshing;
    public MouseModeView() { InitializeComponent(); }
    private void BuildMouse()
    {
        MouseCanvas.Children.Add(new System.Windows.Shapes.Path
        {
            Data = Geometry.Parse("M 288,6 C 218,6 191,40 191,100 L 191,155 C 191,210 224,234 288,234 C 352,234 385,210 385,155 L 385,100 C 385,40 358,6 288,6 Z"),
            Fill = Brushes.White, Stroke = new SolidColorBrush(Color.FromRgb(205, 208, 224)), StrokeThickness = 2
        });
        void Add(string id, string label, double x, double y, double width, double height)
        {
            var content = new Grid { Width = width - 16, Height = height - 16 };
            content.Children.Add(new TextBlock { Text = label, FontSize = 12, TextAlignment = TextAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center });
            var marker = new TextBlock { Text = "•", FontSize = 10, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top }; content.Children.Add(marker);
            var button = new Button { Content = content, Tag = id, Width = width, Height = height, Style = (Style)Resources["KeyButton"], ToolTip = id };
            System.Windows.Automation.AutomationProperties.SetName(button, id);
            button.Click += (_, _) => Model.Toggle(id);
            Canvas.SetLeft(button, x); Canvas.SetTop(button, y); MouseCanvas.Children.Add(button); keys.Add((button, id, marker));
        }
        Add("Left", L.T("1\nLeft"), 206, 22, 63, 95); Add("Right", L.T("2\nRight"), 308, 22, 63, 95);
        Add("Middle", "3", 275, 57, 32, 56);
        Add("X1", "4 · X1", 128, 117, 70, 42); Add("X2", "5 · X2", 128, 164, 70, 42);
        Add("Up", L.T("↑  Scroll up"), 427, 50, 170, 48); Add("Down", L.T("↓  Scroll down"), 427, 108, 170, 48);
        var caption = new TextBlock { Text = L.T("3 — wheel click"), Foreground = new SolidColorBrush(Color.FromRgb(110, 114, 139)), FontSize = 11 };
        Canvas.SetLeft(caption, 231); Canvas.SetTop(caption, 178); MouseCanvas.Children.Add(caption);
    }
    public void Initialize(MainViewModel owner)
    {
        Model = new(owner, true); DataContext = Model;
        BuildMouse();
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
            var excluded = Model.IsExcluded(key); var selected = Model.IsSelected(key);
            button.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(excluded ? "#FCE9EE" : selected ? "#6D50D7" : "#FFFFFF"));
            button.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(excluded ? "#B75472" : selected ? "#FFFFFF" : "#444A62"));
            button.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(excluded ? "#E6ADC0" : selected ? "#5C40BE" : "#D8DCE8"));
            marker.Visibility = Model.HasAssignment(key) ? Visibility.Visible : Visibility.Hidden;
        }
    }
    private void RefreshSelections()
    {
        refreshing = true;
        try { foreach (var selector in new Selector[] { MouseTarget, MouseApplication, Assignments }) selector.GetBindingExpression(Selector.SelectedValueProperty)?.UpdateTarget(); }
        finally { refreshing = false; }
    }
    private void SelectKeys(object sender, RoutedEventArgs e) => Model.Select((string)((Button)sender).Tag);
    private void ChoosePreset(object sender, RoutedEventArgs e) => Model.Preset((string)((Button)sender).Tag);
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
    private async void ImportMouse(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "Mouse assignments (*.json)|*.json", Title = L.T("Import mouse assignments — add / update by Id") };
        if (dialog.ShowDialog(Window.GetWindow(this)) != true) return;
        try { await Model.ImportFileAsync(dialog.FileName); }
        catch (Exception ex) { Model.Report(L.F($"Mouse import failed: {ex.Message}")); }
    }
    private void ExportMouse(object sender, RoutedEventArgs e)
    {
        ExportMouseButton.ContextMenu.PlacementTarget = ExportMouseButton;
        ExportMouseButton.ContextMenu.Placement = PlacementMode.Bottom;
        ExportMouseButton.ContextMenu.IsOpen = true;
    }
    private void ExportAllMouse(object sender, RoutedEventArgs e) => ExportMouseFile(false);
    private void ExportSelectedMouse(object sender, RoutedEventArgs e) => ExportMouseFile(true);
    private void ExportMouseFile(bool selectedOnly)
    {
        if (selectedOnly && Model.SelectedId is null) { Model.Report(L.T("Select a saved assignment first.")); return; }
        var dialog = new SaveFileDialog { Filter = "Mouse assignments (*.json)|*.json", FileName = selectedOnly ? "mouse-assignment.json" : "mouse-assignments.json" };
        if (dialog.ShowDialog(Window.GetWindow(this)) != true) return;
        try { Model.ExportFile(dialog.FileName, selectedOnly); }
        catch (Exception ex) { Model.Report(L.F($"Mouse export failed: {ex.Message}")); }
    }
    private void ShowAiInstructions(object sender, RoutedEventArgs e)
    {
        var text = MouseAiInstructions.Read();
        var window = new Window { Owner = Window.GetWindow(this), Title = L.T("AI instructions — mouse assignments"), Width = 800, Height = 680, MinWidth = 560, MinHeight = 400, WindowStartupLocation = WindowStartupLocation.CenterOwner, Background = Brushes.White };
        var grid = new Grid { Margin = new Thickness(18) }; grid.RowDefinitions.Add(new RowDefinition()); grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        var editor = new TextBox { Text = text, IsReadOnly = true, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, FontFamily = new FontFamily("Consolas"), FontSize = 12, Padding = new Thickness(10) };
        grid.Children.Add(editor);
        var footer = new StackPanel { Margin = new Thickness(0, 12, 0, 0) }; Grid.SetRow(footer, 1);
        var status = new TextBlock { Text = L.T("Give these instructions to an AI along with the effects you want."), Margin = new Thickness(0, 0, 0, 8) }; footer.Children.Add(status);
        var copy = new Button { Content = L.T("Copy instructions"), HorizontalAlignment = HorizontalAlignment.Left };
        copy.Click += (_, _) => { try { Clipboard.SetText(text); status.Text = L.T("Copied. Paste it into your AI chat."); } catch (Exception ex) { status.Text = L.T("Could not copy: ") + ex.Message; } };
        footer.Children.Add(copy); grid.Children.Add(footer); window.Content = grid; window.ShowDialog();
    }
    private void RefreshApps(object? sender, EventArgs e) { refreshing = true; try { Model.RefreshApplications(); } finally { refreshing = false; } RefreshSelections(); }
    private void AppSelected(object sender, SelectionChangedEventArgs e)
    {
        if (!refreshing && Model is not null && (MouseApplication.IsDropDownOpen || MouseApplication.IsKeyboardFocusWithin) && MouseApplication.SelectedItem is Choice<string> choice) Model.Draft.Process = choice.Value;
    }
    private void TargetSelected(object sender, SelectionChangedEventArgs e)
    {
        if (!refreshing && Model is not null && (MouseTarget.IsDropDownOpen || MouseTarget.IsKeyboardFocusWithin) && MouseTarget.SelectedItem is Core.Toy toy) Model.Draft.ToyId = toy.Id;
    }
    internal void ScrollToSettings() => SettingsScroll.ScrollToEnd();
    internal async Task<int> VerifyHarnessAsync()
    {
        var checks = 0;
        void Check(bool value, string message) { if (!value) throw new InvalidOperationException("Mouse UI: " + message); checks++; }
        async Task Settle() { await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle); UpdateLayout(); }
        void Click(string id) => keys.Single(k => k.Key == id).Button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        Model.New(); Check(!Model.Save(), "empty selection does not create an all-mouse rule");
        Check(keys.Count == 7 && keys.Select(k => k.Key).ToHashSet().SetEquals(Core.MouseInputs.All), "five buttons and both wheel directions exist");
        foreach (var (button, key, _) in keys)
        {
            Click(key); await Settle();
            Check(Model.IsSelected(key) && button.Background.ToString() == "#FF6D50D7", "select visible input " + key);
            Click(key); Check(!Model.IsSelected(key), "deselect visible input " + key);
            Check(Canvas.GetLeft(button) >= 0 && Canvas.GetLeft(button) + button.Width <= MouseCanvas.Width && Canvas.GetTop(button) + button.Height <= MouseCanvas.Height, "mouse control inside canvas " + key);
        }
        Model.Select("all"); Click("Right"); await Settle();
        Check(Model.SelectionCount == 6 && Model.IsExcluded("Right") && keys.Single(k => k.Key == "Right").Button.Background.ToString() == "#FFFCE9EE", "all mouse minus pink exclusion");
        Model.Draft.Name = "Mouse · feedback without right button";
        Check(Model.Save(), "save mouse assignment"); var baseId = Model.SelectedId!.Value;
        Model.New(); Model.Load(baseId); Check(Model.IsExcluded("Right") && Model.SelectionCount == 6, "reload exclusions");
        Model.Duplicate(); Model.Select("buttons"); Model.Preset("feedback");
        Check(Model.SelectionCount == 5 && !Model.IsSelected("Up") && Model.Draft.CooldownMs == 0 && Model.Draft.DurationSeconds == 0.1, "button preset has zero pause and 100 ms effect");
        Model.Preset("pulse"); Check(Model.Draft.Action == Core.ActionKind.Pulse && !Model.Draft.WheelContinuous, "pulse uses ordinary effects");
        Model.Select("wheel"); Model.Preset("scroll"); await Settle();
        Check(Model.SelectionCount == 2 && Model.Draft.WheelContinuous && Model.Draft.Action == Core.ActionKind.Vibrate && Model.Draft.WheelIdleMs == 150, "live wheel preset keeps both directions");
        Check(MouseGraph.Duration == 0.15 && MouseGraph.Intensity == 5, "wheel graph displays stop tail");
        Model.Draft.Name = "Wheel · while scrolling"; Model.Draft.Process = "harness-game"; Model.Draft.WindowTitleContains = "Raid"; Model.Draft.ToyId = "missing-mouse-toy";
        await Settle(); RefreshSelections();
        Check((string?)MouseApplication.SelectedValue == "harness-game" && (string?)MouseTarget.SelectedValue == "missing-mouse-toy", "window and missing device selectors retain values");
        Check(Model.Save() && Model.SelectedId != baseId, "duplicate gets new stable ID"); var wheelId = Model.SelectedId!.Value;
        IEnumerable<TextBox> Inputs(DependencyObject root)
        {
            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            {
                var child = VisualTreeHelper.GetChild(root, i); if (child is TextBox box) yield return box;
                foreach (var item in Inputs(child)) yield return item;
            }
        }
        var tail = Inputs(AssignmentEditor).Single(b => b.GetBindingExpression(TextBox.TextProperty)?.ParentBinding.Path.Path == "Draft.WheelIdleMs");
        Model.Draft.Name = "Must not save"; await Settle(); tail.Text = "invalid"; SaveAssignment(this, new());
        Model.Load(wheelId); await Settle(); Check(Model.Draft.Name == "Wheel · while scrolling", "bad numeric binding blocks save");
        tail.Text = "200"; await Settle(); Check(Model.Draft.WheelIdleMs == 200 && MouseGraph.Duration == 0.2, "wheel tail binding recovers");
        Model.Delete(); Check(Model.Groups.Any(g => g.Id == baseId) && Model.Groups.All(g => g.Id != wheelId), "delete affects selected mouse group only");
        Model.New(); Model.Select("wheel"); Model.Preset("scroll"); Model.Draft.Name = "Wheel · live feedback"; Model.Save(); await Settle();
        return checks;
    }
}
