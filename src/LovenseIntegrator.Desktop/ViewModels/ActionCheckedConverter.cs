using System.Globalization;
using System.Windows.Data;
using LovenseIntegrator.Core;

namespace LovenseIntegrator.Desktop.ViewModels;

public sealed class ActionCheckedConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value is ActionKind kind && kind.ToString() == parameter.ToString();
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
}
