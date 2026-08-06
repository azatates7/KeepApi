using System.Text.Json;
using System.Text.Json.Serialization;

namespace KeepApi.Common
{
    public sealed class TrimStringJsonConverter : JsonConverter<string?>
    {
        public override string? Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            return reader.GetString()?.Trim();
        }

        public override void Write(
            Utf8JsonWriter writer,
            string? value,
            JsonSerializerOptions options)
        {
            writer.WriteStringValue(value);
        }
    }
}
