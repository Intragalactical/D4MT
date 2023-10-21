using D4MT.Library;
using D4MT.Library.Common;
using D4MT.Library.RegularExpressions;
using D4MT.Library.Text;
using D4MT.UI.Common;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;

using WpfApplication = System.Windows.Application;

namespace D4MT.UI.ViewModels;

public interface ILauncherViewModel : IViewModel<ILauncherViewModel> {
    string ProjectFilter { get; set; }
    string ProjectsDirectoryPath { get; set; }
    string GameDirectoryPath { get; set; }
    string ModsDirectoryPath { get; set; }
    string ProjectName { get; set; }
    bool ValidProjectName { get; }
    bool CanOpenProject { get; }
    bool CanCreateNewProject { get; }
    bool AreProjectsVisible { get; }
    ICollectionView FoundProjectsView { get; }
    int SelectedProjectIndex { get; }
    bool CanExit { get; }

    string GetConfigurationDirectoryPath(ConfigurationDirectory configurationDirectory);
    void SetConfigurationDirectoryPath(ConfigurationDirectory configurationDirectory, string directoryPath);
    IProject? GetSelectedProject();
    bool IsValidConfigurationDirectory(ConfigurationDirectory configurationDirectory);
    bool AreValidConfigurationDirectories(params ConfigurationDirectory[] configurationDirectories);
}

public sealed class LauncherViewModel : ViewModel<ILauncherViewModel>, ILauncherViewModel {
    private const sbyte ThreadIdleUpdateFrequencyMilliseconds = 1000 / 60;
    private static readonly SortDescription NameSortDescription = new("HumanFriendlyName", ListSortDirection.Ascending);

    private readonly IProjects _projects;
    private readonly ITextTransformer _projectNameTransformer;
    private readonly ITextValidator _projectNameValidator;
    private readonly IConfiguration _configuration;
    private readonly Task _updateTask;
    private readonly Task _saveConfigurationTask;
    private readonly CancellationToken _cancellationToken;

    #region Queues for update thread
    private readonly ConcurrentQueue<Task?> _saveConfigurationQueue;
    private readonly ConcurrentQueue<IAsyncEnumerable<IProject>> _fetchProjectsQueue;
    #endregion

    public Guid Id { get; } = Guid.NewGuid();

    private ObservableCollection<IProject> FoundProjects { get; }
    public ICollectionView FoundProjectsView { get; }

    private int _selectedProjectIndex = -1;
    public int SelectedProjectIndex {
        get { return _selectedProjectIndex; }
        set {
            if (_selectedProjectIndex != value && value < FoundProjects.Count) {
                _selectedProjectIndex = value;
                NotifyPropertiesChanged(this, nameof(SelectedProjectIndex), nameof(CanOpenProject));
            }
        }
    }

    private string? _projectsDirectoryPath;
    public string ProjectsDirectoryPath {
        get { return _projectsDirectoryPath ?? string.Empty; }
        set { SetDirectory(ConfigurationDirectory.Projects, value); }
    }

    private string? _gameDirectoryPath;
    public string GameDirectoryPath {
        get { return _gameDirectoryPath ?? string.Empty; }
        set { SetDirectory(ConfigurationDirectory.Game, value); }
    }

    private string? _modsDirectoryPath;
    public string ModsDirectoryPath {
        get { return _modsDirectoryPath ?? string.Empty; }
        set { SetDirectory(ConfigurationDirectory.Mods, value); }
    }

    private string? _projectName = null;
    public string ProjectName {
        get { return _projectName ?? string.Empty; }
        set {
            if (_projectName is null || _projectName.Equals(value) is false) {
                string transformedValue = _projectNameTransformer.Transform(value);
                _projectName = transformedValue;
                NotifyPropertiesChanged(this, nameof(ProjectName), nameof(ValidProjectName));
            }
        }
    }

    public bool ValidProjectName {
        get {
            bool invalidName = IsValidConfigurationDirectory(ConfigurationDirectory.Projects) ||
                string.IsNullOrWhiteSpace(ProjectName) ||
                _projectNameValidator.IsInvalid(ProjectName) ||
                _projects.GetByName(
                    ProjectsDirectoryPath,
                    ProjectName,
                    _projectNameValidator
                ) is null or { Exists: false };
            return invalidName is false;
        }
    }

    public bool CanCreateNewProject {
        get {
            return ValidProjectName &&
                AreValidConfigurationDirectories(ConfigurationDirectory.Projects, ConfigurationDirectory.Game, ConfigurationDirectory.Mods);
        }
    }

    public bool CanOpenProject {
        get { return GetSelectedProject() is { Exists: true }; }
    }

    public bool AreProjectsVisible {
        get { return AreValidConfigurationDirectories(ConfigurationDirectory.Projects, ConfigurationDirectory.Game, ConfigurationDirectory.Mods); }
    }

    public bool CanExit {
        get { return _cancellationToken.IsCancellationRequested && _updateTask.IsCompleted && _saveConfigurationTask.IsCompleted; }
    }

    private string? _projectFilter;

    public string ProjectFilter {
        get { return _projectFilter ?? string.Empty; }
        set {
            if (_projectFilter is null || _projectFilter.Equals(value, StringComparison.Ordinal) is false) {
                _projectFilter = value;
                NotifyPropertyChanged(this);
            }
        }
    }

    public LauncherViewModel(ITextValidator projectNameValidator, CancellationToken cancellationToken) {
        _cancellationToken = cancellationToken;
        _configuration = Configuration.Deserialize(Constants.Strings.Paths.ConfigurationFile) ?? new Configuration();

        PropertyChanged += LauncherViewModel_PropertyChanged;

        _projects = Projects.Shared;
        _projectNameTransformer = ProjectNameTransformer.Shared;
        _projectNameValidator = projectNameValidator;
        _saveConfigurationQueue = new();
        _fetchProjectsQueue = new();

        FoundProjects = new();
        FoundProjectsView = CollectionViewSource.GetDefaultView(FoundProjects);
        FoundProjectsView.SortDescriptions.Add(NameSortDescription);
        FoundProjectsView.Filter = IsProjectVisible;

        ProjectsDirectoryPath = _configuration.GetDirectoryPath(ConfigurationDirectory.Projects) ?? throw new Exception("");
        GameDirectoryPath = _configuration.GetDirectoryPath(ConfigurationDirectory.Game) ?? throw new Exception("");
        ModsDirectoryPath = _configuration.GetDirectoryPath(ConfigurationDirectory.Mods) ?? throw new Exception("");

        _updateTask = StartUpdateTask(cancellationToken);
        _saveConfigurationTask = SaveConfiguration(cancellationToken);
    }

    public string GetConfigurationDirectoryPath(ConfigurationDirectory configurationDirectory) {
        return configurationDirectory switch {
            ConfigurationDirectory.Projects => ProjectsDirectoryPath,
            ConfigurationDirectory.Game => GameDirectoryPath,
            ConfigurationDirectory.Mods => ModsDirectoryPath,
            _ => throw new UnreachableException("")
        };
    }

    public void SetConfigurationDirectoryPath(ConfigurationDirectory configurationDirectory, string directoryPath) {
        switch (configurationDirectory) {
            case ConfigurationDirectory.Projects:
                ProjectsDirectoryPath = directoryPath;
                break;
            case ConfigurationDirectory.Game:
                GameDirectoryPath = directoryPath;
                break;
            case ConfigurationDirectory.Mods:
                ModsDirectoryPath = directoryPath;
                break;
            default:
                throw new UnreachableException("");
        }
    }

    private bool IsProjectVisible(object obj) {
        if (obj is not IProject project) {
            return false;
        }

        return AreProjectsVisible &&
            project.Exists &&
            (
                project.HumanFriendlyName.Contains(ProjectFilter, StringComparison.OrdinalIgnoreCase) ||
                project.HumanFriendlyDirectoryPath.Contains(ProjectFilter, StringComparison.OrdinalIgnoreCase)
            );
    }

    private void RefreshFoundProjectsThreadSafe() {
        WpfApplication.Current.Dispatcher.Invoke(FoundProjectsView.Refresh);
    }

    public IProject? GetSelectedProject() {
        return SelectedProjectIndex is >= 0 || SelectedProjectIndex < FoundProjects.Count ?
            FoundProjects.ElementAtOrDefault(SelectedProjectIndex) :
            null;
    }

    private void LauncherViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e) {
        _ = Task.Run(e.PropertyName switch {
            nameof(ProjectName) => NotifyCanCreateNewModChanged,
            nameof(AreProjectsVisible) => EnqueueProjects,
            nameof(ProjectFilter) => RefreshFoundProjectsThreadSafe,
            _ => () => { }
        }, _cancellationToken);
    }

    public bool IsValidConfigurationDirectory(ConfigurationDirectory configurationDirectory) {
        return GetDirectoryPath(configurationDirectory) is string directoryPath &&
            Directory.Exists(directoryPath) &&
            Regexes.RestrictedPathMatcher.IsMatch(directoryPath) is false;
    }

    public bool AreValidConfigurationDirectories(params ConfigurationDirectory[] configurationDirectories) {
        return configurationDirectories.All(IsValidConfigurationDirectory);
    }

    private string? SetDirectoryPathField(ConfigurationDirectory configurationDirectory, string? value) {
        return configurationDirectory switch {
            ConfigurationDirectory.Projects => _projectsDirectoryPath = value,
            ConfigurationDirectory.Game => _gameDirectoryPath = value,
            ConfigurationDirectory.Mods => _modsDirectoryPath = value,
            _ => throw new UnreachableException("")
        };
    }

    private string? GetDirectoryPath(ConfigurationDirectory configurationDirectory) {
        return configurationDirectory switch {
            ConfigurationDirectory.Projects => _projectsDirectoryPath,
            ConfigurationDirectory.Game => _gameDirectoryPath,
            ConfigurationDirectory.Mods => _modsDirectoryPath,
            _ => throw new UnreachableException("")
        };
    }

    private void SetDirectory(ConfigurationDirectory configurationDirectory, string newDirectoryPath) {
        const StringComparison CaseSensitive = StringComparison.Ordinal;

        string? currentConfigurationDirectoryPath = _configuration.GetDirectoryPath(configurationDirectory);
        string? currentDirectoryPath = GetDirectoryPath(configurationDirectory);

        Task? saveConfigurationTask = currentConfigurationDirectoryPath?.Equals(newDirectoryPath, CaseSensitive) is not true ?
            _configuration.SetDirectoryAsync(configurationDirectory, newDirectoryPath, _cancellationToken) :
            null;
        _saveConfigurationQueue.Enqueue(saveConfigurationTask);

        if (currentDirectoryPath?.Equals(newDirectoryPath, CaseSensitive) is true) {
            return;
        }

        if (SetDirectoryPathField(configurationDirectory, newDirectoryPath)?.Equals(newDirectoryPath, CaseSensitive) is not true) {
            return;
        }

        NotifyConfigurationDirectoryChanged(configurationDirectory);
        NotifyPropertiesChanged(this, nameof(CanCreateNewProject), nameof(AreProjectsVisible));
    }

    private void EnqueueProjects() {
        IEnumerable<string> projectFilePaths = _projects.GetProjectFilePaths(ProjectsDirectoryPath);
        _fetchProjectsQueue.Enqueue(_projects.DeserializeAllAsync(projectFilePaths, _cancellationToken));
    }

    private void NotifyConfigurationDirectoryChanged(ConfigurationDirectory configurationDirectory) {
        string propertyName = configurationDirectory switch {
            ConfigurationDirectory.Projects => nameof(ProjectsDirectoryPath),
            ConfigurationDirectory.Game => nameof(GameDirectoryPath),
            ConfigurationDirectory.Mods => nameof(ModsDirectoryPath),
            _ => throw new UnreachableException("")
        };
        NotifyPropertyChanged(this, propertyName);
    }

    private void NotifyCanCreateNewModChanged() {
        NotifyPropertyChanged(this, nameof(CanCreateNewProject));
    }

    private async Task DequeueAllAndSave(CancellationToken cancellationToken) {
        bool respectCancellationToken = cancellationToken.Equals(CancellationToken.None);
        while (
            (respectCancellationToken is false || cancellationToken.IsCancellationRequested is false) &&
            _saveConfigurationQueue.TryDequeue(out Task? saveConfigurationTask) &&
            saveConfigurationTask is not null
        ) {
            await saveConfigurationTask;
        }
    }

    private Task SaveConfiguration(CancellationToken cancellationToken) {
        return Task.Run(async () => {
            if (cancellationToken.IsCancellationRequested) {
                return;
            }

            while (!_cancellationToken.IsCancellationRequested) {
                await DequeueAllAndSave(cancellationToken);
                Thread.Sleep(ThreadIdleUpdateFrequencyMilliseconds);
            }

            await DequeueAllAndSave(CancellationToken.None);
        }, cancellationToken);
    }

    private Task StartUpdateTask(CancellationToken cancellationToken) {
        return Task.Run(() => {
            while (cancellationToken.IsCancellationRequested is false) {
                while (
                    cancellationToken.IsCancellationRequested is false &&
                    _fetchProjectsQueue.TryDequeue(out IAsyncEnumerable<IProject>? projectsEnumeration) &&
                    projectsEnumeration is not null
                ) {
                    List<IProject> projects = projectsEnumeration
                        .ToBlockingEnumerable(cancellationToken)
                        .ToList(); // ToList is important, since it creates an actual collection from enumeration unlike ToBlockingEnumerable.
                    WpfApplication.Current.Dispatcher.Invoke(() => {
                        FoundProjects.Clear();
                        projects.ForEach(FoundProjects.Add);
                    });
                }

                Thread.Sleep(ThreadIdleUpdateFrequencyMilliseconds);
            }
        }, cancellationToken);
    }

    public override bool Equals(object? obj) {
        return obj is ILauncherViewModel launcherViewModel && Equals(launcherViewModel);
    }

    public override int GetHashCode() {
        return Id.GetHashCode();
    }

    public bool Equals(ILauncherViewModel? other) {
        return other is not null && other.Id.Equals(Id);
    }
}
