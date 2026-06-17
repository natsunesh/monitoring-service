using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Contracts;

namespace GUI_zakupki.Helpers;

public sealed class ServiceStatusBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value switch
        {
            ServiceStatus.Running => Brushes.LimeGreen,
            ServiceStatus.Stopped => Brushes.IndianRed,
            ServiceStatus.Error => Brushes.Goldenrod,
            _ => Brushes.Gray
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}