using D4MT.Library.Common;
using D4MT.Library.Exceptions;
using D4MT.Library.Serialization;

using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace D4MT.Library;

public interface IConfiguration : ISaveable {
    string? FilePath { get; }
    CultureInfo Language { get; set; }

    string? GetDirectoryPath(ConfigurationDirectory configurationDirectory);
    Func<bool>? TrySetDirectory(ConfigurationDirectory configurationDirectory, string? directoryPath);
    Task<bool>? TrySetDirectoryAsync(ConfigurationDirectory configurationDirectory, string? directoryPath, CancellationToken cancellationToken);
}

public interface IUnsafeConfiguration {
    string? ProjectsDirectoryPath { get; set; }
    string? GameDirectoryPath { get; set; }
    string? ModsDirectoryPath { get; set; }
}

[JsonSerializable(typeof(Configuration))]
public sealed class Configuration : IConfiguration, IUnsafeConfiguration, IDeserialize<IConfiguration> {
    private const FileMode SaveFileMode = FileMode.Create;
    private const FileMode ReadFileMode = FileMode.OpenOrCreate;
    private const FileAccess SaveFileAccess = FileAccess.Write;
    private const FileAccess ReadFileAccess = FileAccess.Read;
    private static readonly IEnumerable<char> InvalidFilePathCharacters = Path.GetInvalidPathChars();
    private static readonly IEnumerable<char> InvalidFileNameCharacters = Path.GetInvalidFileNameChars();

    private static bool IsInvalidFilePathCharacter(char character) {
        return InvalidFilePathCharacters.Contains(character);
    }

    private static bool IsInvalidFileNameCharacter(char character) {
        return InvalidFileNameCharacters.Contains(character);
    }

    [JsonIgnore]
    public string? FilePath { get; private set; }

    [JsonPropertyName("projectsDirectory")]
    public string? ProjectsDirectoryPath { get; set; }

    [JsonPropertyName("gameDirectory")]
    public string? GameDirectoryPath { get; set; }

    [JsonPropertyName("modsDirectory")]
    public string? ModsDirectoryPath { get; set; }

    [JsonPropertyName("language")]
    public CultureInfo Language { get; set; } = CultureInfo.InvariantCulture;

    private static JsonSerializerOptions CreateOptions() {
        JsonSerializerOptions options = new() {
            IgnoreReadOnlyFields = true,
            AllowTrailingCommas = true,
            WriteIndented = true,
        };
        options.Converters.Add(DirectoryInfoJsonConverter.Shared);
        options.Converters.Add(CultureInfoJsonConverter.Shared);
        return options;
    }

    private static bool IsInvalidFilePath(string? filePath) {
        return filePath is null ||
            Path.GetDirectoryName(filePath) is not string directoryPath ||
            Path.GetFileName(filePath) is not string fileName ||
            directoryPath.Any(IsInvalidFilePathCharacter) ||
            fileName.Any(IsInvalidFileNameCharacter);
    }

    public async Task<bool> TrySaveAsync(CancellationToken cancellationToken) {
        if (cancellationToken.IsCancellationRequested || string.IsNullOrWhiteSpace(FilePath) || IsInvalidFilePath(FilePath)) {
            return false;
        }

        FileStream fileStream = File.Open(FilePath, SaveFileMode, SaveFileAccess);
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
        if (string.IsNullOrWhiteSpace(FilePath) || IsInvalidFilePath(FilePath)) {
            return false;
        }

        using FileStream fileStream = File.Open(FilePath, SaveFileMode, SaveFileAccess);
        JsonSerializer.Serialize(
            utf8Json: fileStream,
            value: this,
            options: CreateOptions()
        );
        fileStream.Flush();
        return true;
    }

    public static async Task<IConfiguration?> DeserializeAsync(string filePath, CancellationToken cancellationToken) {
        if (cancellationToken.IsCancellationRequested || IsInvalidFilePath(filePath) || File.Exists(filePath) is false) {
            return default;
        }

        FileStream fileStream = File.Open(filePath, ReadFileMode, ReadFileAccess);
        Configuration configuration = await JsonSerializer.DeserializeAsync<Configuration>(
            utf8Json: fileStream,
            options: CreateOptions(),
            cancellationToken
        ) ?? throw D4MTConfigurationException.CouldNotDeserialize;
        configuration.FilePath = filePath;
        await fileStream.DisposeAsync();
        return configuration;
    }

    public static IConfiguration? Deserialize(string filePath) {
        if (IsInvalidFilePath(filePath) || File.Exists(filePath) is false) {
            return default;
        }

        using FileStream fileStream = File.Open(filePath, ReadFileMode, ReadFileAccess);
        Configuration configuration = JsonSerializer.Deserialize<Configuration>(
            utf8Json: fileStream,
            options: CreateOptions()
        ) ?? throw D4MTConfigurationException.CouldNotDeserialize;
        configuration.FilePath = filePath;
        return configuration;
    }

    public Func<bool>? TrySetDirectory(ConfigurationDirectory configurationDirectory, string? directoryPath) {
        string? newPath = configurationDirectory switch {
            ConfigurationDirectory.Projects => ProjectsDirectoryPath = directoryPath,
            ConfigurationDirectory.Game => GameDirectoryPath = directoryPath,
            ConfigurationDirectory.Mods => ModsDirectoryPath = directoryPath,
            _ => throw new UnreachableException("")
        };
        return newPath is not null && newPath.Equals(directoryPath, StringComparison.Ordinal) ?
            TrySave :
            null;
    }

    public Task<bool>? TrySetDirectoryAsync(ConfigurationDirectory configurationDirectory, string? directoryPath, CancellationToken cancellationToken) {
        if (cancellationToken.IsCancellationRequested) {
            return null;
        }

        string? newPath = configurationDirectory switch {
            ConfigurationDirectory.Projects => ProjectsDirectoryPath = directoryPath,
            ConfigurationDirectory.Game => GameDirectoryPath = directoryPath,
            ConfigurationDirectory.Mods => ModsDirectoryPath = directoryPath,
            _ => throw new UnreachableException("")
        };
        return newPath is not null && newPath.Equals(directoryPath, StringComparison.Ordinal) ?
            TrySaveAsync(cancellationToken) :
            null;
    }

    public string? GetDirectoryPath(ConfigurationDirectory configurationDirectory) {
        return configurationDirectory switch {
            ConfigurationDirectory.Projects => ProjectsDirectoryPath,
            ConfigurationDirectory.Game => GameDirectoryPath,
            ConfigurationDirectory.Mods => ModsDirectoryPath,
            _ => throw new UnreachableException("")
        };
    }
}
