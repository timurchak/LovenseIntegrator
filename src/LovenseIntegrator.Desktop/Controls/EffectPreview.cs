using LovenseIntegrator.Desktop.Localization;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using LovenseIntegrator.Core;

namespace LovenseIntegrator.Desktop.Controls;

public sealed class EffectPreview : FrameworkElement
{
    public static readonly DependencyProperty KindProperty = DependencyProperty.Register(nameof(Kind), typeof(ActionKind), typeof(EffectPreview), new FrameworkPropertyMetadata(ActionKind.Vibrate, FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty IntensityProperty = DependencyProperty.Register(nameof(Intensity), typeof(int), typeof(EffectPreview), new FrameworkPropertyMetadata(7, FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty DurationProperty = DependencyProperty.Register(nameof(Duration), typeof(double), typeof(EffectPreview), new FrameworkPropertyMetadata(3.0, FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty PulseProperty = DependencyProperty.Register(nameof(Pulse), typeof(int), typeof(EffectPreview), new FrameworkPropertyMetadata(500, FrameworkPropertyMetadataOptions.AffectsRender));
    public ActionKind Kind { get => (ActionKind)GetValue(KindProperty); set => SetValue(KindProperty, value); }
    public int Intensity { get => (int)GetValue(IntensityProperty); set => SetValue(IntensityProperty, value); }
    public double Duration { get => (double)GetValue(DurationProperty); set => SetValue(DurationProperty, value); }
    public int Pulse { get => (int)GetValue(PulseProperty); set => SetValue(PulseProperty, value); }
    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        var width = ActualWidth; var height = ActualHeight;
        if (width < 70 || height < 60) return;
        var muted = new SolidColorBrush(Color.FromRgb(118, 124, 143));
        var accent = new SolidColorBrush(Color.FromRgb(109, 80, 215));
        var top = 12.0; var bottom = height - 28; var left = 28.0; var right = width - 12;
        var duration = double.IsFinite(Duration) ? Math.Clamp(Duration, 0.1, 300) : 3;
        var shown = Math.Min(duration, 12);
        void Label(string text, double x, double y) => dc.DrawText(new FormattedText(text, CultureInfo.GetCultureInfo("en-US"), FlowDirection.LeftToRight, new Typeface("Segoe UI"), 10, muted, VisualTreeHelper.GetDpi(this).PixelsPerDip), new(x, y));
        for (var value = 0; value <= 20; value += 5)
        {
            var y = bottom - (bottom - top) * value / 20;
            dc.DrawLine(new Pen(new SolidColorBrush(Color.FromRgb(230, 227, 241)), 1), new(left, y), new(right, y));
            if (value % 10 == 0) Label(value.ToString(), 2, y - 7);
        }
        Label(duration < 1 ? L.T("0 ms") : L.T("0 s"), left, bottom + 8);
        Label(duration < 1 ? L.F($"{shown * 1000:0} ms") : L.F($"{shown:0.##} s"), right - 42, bottom + 8);
        if (duration > shown) Label(L.F($"first {shown:0.#} seconds"), left + 42, bottom + 8);
        var level = Kind == ActionKind.Stop ? 0 : Math.Clamp(Intensity, 0, 20);
        double Y(bool on) => bottom - (bottom - top) * (on ? level : 0) / 20;
        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            ctx.BeginFigure(new(left, Y(true)), false, false);
            if (Kind == ActionKind.Pulse)
            {
                var pulseSeconds = Math.Clamp(Pulse, 150, 10000) / 1000.0;
                var on = true;
                for (var t = pulseSeconds; t < shown; t += pulseSeconds)
                {
                    var x = left + (right - left) * t / shown;
                    ctx.LineTo(new(x, Y(on)), true, false); on = !on;
                    ctx.LineTo(new(x, Y(on)), true, false);
                }
                ctx.LineTo(new(right, Y(on)), true, false);
            }
            else ctx.LineTo(new(right, Y(true)), true, false);
            if (duration <= shown) ctx.LineTo(new(right, bottom), true, false);
        }
        geometry.Freeze(); dc.DrawGeometry(null, new Pen(accent, 2.5), geometry);
    }
}
