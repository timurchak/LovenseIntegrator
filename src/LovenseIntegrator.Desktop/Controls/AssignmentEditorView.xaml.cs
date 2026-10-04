using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using LovenseIntegrator.Core;
using LovenseIntegrator.Desktop.ViewModels;

namespace LovenseIntegrator.Desktop.Controls;

/// <summary>Shared assignment shell. Only the trigger content belongs to a mode.</summary>
public sealed class AssignmentEditorView : Control
{
    internal TextBox RuleName { get { ApplyTemplate(); return (TextBox)Template.FindName(nameof(RuleName), this); } }
    internal ComboBox TargetSelector { get { ApplyTemplate(); return (ComboBox)Template.FindName(nameof(TargetSelector), this); } }
    internal ComboBox ApplicationSelector { get { ApplyTemplate(); return (ComboBox)Template.FindName(nameof(ApplicationSelector), this); } }
    internal Slider IntensitySlider { get { ApplyTemplate(); return (Slider)Template.FindName(nameof(IntensitySlider), this); } }
    internal Slider DurationSlider { get { ApplyTemplate(); return (Slider)Template.FindName(nameof(DurationSlider), this); } }
    internal StackPanel EffectSettings { get { ApplyTemplate(); return (StackPanel)Template.FindName(nameof(EffectSettings), this); } }
    internal StackPanel PulseSettings { get { ApplyTemplate(); return (StackPanel)Template.FindName(nameof(PulseSettings), this); } }
    internal Expander AdvancedSettings { get { ApplyTemplate(); return (Expander)Template.FindName(nameof(AdvancedSettings), this); } }
    internal ScrollViewer EditorScroll { get { ApplyTemplate(); return (ScrollViewer)Template.FindName(nameof(EditorScroll), this); } }
    internal StackPanel Editor { get { ApplyTemplate(); return (StackPanel)Template.FindName(nameof(Editor), this); } }
    internal EffectPreview EffectGraph { get { ApplyTemplate(); return (EffectPreview)Template.FindName(nameof(EffectGraph), this); } }
    internal Button SaveAssignmentButton { get { ApplyTemplate(); return (Button)Template.FindName(nameof(SaveAssignmentButton), this); } }
    internal ComboBox EffectSourceSelector { get { ApplyTemplate(); return (ComboBox)Template.FindName(nameof(EffectSourceSelector), this); } }
    public static readonly DependencyProperty TriggerContentProperty = DependencyProperty.Register(
        nameof(TriggerContent), typeof(object), typeof(AssignmentEditorView));
    public object TriggerContent { get => GetValue(TriggerContentProperty); set => SetValue(TriggerContentProperty, value); }
    public event RoutedEventHandler? SaveRequested;
    public event RoutedEventHandler? TestRequested;
    public event RoutedEventHandler? DeleteRequested;
    private IAssignmentEditorContext? Model => DataContext as IAssignmentEditorContext;
    public AssignmentEditorView()
    {
        AddHandler(ButtonBase.ClickEvent, new RoutedEventHandler(OnButtonClick));
    }
    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        ApplicationSelector.DropDownOpened += RefreshApplications;
        ApplicationSelector.SelectionChanged += ApplicationSelected;
    }
    private void OnButtonClick(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is not ButtonBase button || !button.IsEnabled || !ReferenceEquals(button.TemplatedParent, this)) return;
        if (button.Name == "SaveAssignmentButton") Save(sender, e);
        else if (button.Name == "TestAssignmentButton") Test(sender, e);
        else if (button.Name == "DeleteAssignmentButton") Delete(sender, e);
        else if (button.Name == "CopyEffectButton" && EffectSourceSelector.SelectedItem is Rule source) Model?.Draft.CopyEffectFrom(source);
        else if (button.Name == "ShowEffectButton") ((FrameworkElement)Template.FindName("EffectSection", this)).BringIntoView();
        else if (button is RadioButton) EffectSelected(button, e);
        else if (Model is { } model && button.Tag is string tag)
        {
            if (tag.StartsWith("intensity:", StringComparison.Ordinal)) model.Draft.Intensity = int.Parse(tag[10..]);
            if (tag.StartsWith("duration:", StringComparison.Ordinal)) model.Draft.DurationSeconds = double.Parse(tag[9..], System.Globalization.CultureInfo.InvariantCulture);
        }
    }
    private void Save(object sender, RoutedEventArgs e) => SaveRequested?.Invoke(this, e);
    private void Test(object sender, RoutedEventArgs e) => TestRequested?.Invoke(this, e);
    private void Delete(object sender, RoutedEventArgs e) => DeleteRequested?.Invoke(this, e);
    private void EffectSelected(object sender, RoutedEventArgs e)
    {
        if (Model is { } model && sender is RadioButton { Tag: string name }) model.Draft.Action = Enum.Parse<ActionKind>(name);
    }
    private void RefreshApplications(object? sender, EventArgs e)
    {
        Model?.RefreshApplications();
        ApplicationSelector.GetBindingExpression(ComboBox.SelectedValueProperty)?.UpdateTarget();
    }
    private void ApplicationSelected(object sender, SelectionChangedEventArgs e)
    {
        if (Model is { } model && (ApplicationSelector.IsDropDownOpen || ApplicationSelector.IsKeyboardFocusWithin) && ApplicationSelector.SelectedItem is Choice<string> choice)
            model.Draft.Process = choice.Value;
    }
}
