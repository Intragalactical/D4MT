using D4MT.Library.Extensions;

using System.Text.Json;
using System.Text.Json.Serialization;

namespace D4MT.Library.Serialization;

public sealed class DirectoryInfoJsonConverter : JsonConverter<DirectoryInfo?> {
    public static readonly JsonConverter<DirectoryInfo?> Shared = new DirectoryInfoJsonConverter() {
        DefaultDirectory = null,
        DefaultDirectoryPath = string.Empty
    };

    public required string DefaultDirectoryPath { get; init; }
    public required DirectoryInfo? DefaultDirectory { get; init; }

    public override DirectoryInfo? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        return reader.GetString() is string pathString && string.IsNullOrWhiteSpace(pathString) is false ?
            pathString.ToDirectoryInfo() :
            DefaultDirectory;
    }

    public override void Write(Utf8JsonWriter writer, DirectoryInfo? value, JsonSerializerOptions options) {
        writer.WriteStringValue(value?.FullName ?? DefaultDirectoryPath);
    }
}
