﻿using D4MT.Library.Common;
using D4MT.Library.Exceptions;
using D4MT.Library.Text;

using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace D4MT.Library;

public interface IProject : ISaveable, IEquatable<IProject>, IComparable<IProject>, IWithEqualsOperator<IProject> {
    Guid Id { get; }
    string? DirectoryPath { get; }
    string HumanFriendlyDirectoryPath { get; }
    string? Name { get; }
    string HumanFriendlyName { get; }
    bool Exists { get; }

    bool TrySetName(string projectName, ITextValidator projectNameValidator);
}

public interface IUnsafeProject {
    Guid Id { get; set; }
}

public interface IWithEqualsOperator<T> where T : IWithEqualsOperator<T>, IEquatable<T> {
    public static virtual bool operator ==(T current, T? other) {
        return current.Equals(other);
    }

    public static virtual bool operator !=(T current, T? other) {
        return !current.Equals(other);
    }
}

public interface IProjectCreator {
    static abstract Task<IProject?> CreateAsync(
        string projectsDirectoryPath,
        string projectName,
        ITextValidator projectNameValidator,
        CancellationToken cancellationToken
    );
    static abstract IProject? Create(string projectsDirectoryPath, string projectName, ITextValidator projectNameValidator);
}

[JsonSerializable(typeof(Project))]
public sealed class Project() : IProject, IUnsafeProject, IDeserialize<IProject>, IProjectCreator {
    private const FileMode SaveFileMode = FileMode.Create;
    private const FileMode ReadFileMode = FileMode.Open;
    private const FileAccess SaveFileAccess = FileAccess.Write;
    private const FileAccess ReadFileAccess = FileAccess.Read;
    private static readonly char[] InvalidPathCharacters = Path.GetInvalidPathChars();

    private static bool IsInvalidPathCharacter(char c) {
        return InvalidPathCharacters.Contains(c);
    }

    [JsonPropertyName("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [JsonIgnore]
    private string? FilePath { get; set; }

    [JsonIgnore]
    public string? DirectoryPath {
        get { return Path.GetDirectoryName(FilePath); }
    }

    [JsonIgnore]
    public string HumanFriendlyDirectoryPath {
        get { return DirectoryPath ?? ""; }
    }

    [JsonIgnore]
    public string? Name {
        get { return Path.GetFileName(DirectoryPath); }
    }

    [JsonIgnore]
    public string HumanFriendlyName {
        get { return Name ?? ""; }
    }

    public bool Exists {
        get { return File.Exists(FilePath); }
    }

    private Project(string filePath) : this() {
        FilePath = filePath;
    }

    private static JsonSerializerOptions CreateOptions() {
        JsonSerializerOptions options = new() {
            IgnoreReadOnlyFields = true,
            AllowTrailingCommas = true,
            WriteIndented = true,
        };
        return options;
    }

    private static async Task<FileStream?> GetFileStreamWhenAccessibleAsync(
        string filePath,
        FileMode fileMode,
        FileAccess fileAccess,
        CancellationToken cancellationToken
    ) {
        const sbyte SharingViolation = 32;
        const sbyte LockViolation = 33;

        bool isAccessible = false;
        FileStream? fileStream = null;

        while (isAccessible is false || fileStream is null) {
            if (cancellationToken.IsCancellationRequested) {
                return null;
            }

            try {
                fileStream = File.Open(filePath, fileMode, fileAccess);
                isAccessible = true;
            } catch (IOException ioException) when ((ioException.HResult & ((1 << 16) - 1)) is SharingViolation or LockViolation) {
                Debug.WriteLine("Not accessible!");
                isAccessible = false;
            } finally {
                if (fileStream is not null && isAccessible is false) {
                    fileStream.Close();
                    await fileStream.DisposeAsync();
                }
            }
        }

        bool canPerformNecessaryOperations = fileAccess switch {
            FileAccess.Read => fileStream.CanRead,
            FileAccess.Write => fileStream.CanWrite,
            FileAccess.ReadWrite => fileStream.CanRead && fileStream.CanWrite,
            _ => throw new UnreachableException("")
        };
        return isAccessible && canPerformNecessaryOperations ?
            fileStream :
            null;
    }

    // @TODO: implement timeout so this doesn't get stuck infinitely
    private static FileStream? GetFileStreamWhenAccessible(string filePath, FileMode fileMode, FileAccess fileAccess) {
        const sbyte SharingViolation = 32;
        const sbyte LockViolation = 33;

        bool isAccessible = false;
        FileStream? fileStream = null;

        while (isAccessible is false || fileStream is null) {
            try {
                fileStream = File.Open(filePath, fileMode, fileAccess);
                isAccessible = true;
            } catch (IOException ioException) when ((ioException.HResult & ((1 << 16) - 1)) is SharingViolation or LockViolation) {
                Debug.WriteLine("Not accessible!");
                isAccessible = false;
            } finally {
                if (fileStream is not null && isAccessible is false) {
                    fileStream.Close();
                    fileStream.Dispose();
                }
            }
        }

        bool canPerformNecessaryOperations = fileAccess switch {
            FileAccess.Read => fileStream.CanRead,
            FileAccess.Write => fileStream.CanWrite,
            FileAccess.ReadWrite => fileStream.CanRead && fileStream.CanWrite,
            _ => throw new UnreachableException("")
        };
        return isAccessible && canPerformNecessaryOperations ?
            fileStream :
            null;
    }

    public static async Task<IProject?> DeserializeAsync(string filePath, CancellationToken cancellationToken) {
        if (
            cancellationToken.IsCancellationRequested ||
            File.Exists(filePath) is false ||
            await GetFileStreamWhenAccessibleAsync(
                filePath,
                ReadFileMode,
                ReadFileAccess,
                cancellationToken
            ) is not FileStream fileStream
        ) {
            return null;
        }

        Project project = await JsonSerializer.DeserializeAsync<Project>(
            utf8Json: fileStream,
            options: CreateOptions(),
            cancellationToken
        ) ?? throw ProjectException.CouldNotDeserialize;
        project.FilePath = filePath;
        await fileStream.DisposeAsync();
        return project;
    }

    public static IProject? Deserialize(string filePath) {
        if (
            File.Exists(filePath) is false ||
            GetFileStreamWhenAccessible(filePath, ReadFileMode, ReadFileAccess) is not FileStream fileStream
        ) {
            return null;
        }

        Project project = JsonSerializer.Deserialize<Project>(
            utf8Json: fileStream,
            options: CreateOptions()
        ) ?? throw ProjectException.CouldNotDeserialize;
        project.FilePath = filePath;
        fileStream.Dispose();
        return project;
    }

    public static async Task<IProject?> CreateAsync(
        string projectsDirectoryPath,
        string projectName,
        ITextValidator projectNameValidator,
        CancellationToken cancellationToken
    ) {
        if (
            cancellationToken.IsCancellationRequested ||
            projectsDirectoryPath.Any(IsInvalidPathCharacter) || Directory.Exists(projectsDirectoryPath) is false ||
            string.IsNullOrWhiteSpace(projectName) || projectName.Any(IsInvalidPathCharacter) || projectNameValidator.IsValid(projectName) is false
        ) {
            return null;
        }

        string projectDirectoryPath = Path.Combine(projectsDirectoryPath, projectName);
        DirectoryInfo projectDirectoryInfo = Directory.CreateDirectory(projectDirectoryPath);

        if (projectDirectoryInfo.Exists is false) {
            return null;
        }

        string projectFilePath = Path.Combine(projectDirectoryPath, Constants.Strings.Patterns.ProjectFileName);
        IProject project = new Project(projectFilePath);
        bool success = await project.TrySaveAsync(cancellationToken);
        return success ? project : null;
    }

    public static IProject? Create(string projectsDirectoryPath, string projectName, ITextValidator projectNameValidator) {
        if (
            projectsDirectoryPath.Any(IsInvalidPathCharacter) || Directory.Exists(projectsDirectoryPath) is false ||
            string.IsNullOrWhiteSpace(projectName) || projectName.Any(IsInvalidPathCharacter) || projectNameValidator.IsValid(projectName) is false
        ) {
            return null;
        }

        string projectDirectoryPath = Path.Combine(projectsDirectoryPath, projectName);
        DirectoryInfo projectDirectoryInfo = Directory.CreateDirectory(projectDirectoryPath);

        if (projectDirectoryInfo.Exists is false) {
            return null;
        }

        string projectFilePath = Path.Combine(projectDirectoryPath, Constants.Strings.Patterns.ProjectFileName);
        IProject project = new Project(projectFilePath);
        bool success = project.TrySave();
        return success ? project : null;
    }

    public async Task<bool> TrySaveAsync(CancellationToken cancellationToken) {
        if (
            cancellationToken.IsCancellationRequested ||
            string.IsNullOrWhiteSpace(FilePath) ||
            FilePath.Any(IsInvalidPathCharacter) ||
            await GetFileStreamWhenAccessibleAsync(FilePath, SaveFileMode, SaveFileAccess, cancellationToken) is not FileStream fileStream
        ) {
            return false;
        }

        await JsonSerializer.SerializeAsync(
            utf8Json: fileStream,
            value: this,
            options: CreateOptions(),
            cancellationToken
        );
        await fileStream.FlushAsync(cancellationToken);
        await fileStream.DisposeAsync();
        return true;
    }

    public bool TrySave() {
        if (
            string.IsNullOrWhiteSpace(FilePath) ||
            FilePath.Any(IsInvalidPathCharacter) ||
            GetFileStreamWhenAccessible(FilePath, SaveFileMode, SaveFileAccess) is not FileStream fileStream
        ) {
            return false;
        }

        JsonSerializer.Serialize(
            utf8Json: fileStream,
            value: this,
            options: CreateOptions()
        );
        fileStream.Flush();
        fileStream.Dispose();
        return true;
    }

    public bool TrySetName(string projectName, ITextValidator projectNameValidator) {
        if (
            Directory.Exists(DirectoryPath) is false ||
            Directory.GetParent(DirectoryPath) is not DirectoryInfo parentDirectoryInfo ||
            parentDirectoryInfo.Exists is false ||
            string.IsNullOrWhiteSpace(projectName) ||
            projectName.Any(IsInvalidPathCharacter) ||
            projectNameValidator.IsInvalid(projectName)
        ) {
            return false;
        }

        string newDirectoryPath = Path.Combine(parentDirectoryInfo.FullName, projectName);

        if (Directory.Exists(newDirectoryPath)) {
            return false;
        }

        Directory.Move(DirectoryPath, newDirectoryPath);
        return Directory.Exists(newDirectoryPath);
    }

    public bool Equals(Project? other) {
        return Equals((IProject?)other);
    }

    public override bool Equals(object? obj) {
        return obj is IProject project && Equals(project);
    }

    public override int GetHashCode() {
        return FilePath is not null ? FilePath.GetHashCode() : Id.GetHashCode();
    }

    public bool Equals(IProject? other) {
        return other is not null && other.GetHashCode().Equals(GetHashCode());
    }

    public int CompareTo(IProject? other) {
        if (other is null) {
            return -1;
        }

        return HumanFriendlyName.CompareTo(other.HumanFriendlyName);
    }
}
