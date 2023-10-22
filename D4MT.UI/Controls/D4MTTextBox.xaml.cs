using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace D4MT.UI.Controls;

public partial class D4MTTextBox : UserControl, INotifyPropertyChanged {
    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(
            nameof(Text),
            typeof(string),
            typeof(D4MTTextBox)
        );
    public static readonly DependencyProperty DisabledBackgroundProperty =
        DependencyProperty.Register(
            nameof(DisabledBackground),
            typeof(Brush),
            typeof(D4MTTextBox),
            new(Brushes.LightGray)
        );
    public static readonly DependencyProperty PlaceholderForegroundProperty =
        DependencyProperty.Register(
            nameof(PlaceholderForeground),
            typeof(Brush),
            typeof(D4MTTextBox),
            new(Brushes.LightGray)
        );
    public static readonly DependencyProperty IsPlaceholderEnabledProperty =
        DependencyProperty.Register(
            nameof(IsPlaceholderEnabled),
            typeof(bool),
            typeof(D4MTTextBox),
            new(true, OnIsPlaceholderEnabledChanged)
        );

    public event TextChangedEventHandler? TextChanged;
    public event PropertyChangedEventHandler? PropertyChanged;


    private AlignmentX _placeholderHorizontalAlignment = AlignmentX.Left;
    public AlignmentX PlaceholderHorizontalAlignment {
        get { return _placeholderHorizontalAlignment; }
        set {
            if (_placeholderHorizontalAlignment.Equals(value) is false) {
                _placeholderHorizontalAlignment = value;
                NotifyPropertyChanged();
            }
        }
    }

    public AlignmentY PlaceholderVerticalAlignment {
        get {
            return VerticalTextAlignment switch {
                VerticalAlignment.Top => AlignmentY.Top,
                VerticalAlignment.Center or VerticalAlignment.Stretch => AlignmentY.Center,
                VerticalAlignment.Bottom => AlignmentY.Bottom,
                _ => throw new UnreachableException("")
            };
        }
    }

    private double _placeholderMarginTop = 4;
    public double PlaceholderMarginTop {
        get { return PlaceholderVerticalAlignment != AlignmentY.Center ? _placeholderMarginTop : 0; }
        set {
            if (_placeholderMarginTop.Equals(value) is false) {
                _placeholderMarginTop = value;
                NotifyPropertyChanged();
            }
        }
    }

    private double _placeholderMarginLeft = 2;
    public double PlaceholderMarginLeft {
        get { return _placeholderMarginLeft; }
        set {
            if (_placeholderMarginLeft.Equals(value) is false) {
                _placeholderMarginLeft = value;
                NotifyPropertyChanged();
            }
        }
    }

    private ScrollBarVisibility _verticalScrollBarVisibility = ScrollBarVisibility.Disabled;
    public ScrollBarVisibility VerticalScrollBarVisibility {
        get { return _verticalScrollBarVisibility; }
        set {
            if (_verticalScrollBarVisibility.Equals(value) is false) {
                _verticalScrollBarVisibility = value;
                NotifyPropertyChanged();
            }
        }
    }

    private VerticalAlignment _verticalTextAlignment = VerticalAlignment.Center;
    public VerticalAlignment VerticalTextAlignment {
        get { return _verticalTextAlignment; }
        set {
            if (_verticalTextAlignment.Equals(value) is false) {
                _verticalTextAlignment = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(PlaceholderVerticalAlignment));
                NotifyPropertyChanged(nameof(PlaceholderMarginTop));
            }
        }
    }

    private TextWrapping _textWrapping = TextWrapping.NoWrap;
    public TextWrapping TextWrapping {
        get { return _textWrapping; }
        set {
            if (_textWrapping.Equals(value) is false) {
                _textWrapping = value;
                NotifyPropertyChanged();
            }
        }
    }

    private bool _disableToolTipWhenNoText = true;
    public bool DisableToolTipWhenNoText {
        get { return _disableToolTipWhenNoText; }
        set {
            if (_disableToolTipWhenNoText.Equals(value) is false) {
                _disableToolTipWhenNoText = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(IsToolTipEnabled));
            }
        }
    }

    public bool IsToolTipEnabled {
        get {
            return DisableToolTipWhenNoText is false ||
                (DisableToolTipWhenNoText && ToolTip is string toolTipText && string.IsNullOrWhiteSpace(toolTipText) is false);
        }
    }

    public bool IsPlaceholderEnabled {
        get { return (bool)GetValue(IsPlaceholderEnabledProperty); }
        set {
            SetValue(IsPlaceholderEnabledProperty, value);
            NotifyPropertyChanged();
        }
    }

    public bool ShowPlaceholder {
        get { return IsPlaceholderEnabled && IsTextBoxFocused is false && string.IsNullOrWhiteSpace(Text); }
    }

    public Brush PlaceholderForeground {
        get { return (Brush)GetValue(PlaceholderForegroundProperty); }
        set { SetValue(PlaceholderForegroundProperty, value); }
    }

    public Brush DisabledBackground {
        get { return (Brush)GetValue(DisabledBackgroundProperty); }
        set { SetValue(DisabledBackgroundProperty, value); }
    }

    private string? _placeholderText;
    public string PlaceholderText {
        get { return _placeholderText ?? string.Empty; }
        set {
            if (_placeholderText is null || _placeholderText.Equals(value, StringComparison.Ordinal) is false) {
                _placeholderText = value;
                NotifyPropertyChanged();
            }
        }
    }

    private bool _isTextBoxFocused = false;
    private bool IsTextBoxFocused {
        get { return _isTextBoxFocused; }
        set {
            if (_isTextBoxFocused.Equals(value) is false) {
                _isTextBoxFocused = value;
                NotifyPropertyChanged();
            }
        }
    }

    public string Text {
        get { return (string)GetValue(TextProperty); }
        set { SetValue(TextProperty, value); }
    }

    public D4MTTextBox() {
        InitializeComponent();

        PropertyChanged += D4MTTextBox_PropertyChanged;
        DataContext = this;
    }

    private static void OnIsPlaceholderEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        if (d is not D4MTTextBox d4mtTextBox) {
            return;
        }

        d4mtTextBox.UpdateShowPlaceholder();
    }

    private void UpdateShowPlaceholder() {
        NotifyPropertyChanged(nameof(ShowPlaceholder));
    }

    private void D4MTTextBox_PropertyChanged(object? sender, PropertyChangedEventArgs e) {
        switch (e.PropertyName) {
            case nameof(DisableToolTipWhenNoText):
                NotifyPropertyChanged(nameof(IsToolTipEnabled));
                break;
            case nameof(IsTextBoxFocused):
                NotifyPropertyChanged(nameof(ShowPlaceholder));
                break;
            default:
                break;
        }
    }

    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e) {
        base.OnPropertyChanged(e);

        if (e is not { Property.Name: string propertyName }) {
            return;
        }

        switch (propertyName) {
            case nameof(Text):
                NotifyPropertyChanged(nameof(ShowPlaceholder));
                NotifyPropertyChanged(nameof(IsToolTipEnabled));
                break;
            case nameof(Background):
                NotifyPropertyChanged(propertyName);
                break;
            case nameof(ToolTip):
                NotifyPropertyChanged(nameof(IsToolTipEnabled));
                break;
            default:
                break;
        }
    }

    private void NotifyPropertyChanged([CallerMemberName] string? propertyName = null) {
        PropertyChanged?.Invoke(this, new(propertyName));
    }

    private void UserControlTextBox_TextChanged(object sender, TextChangedEventArgs e) {
        Text = UserControlTextBox.Text;
        TextChanged?.Invoke(this, e);
    }

    private void UserControlTextBox_LostFocus(object sender, RoutedEventArgs e) {
        IsTextBoxFocused = false;
    }

    private void UserControlTextBox_GotFocus(object sender, RoutedEventArgs e) {
        IsTextBoxFocused = true;
    }
}
