using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace D4MT.UI.Converters;

public sealed class TextWrappingToAcceptsReturnConverter : IValueConverter {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
        if (value is not TextWrapping textWrapping) {
            throw new Exception("");
        }

        return textWrapping switch {
            TextWrapping.Wrap or TextWrapping.WrapWithOverflow => true,
            TextWrapping.NoWrap => false,
            _ => throw new UnreachableException("")
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
        throw new NotImplementedException("");
    }
}
