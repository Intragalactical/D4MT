using D4MT.Library;
using D4MT.Library.Logging;
using D4MT.Library.Text;
using D4MT.UI.Common;
using D4MT.UI.ViewModels;

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace D4MT.UI;

public partial class EditorWindow : Window, IViewModelDataContext<IProjectViewModel> {
    private readonly IDebugLogger _logger;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly ITextValidator _projectNameValidator;
    private LauncherWindow? _launcherWindow;

    public new IProjectViewModel DataContext {
        get {
            return base.DataContext is IProjectViewModel projectViewModel ?
                projectViewModel :
                throw new Exception("");
        }
        set {
            if (base.DataContext is not IProjectViewModel projectViewModel || projectViewModel.Equals(value) is false) {
                base.DataContext = value;
            }
        }
    }

    public EditorWindow(IDebugLogger logger, IProject project, ITextValidator projectNameValidator, CancellationTokenSource cancellationTokenSource) {
        _logger = logger;
        _cancellationTokenSource = cancellationTokenSource;
        _projectNameValidator = projectNameValidator;
        DataContext = new ProjectViewModel(
            logger.CreateChildFromType(typeof(ProjectViewModel)),
            project,
            _projectNameValidator,
            _cancellationTokenSource.Token
        );

        InitializeComponent();
    }

    private bool CanExit() {
        return _cancellationTokenSource.Token.IsCancellationRequested && _launcherWindow is not null and { ShowActivated: true };
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
        IsEnabled = false;
        Visibility = Visibility.Hidden;

        bool canExit = CanExit();
        e.Cancel = canExit is false;

        if (_cancellationTokenSource.Token.IsCancellationRequested is false) {
            _cancellationTokenSource.Cancel();
        }

        if (_launcherWindow is null or { ShowActivated: false }) {
            _launcherWindow ??= new LauncherWindow(DebugLogger.Shared.CreateChildFromType(typeof(LauncherWindow)));
            _launcherWindow.Show();
        }

        if (!canExit) {
            _ = Task.Run(() => {
                Thread.Sleep(250);
                Application.Current.Dispatcher.Invoke(Close);
            });
        }
    }

    private void Window_Loaded(object sender, RoutedEventArgs e) {
        _logger.Log($"EditorWindow loaded with project {DataContext.Name}.");
    }
}
