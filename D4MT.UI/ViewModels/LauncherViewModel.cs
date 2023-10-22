using D4MT.Library;
using D4MT.Library.Common;
using D4MT.Library.Logging;
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
    string ModName { get; set; }
    string VisibleName { get; set; }
    bool SynchronizeModNameWithProjectName { get; set; }
    bool ValidVisibleName { get; }
    bool ValidProjectName { get; }
    bool ValidModName { get; }
    bool CanOpenProject { get; }
    bool CanCreateNewProject { get; }
    bool AreProjectsVisible { get; }
    ICollectionView FoundProjectsView { get; }
    int SelectedProjectIndex { get; }
    bool CanExit { get; }

    string GetConfigurationDirectoryPath(ConfigurationDirectory configurationDirectory);
    bool TrySetConfigurationDirectoryPath(ConfigurationDirectory configurationDirectory, string directoryPath);
    IProject? GetSelectedProject();
    bool IsValidConfigurationDirectory(ConfigurationDirectory configurationDirectory);
    bool AreValidConfigurationDirectories(params ConfigurationDirectory[] configurationDirectories);
}

public sealed class LauncherViewModel : ViewModel<ILauncherViewModel>, ILauncherViewModel {
    private const sbyte ThreadIdleUpdateFrequencyMilliseconds = 1000 / 60;
    private static readonly SortDescription NameSortDescription = new("HumanFriendlyName", ListSortDirection.Ascending);

    private readonly IDebugLogger _logger;
    private readonly IProjects _projects = Projects.Shared;
    private readonly ITextTransformer _projectNameTransformer = ProjectNameTransformer.Shared;
    private readonly ITextValidator _projectNameValidator;
    private readonly IConfiguration _configuration = Configuration.Deserialize(Constants.Strings.Paths.ConfigurationFile) ?? new Configuration();
    private readonly Task _updateTask;
    private readonly Task _saveConfigurationTask;
    private readonly CancellationToken _cancellationToken;
    private readonly ObservableCollection<IProject> _foundProjects = new();

    #region Queues for update thread
    private readonly ConcurrentQueue<Task<bool>?> _saveConfigurationQueue = new();
    private readonly ConcurrentQueue<IAsyncEnumerable<IProject>> _fetchProjectsQueue = new();
    #endregion

    public Guid Id { get; } = Guid.NewGuid();

    public ICollectionView FoundProjectsView { get; }

    private int _selectedProjectIndex = -1;
    public int SelectedProjectIndex {
        get { return _selectedProjectIndex; }
        set {
            if (_selectedProjectIndex != value && value < _foundProjects.Count) {
                _selectedProjectIndex = value;
                NotifyPropertiesChanged(this, nameof(SelectedProjectIndex), nameof(CanOpenProject));
            }
        }
    }

    private string? _projectsDirectoryPath;
    public string ProjectsDirectoryPath {
        get { return _projectsDirectoryPath ?? string.Empty; }
        set {
            bool success = TrySetDirectory(ConfigurationDirectory.Projects, value);
            _logger.LogIf(success is false, $"ConfigurationDirectory {ConfigurationDirectory.Projects} was not set - presumably because it had the same value.");
            // @TODO: throw on fail?
        }
    }

    private string? _gameDirectoryPath;
    public string GameDirectoryPath {
        get { return _gameDirectoryPath ?? string.Empty; }
        set {
            bool success = TrySetDirectory(ConfigurationDirectory.Game, value);
            _logger.LogIf(success is false, $"ConfigurationDirectory {ConfigurationDirectory.Game} was not set - presumably because it had the same value.");
            // @TODO: throw on fail?
        }
    }

    private string? _modsDirectoryPath;
    public string ModsDirectoryPath {
        get { return _modsDirectoryPath ?? string.Empty; }
        set {
            bool success = TrySetDirectory(ConfigurationDirectory.Mods, value);
            _logger.LogIf(success is false, $"ConfigurationDirectory {ConfigurationDirectory.Mods} was not set - presumably because it had the same value.");
            // @TODO: throw on fail?
        }
    }

    private string? _projectName = null;
    public string ProjectName {
        get { return _projectName ?? string.Empty; }
        set {
            if (_projectName is null || _projectName.Equals(value, StringComparison.Ordinal) is false) {
                string transformedValue = _projectNameTransformer.Transform(value);
                _projectName = transformedValue;
                NotifyPropertiesChanged(this, nameof(ProjectName), nameof(ModName), nameof(ValidProjectName), nameof(CanCreateNewProject));
                if (SynchronizeModNameWithProjectName) {
                    ModName = transformedValue;
                }
            }
        }
    }

    private string? _modName = null;
    public string ModName {
        get { return _modName ?? string.Empty; }
        set {
            if (_modName is null || _modName.Equals(value, StringComparison.Ordinal) is false) {
                string transformedValue = _projectNameTransformer.Transform(value);
                _modName = transformedValue;
                NotifyPropertiesChanged(this, nameof(ModName), nameof(ValidModName), nameof(CanCreateNewProject));
            }
        }
    }

    private string? _visibleName = null;
    public string VisibleName {
        get { return _visibleName ?? string.Empty; }
        set {
            if (_visibleName is null || _visibleName.Equals(value, StringComparison.Ordinal) is false) {
                _visibleName = value; // @TODO: add text transformer
                NotifyPropertiesChanged(this, nameof(VisibleName), nameof(ValidVisibleName), nameof(CanCreateNewProject));
            }
        }
    }

    private bool _synchronizeModNameWithProjectName = true;
    public bool SynchronizeModNameWithProjectName {
        get { return _synchronizeModNameWithProjectName; }
        set {
            if (_synchronizeModNameWithProjectName.Equals(value) is false) {
                _synchronizeModNameWithProjectName = value;
                NotifyPropertyChanged(this);
            }
            if (value is true) {
                ModName = ProjectName;
            }
        }
    }
    public bool ValidVisibleName {
        get { return string.IsNullOrWhiteSpace(VisibleName) is false; } // @TODO: validate
    }

    public bool ValidProjectName {
        get {
            return IsValidConfigurationDirectory(ConfigurationDirectory.Projects) &&
                string.IsNullOrWhiteSpace(ProjectName) is false &&
                _projectNameValidator.IsValid(ProjectName) &&
                _projects.GetByName(
                    ProjectsDirectoryPath,
                    ProjectName,
                    _projectNameValidator
                ) is null or { Exists: false };
        }
    }

    public bool ValidModName {
        get { return _projectNameValidator.IsValid(ProjectName); }
    }

    public bool CanCreateNewProject {
        get {
            bool validConfigurationDirectories =
                AreValidConfigurationDirectories(ConfigurationDirectory.Projects, ConfigurationDirectory.Game, ConfigurationDirectory.Mods);
            _logger.LogIf(ValidProjectName is false, "CanCreateNewProject is false because project name is not valid.", LogLevel.Trace);
            _logger.LogIf(ValidModName is false, "CanCreateNewProject is false because mod name is not valid.", LogLevel.Trace);
            _logger.LogIf(ValidVisibleName is false, "CanCreateNewProject is false because visible name is not valid.", LogLevel.Trace);
            _logger.LogIf(validConfigurationDirectories is false, "CanCreateNewProject is false because configuration directories are not valid.", LogLevel.Trace);
            return ValidProjectName && ValidModName && ValidVisibleName && validConfigurationDirectories;
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

    public LauncherViewModel(IDebugLogger logger, ITextValidator projectNameValidator, CancellationToken cancellationToken) {
        PropertyChanged += LauncherViewModel_PropertyChanged;

        _cancellationToken = cancellationToken;
        _logger = logger;

        _projectNameValidator = projectNameValidator;

        FoundProjectsView = CollectionViewSource.GetDefaultView(_foundProjects);
        FoundProjectsView.SortDescriptions.Add(NameSortDescription);
        FoundProjectsView.Filter = IsProjectVisible;

        ProjectsDirectoryPath = _configuration.GetDirectoryPath(ConfigurationDirectory.Projects) ?? throw new Exception("");
        GameDirectoryPath = _configuration.GetDirectoryPath(ConfigurationDirectory.Game) ?? throw new Exception("");
        ModsDirectoryPath = _configuration.GetDirectoryPath(ConfigurationDirectory.Mods) ?? throw new Exception("");

        _updateTask = StartUpdateTask(cancellationToken);
        _saveConfigurationTask = SaveConfiguration(cancellationToken);

        _logger.Log("LauncherViewModel constructed.");
    }

    public string GetConfigurationDirectoryPath(ConfigurationDirectory configurationDirectory) {
        return configurationDirectory switch {
            ConfigurationDirectory.Projects => ProjectsDirectoryPath,
            ConfigurationDirectory.Game => GameDirectoryPath,
            ConfigurationDirectory.Mods => ModsDirectoryPath,
            _ => throw new UnreachableException("")
        };
    }

    public bool TrySetConfigurationDirectoryPath(ConfigurationDirectory configurationDirectory, string directoryPath) {
        string newDirectoryPath = configurationDirectory switch {
            ConfigurationDirectory.Projects => ProjectsDirectoryPath = directoryPath,
            ConfigurationDirectory.Game => GameDirectoryPath = directoryPath,
            ConfigurationDirectory.Mods => ModsDirectoryPath = directoryPath,
            _ => throw new UnreachableException("")
        };
        return newDirectoryPath.Equals(directoryPath, StringComparison.Ordinal) is true;
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
        return SelectedProjectIndex is >= 0 || SelectedProjectIndex < _foundProjects.Count ?
            _foundProjects.ElementAtOrDefault(SelectedProjectIndex) :
            null;
    }

    private void LauncherViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e) {
        _ = Task.Run(e.PropertyName switch {
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

    private bool TrySetDirectory(ConfigurationDirectory configurationDirectory, string newDirectoryPath) {
        const StringComparison CaseSensitive = StringComparison.Ordinal;

        string? currentConfigurationDirectoryPath = _configuration.GetDirectoryPath(configurationDirectory);
        string? currentDirectoryPath = GetDirectoryPath(configurationDirectory);

        Task<bool>? saveConfigurationTask = currentConfigurationDirectoryPath?.Equals(newDirectoryPath, CaseSensitive) is not true ?
            _configuration.TrySetDirectoryAsync(configurationDirectory, newDirectoryPath, _cancellationToken) :
            null;
        _saveConfigurationQueue.Enqueue(saveConfigurationTask);

        if (currentDirectoryPath?.Equals(newDirectoryPath, CaseSensitive) is true) {
            return false;
        }

        if (SetDirectoryPathField(configurationDirectory, newDirectoryPath)?.Equals(newDirectoryPath, CaseSensitive) is not true) {
            return false;
        }

        NotifyConfigurationDirectoryChanged(configurationDirectory);
        NotifyPropertiesChanged(this, nameof(CanCreateNewProject), nameof(AreProjectsVisible));
        return true;
    }

    private void EnqueueProjects() {
        IEnumerable<string> projectFilePaths = _projects.GetFilePaths(ProjectsDirectoryPath);
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

    private async Task DequeueAllAndSave(CancellationToken cancellationToken) {
        bool respectCancellationToken = cancellationToken.Equals(CancellationToken.None);
        while (
            (respectCancellationToken is false || cancellationToken.IsCancellationRequested is false) &&
            _saveConfigurationQueue.TryDequeue(out Task<bool>? saveConfigurationTask) &&
            saveConfigurationTask is not null
        ) {
            bool success = await saveConfigurationTask;
            _logger.LogIf(success is false, "Could not save configuration!", LogLevel.Error);
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
                        _foundProjects.Clear();
                        projects.ForEach(_foundProjects.Add);
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
