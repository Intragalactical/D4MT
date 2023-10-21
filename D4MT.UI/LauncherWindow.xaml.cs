using D4MT.Library;
using D4MT.Library.Common;
using D4MT.Library.Text;
using D4MT.UI.Common;

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

using WindowsFormsDialogResult = System.Windows.Forms.DialogResult;
using WindowsFormsDirectoryDialog = System.Windows.Forms.FolderBrowserDialog;

namespace D4MT.UI;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class LauncherWindow : Window, IViewModelDataContext<ILauncherViewModel> {
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly WindowInteropHelper _windowInteropHelper;
    private readonly ITextValidator _projectNameValidator;
    private readonly SynchronizationContext _synchronizationContext;

    public new ILauncherViewModel DataContext {
        get {
            return base.DataContext is ILauncherViewModel launcherViewModel ?
                launcherViewModel :
                throw new Exception("");
        }
        set {
            if (base.DataContext is not ILauncherViewModel launcherViewModel || launcherViewModel.Equals(value) is false) {
                base.DataContext = value;
            }
        }
    }

    public LauncherWindow() {
        _projectNameValidator = ProjectNameValidator.Shared;
        _cancellationTokenSource = new();

        DataContext = new LauncherViewModel(
            _projectNameValidator,
            _cancellationTokenSource.Token
        );

        InitializeComponent();

        _synchronizationContext = SynchronizationContext.Current ?? throw new Exception("");
        _windowInteropHelper = new(this);
    }

    private static void OnAggregateExceptionThrow(object? exceptionOrNullObject) {
        if (exceptionOrNullObject is AggregateException exception) {
            throw exception;
        }
    }

    private static string? GetUserSelectedDirectory(string dialogTitle, string initialDirectory) {
        using WindowsFormsDirectoryDialog folderDialog = new() {
            InitialDirectory = initialDirectory,
            Description = dialogTitle,
            UseDescriptionForTitle = true
        };
        return folderDialog.ShowDialog() is WindowsFormsDialogResult.OK ?
            folderDialog.SelectedPath :
            null;
    }

    private static async Task OpenExplorerAsync(nint windowHandle, string path, CancellationToken cancellationToken) {
        if (cancellationToken.IsCancellationRequested) {
            return;
        }

        ProcessStartInfo explorerStartInfo = new() {
            FileName = Constants.Strings.Patterns.ExplorerFileName,
            WorkingDirectory = Constants.Strings.Paths.WindowsDirectory,
            WindowStyle = ProcessWindowStyle.Normal,
            ErrorDialog = true,
            ErrorDialogParentHandle = windowHandle,
            Arguments = path,
            UseShellExecute = true
        };
        Process explorerProcess = new() {
            StartInfo = explorerStartInfo
        };

        bool started = explorerProcess.Start();

        if (!started) {
            Debug.WriteLine("Explorer process not started!");
            return;
        }

        Debug.WriteLineIf(
            explorerProcess is { HasExited: false, Responding: true },
            $"Process {explorerProcess.ProcessName} (Id: {explorerProcess.Id}) started with handle {explorerProcess.Handle}."
        );

        await explorerProcess.WaitForExitAsync(cancellationToken);
        int exitCode = explorerProcess.ExitCode;
        explorerProcess.Dispose();
        Debug.WriteLine($"Process exited with code {exitCode}. Disposed.");
    }

    private void Window_Loaded(object sender, RoutedEventArgs e) {

    }

    private void BrowseProjectsDirectoryButton_Click(object sender, RoutedEventArgs e) {
        const string DialogTitle = "Select D4MT Projects Directory";
        string initialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        DataContext.ProjectsDirectoryPath = GetUserSelectedDirectory(DialogTitle, initialDirectory) ?? DataContext.ProjectsDirectoryPath;
    }

    private void BrowseGameDirectoryButton_Click(object sender, RoutedEventArgs e) {
        const string DialogTitle = "Select Democracy 4 Game Directory";
        string initialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        DataContext.GameDirectoryPath = GetUserSelectedDirectory(DialogTitle, initialDirectory) ?? DataContext.GameDirectoryPath;
    }

    private void BrowseModsDirectoryButton_Click(object sender, RoutedEventArgs e) {
        const string DialogTitle = "Select Democracy 4 Mods Directory";
        string initialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        DataContext.ModsDirectoryPath = GetUserSelectedDirectory(DialogTitle, initialDirectory) ?? DataContext.ModsDirectoryPath;
    }

    private async void CreateProjectButton_Click(object sender, RoutedEventArgs e) {
        if (
            DataContext.ProjectsDirectoryPath is null ||
            DataContext.IsValidConfigurationDirectory(ConfigurationDirectory.Projects) is false
        ) {
            throw new Exception("Projects folder is not valid!");
        }

        if (DataContext.ValidProjectName is false) {
            throw new Exception("Invalid project name!");
        }

        if (await Project.CreateAsync(
            DataContext.ProjectsDirectoryPath,
            DataContext.ProjectName,
            _projectNameValidator,
            _cancellationTokenSource.Token
        ) is not IProject project) {
            throw new Exception("Could not create new project!");
        }

        EditorWindow editorWindow = new(project, _projectNameValidator, _cancellationTokenSource);
        editorWindow.Show();
        Close();
    }

    private bool CanExit() {
        return _cancellationTokenSource.Token.IsCancellationRequested && DataContext.CanExit;
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
        IsEnabled = false;
        Visibility = Visibility.Hidden;

        bool canExit = CanExit();
        e.Cancel = canExit is false;

        if (!_cancellationTokenSource.Token.IsCancellationRequested) {
            _cancellationTokenSource.Cancel();
        }

        if (!canExit) {
            _ = Task.Run(() => {
                Thread.Sleep(250);
                Application.Current.Dispatcher.Invoke(Close);
            });
        }
    }

    private void OpenProject() {
        IProject? selectedProject = DataContext.GetSelectedProject();

        if (!DataContext.CanOpenProject || selectedProject is null) {
            return;
        }

        EditorWindow editorWindow = new(selectedProject, _projectNameValidator, _cancellationTokenSource);
        editorWindow.Show();
        Close();
    }

    private void OpenProjectButton_Click(object sender, RoutedEventArgs e) {
        OpenProject();
    }

    private void ListBoxItem_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
        Action action = e.ChangedButton switch {
            MouseButton.Left => OpenProject,
            _ => () => { }
        };
        action();
    }

    private void MenuItem_Click(object sender, RoutedEventArgs e) {
        if (!DataContext.CanOpenProject) {
            return;
        }

        IProject selectedProject = DataContext.GetSelectedProject() ?? throw new Exception("Selected project is null!");
        if (selectedProject.Exists is false || Directory.Exists(selectedProject.DirectoryPath) is false) {
            throw new Exception("Selected project does not exist!");
        }

        nint currentWindowHandle = _windowInteropHelper.Handle;
        string projectPath = selectedProject.DirectoryPath;
        _ = OpenExplorerAsync(currentWindowHandle, projectPath, _cancellationTokenSource.Token)
            .ContinueWith(t => { _synchronizationContext.Post(OnAggregateExceptionThrow, t.Exception); }, _cancellationTokenSource.Token);
    }
}
