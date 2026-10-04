using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LovenseIntegrator.Core;
using LovenseIntegrator.Desktop.ViewModels;

namespace LovenseIntegrator.Desktop;

public partial class EventPickerWindow : Window
{
    public EventOption? SelectedEvent => Options.SelectedItem as EventOption;
    public EventPickerWindow(EventKind current)
    {
        InitializeComponent();
        Category.ItemsSource = new[] { "All events" }.Concat(RulePresentation.Events.Select(e => e.Category).Distinct()).ToArray();
        Category.SelectedIndex = 0; Filter(); Options.SelectedItem = RulePresentation.Event(current);
    }
    private void Filter()
    {
        if (Options is null || Category is null || Search is null || Count is null) return;
        var text = Search.Text.Trim();
        var filtered = RulePresentation.Events.Where(e => (Category.SelectedIndex <= 0 || e.Category == (string)Category.SelectedItem) &&
            (text.Length == 0 || (e.Label + " " + e.Help).Contains(text, StringComparison.OrdinalIgnoreCase))).ToArray();
        Options.ItemsSource = filtered; Count.Text = $"Events: {filtered.Length}";
    }
    private void FilterChanged(object sender, SelectionChangedEventArgs e) => Filter();
    private void SearchChanged(object sender, TextChangedEventArgs e) => Filter();
    private void OptionChanged(object sender, SelectionChangedEventArgs e) { if (ChooseButton is not null) ChooseButton.IsEnabled = SelectedEvent is not null; }
    private void Choose(object sender, RoutedEventArgs e) { if (SelectedEvent is not null) DialogResult = true; }
    private void ChooseOnDoubleClick(object sender, MouseButtonEventArgs e) { if (SelectedEvent is not null) DialogResult = true; }
    internal int VerifyHarness()
    {
        var checks = 0;
        void Check(bool passed, string label)
        {
            if (!passed) throw new InvalidOperationException("Event catalog harness: " + label);
            checks++;
        }
        foreach (var category in RulePresentation.Events.Select(e => e.Category).Distinct())
        {
            Category.SelectedItem = category; Search.Text = "";
            Check(Options.Items.Cast<EventOption>().Select(e => e.Value).SequenceEqual(RulePresentation.Events.Where(e => e.Category == category).Select(e => e.Value)), category + " has exactly its events");
        }
        Category.SelectedIndex = 0;
        foreach (var option in RulePresentation.Events)
        {
            Search.Text = "  " + option.Label.ToUpperInvariant() + "  ";
            Check(Options.Items.Cast<EventOption>().Any(e => e.Value == option.Value), "case-insensitive trimmed search for " + option.Value);
            Options.SelectedItem = option;
            Check(ChooseButton.IsEnabled && SelectedEvent?.Value == option.Value, "select matched " + option.Value);
        }
        Search.Text = "no-such-event-harness";
        Check(Options.Items.Count == 0 && SelectedEvent is null && !ChooseButton.IsEnabled && Count.Text == "Events: 0", "empty results cannot choose stale event");
        Search.Text = "";
        Check(Options.Items.Count == RulePresentation.Events.Count && !ChooseButton.IsEnabled, "clearing search restores complete list without stale selection");
        return checks;
    }
    internal void VerifySmoke(string directory)
    {
        if (SelectedEvent?.Value != EventKind.Timer) throw new InvalidOperationException("Event catalog: initial selection.");
        Category.SelectedItem = "Mouse"; Search.Text = "double";
        if (Options.Items.Count != 1 || ((EventOption)Options.Items[0]).Value != EventKind.MouseDoubleClick) throw new InvalidOperationException("Event catalog: category and search.");
        Options.SelectedIndex = 0;
        if (!ChooseButton.IsEnabled) throw new InvalidOperationException("Event catalog: selection.");
        Category.SelectedIndex = 0; Search.Text = "";
        var content = (FrameworkElement)Content;
        content.Measure(new Size(720, 600)); content.Arrange(new Rect(0, 0, 720, 600)); content.UpdateLayout();
        var bitmap = new System.Windows.Media.Imaging.RenderTargetBitmap(720, 600, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
        var background = new System.Windows.Media.DrawingVisual();
        using (var dc = background.RenderOpen()) dc.DrawRectangle(Background, null, new Rect(0, 0, 720, 600));
        bitmap.Render(background); bitmap.Render(content);
        var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder(); encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap));
        using var file = System.IO.File.Create(System.IO.Path.Combine(directory, "event-catalog.png")); encoder.Save(file);
    }
}
