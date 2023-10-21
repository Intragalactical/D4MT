using System.ComponentModel;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace D4MT.Library.Serialization;

public sealed class CultureInfoJsonConverter : JsonConverter<CultureInfo> {
    public static readonly JsonConverter<CultureInfo> Shared = new CultureInfoJsonConverter() {
        TypeDescriptorContext = null,
        Converter = new(),
        DefaultCulture = CultureInfo.InvariantCulture
    };

    public required CultureInfoConverter Converter { get; init; }
    public required ITypeDescriptorContext? TypeDescriptorContext { get; init; }
    public required CultureInfo DefaultCulture { get; init; }

    public override CultureInfo Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        return reader.GetString() is string cultureInfoString && Converter.IsValid(cultureInfoString) ?
            ((CultureInfo?)Converter.ConvertFromString(TypeDescriptorContext, DefaultCulture, cultureInfoString)) ?? DefaultCulture :
            DefaultCulture;
    }

    public override void Write(Utf8JsonWriter writer, CultureInfo value, JsonSerializerOptions options) {
        writer.WriteStringValue(Converter.ConvertToString(TypeDescriptorContext, DefaultCulture, value));
    }
}
