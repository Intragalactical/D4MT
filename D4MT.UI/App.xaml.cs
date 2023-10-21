using System.Windows;

using WpfApplication = System.Windows.Application;

namespace D4MT.UI;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : WpfApplication {
    private void App_Startup(object sender, StartupEventArgs e) {
        new LauncherWindow().Show();
    }
}
