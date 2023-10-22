using System.Collections;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace D4MT.UI.Controls;

public partial class D4MTListBox : UserControl, INotifyPropertyChanged {
    public static readonly DependencyProperty SelectedIndexProperty =
        DependencyProperty.Register(
            nameof(SelectedIndex),
            typeof(int),
            typeof(D4MTListBox),
            new(-1)
        );
    public static readonly DependencyProperty ItemsSourceProperty =
        DependencyProperty.Register(
            nameof(ItemsSource),
            typeof(IEnumerable),
            typeof(D4MTListBox)
        );
    public static readonly DependencyProperty ItemTemplateProperty =
        DependencyProperty.Register(
            nameof(ItemTemplate),
            typeof(DataTemplate),
            typeof(D4MTListBox)
        );
    public static readonly DependencyProperty ItemContextMenuProperty =
        DependencyProperty.Register(
            nameof(ItemContextMenu),
            typeof(D4MTContextMenu),
            typeof(D4MTListBox)
        );
    public static readonly DependencyProperty AlternationDefaultBackgroundProperty =
        DependencyProperty.Register(
            nameof(AlternationDefaultBackground),
            typeof(Brush),
            typeof(D4MTListBox),
            new(Brushes.White)
        );
    public static readonly DependencyProperty AlternationPrimaryBackgroundProperty =
        DependencyProperty.Register(
            nameof(AlternationPrimaryBackground),
            typeof(Brush),
            typeof(D4MTListBox),
            new(Brushes.White)
        );
    public static readonly DependencyProperty AlternationSecondaryBackgroundProperty =
        DependencyProperty.Register(
            nameof(AlternationSecondaryBackground),
            typeof(Brush),
            typeof(D4MTListBox),
            new(Brushes.WhiteSmoke)
        );
    public static readonly DependencyProperty ItemHoverBackgroundProperty =
        DependencyProperty.Register(
            nameof(ItemHoverBackground),
            typeof(Brush),
            typeof(D4MTListBox),
            new(new SolidColorBrush(Color.FromRgb(160, 239, 255)))
        );
    public static readonly DependencyProperty ItemSelectedBackgroundProperty =
        DependencyProperty.Register(
            nameof(ItemSelectedBackground),
            typeof(Brush),
            typeof(D4MTListBox),
            new(new SolidColorBrush(Color.FromRgb(160, 255, 160)))
        );

    public event PropertyChangedEventHandler? PropertyChanged;
    public event MouseButtonEventHandler? ItemDoubleClick;

    private bool _isAlternationEnabled = true;
    public bool IsAlternationEnabled {
        get { return _isAlternationEnabled; }
        set {
            if (_isAlternationEnabled.Equals(value) is false) {
                _isAlternationEnabled = value;
                NotifyPropertyChanged();
            }
        }
    }

    private ScrollBarVisibility _verticalScrollBarVisibility = ScrollBarVisibility.Visible;
    public ScrollBarVisibility VerticalScrollBarVisibility {
        get { return _verticalScrollBarVisibility; }
        set {
            if (_verticalScrollBarVisibility.Equals(value) is false) {
                _verticalScrollBarVisibility = value;
                NotifyPropertyChanged();
            }
        }
    }

    private ScrollBarVisibility _horizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
    public ScrollBarVisibility HorizontalScrollBarVisibility {
        get { return _horizontalScrollBarVisibility; }
        set {
            if (_horizontalScrollBarVisibility.Equals(value) is false) {
                _horizontalScrollBarVisibility = value;
                NotifyPropertyChanged();
            }
        }
    }

    private SelectionMode _selectionMode = SelectionMode.Single;
    public SelectionMode SelectionMode {
        get { return _selectionMode; }
        set {
            if (_selectionMode.Equals(value) is false) {
                _selectionMode = value;
                NotifyPropertyChanged();
            }
        }
    }

    private bool _isSynchronizedWithCurrentItem;
    public bool IsSynchronizedWithCurrentItem {
        get { return _isSynchronizedWithCurrentItem; }
        set {
            if (_isSynchronizedWithCurrentItem.Equals(value) is false) {
                _isSynchronizedWithCurrentItem = value;
                NotifyPropertyChanged();
            }
        }
    }

    private Thickness _padding = new(4, 2, 4, 2);
    public new Thickness Padding {
        get { return _padding; }
        set {
            if (_padding.Equals(value) is false) {
                _padding = value;
                NotifyPropertyChanged();
            }
        }
    }

    private Thickness _itemPadding = new(5, 5, 5, 5);
    public Thickness ItemPadding {
        get { return _itemPadding; }
        set {
            if (_itemPadding.Equals(value) is false) {
                _itemPadding = value;
                NotifyPropertyChanged();
            }
        }
    }

    public int SelectedIndex {
        get { return (int)GetValue(SelectedIndexProperty); }
        set { SetValue(SelectedIndexProperty, value); }
    }

    public IEnumerable ItemsSource {
        get { return (IEnumerable)GetValue(ItemsSourceProperty); }
        set { SetValue(ItemsSourceProperty, value); }
    }

    public DataTemplate ItemTemplate {
        get { return (DataTemplate)GetValue(ItemTemplateProperty); }
        set { SetValue(ItemTemplateProperty, value); }
    }

    public D4MTContextMenu ItemContextMenu {
        get { return (D4MTContextMenu)GetValue(ItemContextMenuProperty); }
        set { SetValue(ItemContextMenuProperty, value); }
    }

    public Brush AlternationDefaultBackground {
        get { return (Brush)GetValue(AlternationDefaultBackgroundProperty); }
        set { SetValue(AlternationDefaultBackgroundProperty, value); }
    }

    public Brush AlternationPrimaryBackground {
        get { return (Brush)GetValue(AlternationPrimaryBackgroundProperty); }
        set { SetValue(AlternationPrimaryBackgroundProperty, value); }
    }

    public Brush AlternationSecondaryBackground {
        get { return (Brush)GetValue(AlternationSecondaryBackgroundProperty); }
        set { SetValue(AlternationSecondaryBackgroundProperty, value); }
    }

    public Brush ItemHoverBackground {
        get { return (Brush)GetValue(ItemHoverBackgroundProperty); }
        set { SetValue(ItemHoverBackgroundProperty, value); }
    }

    public Brush ItemSelectedBackground {
        get { return (Brush)GetValue(ItemSelectedBackgroundProperty); }
        set { SetValue(ItemSelectedBackgroundProperty, value); }
    }

    public D4MTListBox() {
        InitializeComponent();

        DataContext = this;
    }

    private void NotifyPropertyChanged([CallerMemberName] string? propertyName = null) {
        PropertyChanged?.Invoke(this, new(propertyName));
    }

    private void ListBoxItem_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
        ItemDoubleClick?.Invoke(sender, e);
    }
}
