using System;
using System.Globalization;
using System.Windows.Data;

namespace D4MT.UI.Converters;

public sealed class ReverseBooleanConverter : IValueConverter {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
        if (value is not bool booleanValue) {
            throw new ArgumentException("Provided value is not boolean!", nameof(value));
        }

        return booleanValue is false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
        if (value is not bool booleanValue) {
            throw new ArgumentException("Provided value is not boolean!", nameof(value));
        }

        return booleanValue is true;
    }
}
