using System.Globalization;
using Newtonsoft.Json;

namespace PscbApi;

/// <summary>
/// Reads a nullable numeric enum that the bank sends as a string, and yields <c>null</c> instead of
/// throwing when the value carries no code.
/// </summary>
/// <remarks>
/// The bank writes the literal text <c>"null"</c> into <c>lastError.subCode</c> (and
/// <c>lastError.code</c>) for payments that have no processing-center code. Newtonsoft hands that
/// text to <c>Enum.Parse</c>, which throws <c>ArgumentException: Requested value 'null' was not
/// found</c> and takes the whole response down with it. Observed on the production endpoint
/// 2026-10-21 at <c>payments[109].lastError.subCode</c>.
/// <para>
/// Falling back to <c>null</c> matches what <see cref="ProtocolHelper.ParseDescriptionString"/>
/// already does for the same two codes when they arrive inside a description string:
/// <c>int.TryParse(value, out var parsed) ? parsed : (int?)null</c>.
/// </para>
/// </remarks>
internal class NullableNumericEnumConverter : JsonConverter
{
    /// <inheritdoc />
    public override bool CanConvert(Type objectType)
        => Nullable.GetUnderlyingType(objectType) is { IsEnum: true };

    /// <inheritdoc />
    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        var enumType = Nullable.GetUnderlyingType(objectType)!;

        switch (reader.TokenType)
        {
            case JsonToken.Integer:
                return Enum.ToObject(enumType, Convert.ToInt64(reader.Value, CultureInfo.InvariantCulture));
            case JsonToken.String:
                var text = ((string?)reader.Value)?.Trim();
                // The bank spells "no code" as the text "null", and sometimes as an empty string.
                if (string.IsNullOrEmpty(text) || string.Equals(text, "null", StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }
                return long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
                    ? Enum.ToObject(enumType, parsed)
                    : null;
            default:
                return null;
        }
    }

    /// <inheritdoc />
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value is null)
        {
            writer.WriteNull();
        }
        else
        {
            writer.WriteValue(Convert.ToInt64(value, CultureInfo.InvariantCulture));
        }
    }
}
