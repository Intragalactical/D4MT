using D4MT.Library;
using D4MT.Library.Common;
using D4MT.Library.Logging;
using D4MT.Library.Text;
using D4MT.UI.Common;

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace D4MT.UI.ViewModels;

public interface IProjectViewModel : IViewModel<IProjectViewModel> {
    string WindowTitle { get; }
    bool IsUpdating { get; }
    string Name { get; }
}

public sealed class ProjectViewModel : ViewModel<IProjectViewModel>, IProjectViewModel {
    private readonly IDebugLogger _logger;
    private readonly CancellationToken _cancellationToken;
    private readonly Task _updateTask;
    private readonly ITextValidator _projectNameValidator;
    private readonly IProject _project;

    private readonly ConcurrentQueue<Task<bool>?> _projectSaveQueue = new();

    public Guid Id { get; } = Guid.NewGuid();

    public bool IsUpdating { get { return _updateTask.IsCompleted is false; } }

    public string Name {
        get { return _project.Name ?? string.Empty; }
        set {
            if (_project.Name is null || !_project.Name.Equals(value, StringComparison.Ordinal)) {
                bool couldSetName = _project.TrySetName(value, _projectNameValidator);
                Debug.WriteLineIf(couldSetName is false, $"Set project name failed for value: {value}");
            }
        }
    }

    public string WindowTitle {
        get {
            return string.Format(Constants.Strings.FormatLiterals.EditorWindowTitle, _project.Name);
        }
    }

    public ProjectViewModel(IDebugLogger logger, IProject project, ITextValidator projectNameValidator, CancellationToken cancellationToken) {
        _logger = logger;
        _cancellationToken = cancellationToken;
        _projectNameValidator = projectNameValidator;
        _project = project;

        _updateTask = StartUpdateThread(cancellationToken);
    }

    private Task StartUpdateThread(CancellationToken cancellationToken) {
        return Task.Run(async () => {
            while (_cancellationToken.IsCancellationRequested is false) {
                while (
                    _cancellationToken.IsCancellationRequested is false &&
                    _projectSaveQueue.TryDequeue(out Task<bool>? projectSaveTask) &&
                    projectSaveTask is { IsCompleted: false }
                ) {
                    bool successs = await projectSaveTask;
                    _logger.LogIf(successs is false, "Could not save project!", LogLevel.Error);
                    // @TODO: throw on fail?
                }

                Thread.Sleep(1000 / 60);
            }
        }, cancellationToken);
    }

    public override bool Equals(object? obj) {
        return obj is IProjectViewModel projectViewModel && Equals(projectViewModel);
    }

    public override int GetHashCode() {
        return Id.GetHashCode();
    }

    public bool Equals(IProjectViewModel? other) {
        return other is not null && other.Id.Equals(Id);
    }
}
