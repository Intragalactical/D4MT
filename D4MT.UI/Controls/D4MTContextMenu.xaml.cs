using System.Windows.Controls;

namespace D4MT.UI.Controls;

public partial class D4MTContextMenu : ContextMenu {
    public D4MTContextMenu() {
        InitializeComponent();

        DataContext = this;
    }
}
