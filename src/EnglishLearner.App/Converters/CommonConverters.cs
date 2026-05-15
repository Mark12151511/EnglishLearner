using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace EnglishLearner.App.Converters;

public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value switch
        {
            bool b => b ? Visibility.Visible : Visibility.Collapsed,
            null => Visibility.Collapsed,
            _ => Visibility.Visible
        };

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is Visibility.Visible;
}

public class InvertBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value switch
        {
            bool b => b ? Visibility.Collapsed : Visibility.Visible,
            null => Visibility.Visible,
            _ => Visibility.Collapsed
        };

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is Visibility.Collapsed;
}
