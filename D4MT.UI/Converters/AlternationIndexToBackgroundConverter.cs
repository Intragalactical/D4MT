using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace D4MT.UI.Converters;

public sealed class AlternationIndexToBackgroundConverter : IValueConverter {
    public Brush DefaultBackground { get; set; } = Brushes.White;
    public Brush Primary { get; set; } = Brushes.White;
    public Brush Secondary { get; set; } = Brushes.LightGray;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
        if (value is not int intValue) {
            return DefaultBackground;
        }

        return intValue switch {
            >= 0 when intValue % 2 == 0 => Primary,
            >= 0 when intValue % 2 != 0 => Secondary,
            _ => DefaultBackground
        };
        throw new NotImplementedException();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
        throw new NotImplementedException();
    }
}
