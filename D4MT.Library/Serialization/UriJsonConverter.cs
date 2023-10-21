using System.Text.Json;
using System.Text.Json.Serialization;

namespace D4MT.Library.Serialization;

public sealed class UriJsonConverter : JsonConverter<Uri?> {
    public static readonly JsonConverter<Uri?> Shared = new UriJsonConverter() {
        UriCreationOptions = new() { DangerousDisablePathAndQueryCanonicalization = false },
        DefaultUri = null,
        DefaultUriPath = string.Empty
    };

    public required UriCreationOptions UriCreationOptions { get; set; }
    public required Uri? DefaultUri { get; set; }
    public required string DefaultUriPath { get; set; }

    public override Uri? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        if (
            reader.GetString() is not string uriPath ||
            string.IsNullOrWhiteSpace(uriPath) ||
            Uri.TryCreate(uriPath, UriCreationOptions, out Uri? resultUri) is false ||
            resultUri is null
        ) {
            return DefaultUri;
        }

        return resultUri;
    }

    public override void Write(Utf8JsonWriter writer, Uri? value, JsonSerializerOptions options) {
        writer.WriteStringValue(value?.OriginalString ?? DefaultUriPath);
    }
}
