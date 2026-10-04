using System.Windows.Markup;

namespace LovenseIntegrator.Desktop.Localization;

[MarkupExtensionReturnType(typeof(string))]
public sealed class TrExtension : MarkupExtension
{
    public string Text { get; set; } = "";
    public override object ProvideValue(IServiceProvider serviceProvider) => L.T(Text);
}
