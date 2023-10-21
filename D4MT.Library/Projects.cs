using D4MT.Library.Common;
using D4MT.Library.Text;

using System.Runtime.CompilerServices;

namespace D4MT.Library;

public interface IProjects {
    IEnumerable<string> GetFilePaths(string parentDirectoryPath);
    IAsyncEnumerable<IProject> DeserializeAllAsync(IEnumerable<string> projectFilePaths, CancellationToken cancellationToken);
    IProject? GetByName(string parentDirectoryPath, string projectName, ITextValidator projectNameValidator);
    Task<IProject?> GetByNameAsync(string parentDirectoryPath, string projectName, ITextValidator projectNameValidator, CancellationToken cancellationToken);
}

public sealed class Projects : IProjects {
    public static readonly IProjects Shared = new Projects();
    private static readonly IEnumerable<char> InvalidPathCharacters = Path.GetInvalidPathChars();

    private static bool IsInvalidPathCharacter(char character) {
        return InvalidPathCharacters.Contains(character);
    }

    public IEnumerable<string> GetFilePaths(string parentDirectoryPath) {
        if (
            string.IsNullOrWhiteSpace(parentDirectoryPath) ||
            parentDirectoryPath.Any(IsInvalidPathCharacter) ||
            new DirectoryInfo(parentDirectoryPath) is not { Exists: true, Attributes: FileAttributes directoryAttributes } ||
            directoryAttributes.HasFlag(FileAttributes.System) ||
            directoryAttributes.HasFlag(FileAttributes.Hidden) ||
            directoryAttributes.HasFlag(FileAttributes.Directory) is false
        ) {
            return Array.Empty<string>();
        }

        return Directory.EnumerateFiles(
            parentDirectoryPath,
            Constants.Strings.Patterns.ProjectFileName,
            Constants.EnumerationOptions.FirstLevelRecursionNoInaccessibleSpecialSystemOrHidden
        );
    }

    public async IAsyncEnumerable<IProject> DeserializeAllAsync(IEnumerable<string> projectFilePaths, [EnumeratorCancellation] CancellationToken cancellationToken) {
        foreach (string projectFilePath in projectFilePaths) {
            if (await Project.DeserializeAsync(projectFilePath, cancellationToken) is IProject project) {
                yield return project;
            }
        }
    }

    public IProject? GetByName(string parentDirectoryPath, string projectName, ITextValidator projectNameValidator) {
        if (
            parentDirectoryPath.Any(IsInvalidPathCharacter) ||
            Directory.Exists(parentDirectoryPath) is false ||
            string.IsNullOrWhiteSpace(projectName) ||
            projectName.Any(IsInvalidPathCharacter) ||
            projectNameValidator.IsInvalid(projectName)
        ) {
            return null;
        }

        string projectFilePath = Path.Combine(parentDirectoryPath, projectName, Constants.Strings.Patterns.ProjectFileName);
        return Project.Deserialize(projectFilePath);
    }

    public async Task<IProject?> GetByNameAsync(
        string parentDirectoryPath,
        string projectName,
        ITextValidator projectNameValidator,
        CancellationToken cancellationToken
    ) {
        if (
            cancellationToken.IsCancellationRequested ||
            parentDirectoryPath.Any(IsInvalidPathCharacter) ||
            Directory.Exists(parentDirectoryPath) is false ||
            string.IsNullOrWhiteSpace(projectName) ||
            projectName.Any(IsInvalidPathCharacter) ||
            projectNameValidator.IsInvalid(projectName)
        ) {
            return null;
        }

        string projectFilePath = Path.Combine(parentDirectoryPath, projectName, Constants.Strings.Patterns.ProjectFileName);
        return await Project.DeserializeAsync(projectFilePath, cancellationToken);
    }
}
