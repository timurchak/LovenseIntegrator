using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using LovenseIntegrator.Desktop.Services;
using LovenseIntegrator.Desktop.ViewModels;

namespace LovenseIntegrator.Desktop;

public partial class ScreenModeView : UserControl
{
    private MainViewModel owner = null!;
    private ScreenModeViewModel Model => (ScreenModeViewModel)DataContext;
    public ScreenModeView() => InitializeComponent();
    public void Initialize(MainViewModel main) { owner = main; DataContext = main.Screen; Model.New(); }
    private async void Monitor(object sender, RoutedEventArgs e) { if (!HasErrors(RegionInputs)) await Model.ToggleMonitoringAsync(); }
    private void NewAssignment(object sender, RoutedEventArgs e) => Model.New();
    private void SaveAssignment(object sender, RoutedEventArgs e) { if (!HasErrors(AssignmentEditor, RegionInputs)) Model.Save(); }
    private void DeleteAssignment(object sender, RoutedEventArgs e) => Model.Delete();
    private async void TestAssignment(object sender, RoutedEventArgs e) { if (!HasErrors(AssignmentEditor, RegionInputs)) await Model.TestAsync(); }
    private async void Import(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "Screen assignments (*.json)|*.json" };
        if (dialog.ShowDialog(Window.GetWindow(this)) != true) return;
        try { await owner.ImportScreenAsync(ScreenPresetStore.Parse(File.ReadAllText(dialog.FileName))); }
        catch (Exception ex) { owner.Log(ex.Message); }
    }
    private void Export(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog { Filter = "Screen assignments (*.json)|*.json", FileName = "screen-assignments.json" };
        if (dialog.ShowDialog(Window.GetWindow(this)) != true) return;
        try { ScreenPresetStore.Save(dialog.FileName, Model.Assignments); }
        catch (Exception ex) { owner.Log(ex.Message); }
    }
    private void CopyInstructions(object sender, RoutedEventArgs e)
    {
        try
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("ScreenAiInstructions.md")!;
            using var reader = new StreamReader(stream); Clipboard.SetText(reader.ReadToEnd());
        }
        catch (Exception ex) { owner.Log(ex.Message); }
    }
    private static bool HasErrors(DependencyObject node, DependencyObject? excluded = null)
    {
        if (ReferenceEquals(node, excluded)) return false;
        if (node is UIElement { Visibility: Visibility.Collapsed }) return false;
        if (Validation.GetHasError(node)) return true;
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++) if (HasErrors(VisualTreeHelper.GetChild(node, i), excluded)) return true;
        return false;
    }
}
