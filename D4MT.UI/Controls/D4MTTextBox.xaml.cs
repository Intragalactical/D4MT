using System;
using System.ComponentModel;
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

    private double _placeholderMarginTop = 0;
    public double PlaceholderMarginTop {
        get { return _placeholderMarginTop; }
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

    private bool _isPlaceholderEnabled = true;
    public bool IsPlaceholderEnabled {
        get { return _isPlaceholderEnabled; }
        set {
            if (_isPlaceholderEnabled.Equals(value) is false) {
                _isPlaceholderEnabled = value;
                NotifyPropertyChanged();
            }
        }
    }

    public bool ShowPlaceholder {
        get { return _isPlaceholderEnabled && IsTextBoxFocused is false && string.IsNullOrWhiteSpace(Text); }
    }

    private Brush _placeholderForeground = Brushes.DimGray;
    public Brush PlaceholderForeground {
        get { return _placeholderForeground; }
        set {
            if (_placeholderForeground.Equals(value) is false) {
                _placeholderForeground = value;
                NotifyPropertyChanged();
            }
        }
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

    private void D4MTTextBox_PropertyChanged(object? sender, PropertyChangedEventArgs e) {
        switch (e.PropertyName) {
            case nameof(DisableToolTipWhenNoText):
                NotifyPropertyChanged(nameof(IsToolTipEnabled));
                break;
            case nameof(IsPlaceholderEnabled):
                NotifyPropertyChanged(nameof(ShowPlaceholder));
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
    }

    private void UserControlTextBox_LostFocus(object sender, RoutedEventArgs e) {
        IsTextBoxFocused = false;
    }

    private void UserControlTextBox_GotFocus(object sender, RoutedEventArgs e) {
        IsTextBoxFocused = true;
    }
}
