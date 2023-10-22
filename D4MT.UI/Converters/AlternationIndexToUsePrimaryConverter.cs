using System;
using System.Globalization;
using System.Windows.Data;

namespace D4MT.UI.Converters;

public sealed class AlternationIndexToUsePrimaryConverter : IValueConverter {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
        if (value is not int intValue) {
            return true;
        }

        return intValue switch {
            >= 0 when intValue % 2 == 0 => true,
            >= 0 when intValue % 2 != 0 => false,
            _ => true
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
        throw new NotImplementedException();
    }
}
