using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace EnglishLearner.App.Converters;

public class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c)
        => value is true ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type t, object p, CultureInfo c)
        => throw new NotImplementedException();
}
