using D4MT.Library.Logging;

using System.Windows;

using WpfApplication = System.Windows.Application;

namespace D4MT.UI;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : WpfApplication {
    private void App_Startup(object sender, StartupEventArgs e) {
        DebugLogger.Shared.Log("D4MT Started.");
        IDebugLogger launcherWindowLogger = DebugLogger.Shared.CreateChildFromType(typeof(LauncherWindow));
        new LauncherWindow(launcherWindowLogger).Show();
    }
}
