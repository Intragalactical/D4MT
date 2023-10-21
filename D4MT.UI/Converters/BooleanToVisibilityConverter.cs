using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace D4MT.UI.Converters;

public sealed class BooleanToVisibilityConverter : IValueConverter {
    private static readonly IReadOnlyDictionary<Visibility, bool> DefaultMap = new Dictionary<Visibility, bool>(3) {
        { Visibility.Hidden, false },
        { Visibility.Collapsed, false },
        { Visibility.Visible, true }
    };

    private static readonly IReadOnlyDictionary<Visibility, bool> ReversedMap = new Dictionary<Visibility, bool>(3) {
        { Visibility.Hidden, true },
        { Visibility.Collapsed, true },
        { Visibility.Visible, false }
    };

    private static readonly IReadOnlyDictionary<bool, IReadOnlyDictionary<Visibility, bool>> Map = new Dictionary<bool, IReadOnlyDictionary<Visibility, bool>>(2) {
        { false, DefaultMap },
        { true, ReversedMap }
    };

    public Visibility DefaultVisible { get; set; } = Visibility.Visible;
    public Visibility DefaultHidden { get; set; } = Visibility.Hidden;
    public bool IsReversed { get; set; } = false;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
        return value is bool booleanValue && Map[IsReversed][Visibility.Visible] == booleanValue ? DefaultVisible : DefaultHidden;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
        return value is Visibility visibility && Map[IsReversed][visibility];
    }
}
