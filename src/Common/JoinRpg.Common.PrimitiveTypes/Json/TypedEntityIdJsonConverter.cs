using System.Text.Json;
using System.Text.Json.Serialization;

namespace JoinRpg.Common.PrimitiveTypes;

/// <summary>
/// Сериализует типизированный Id одной строкой (тот же формат, что <see cref="object.ToString"/> и
/// <see cref="ISpanParsable{TSelf}"/>), вместо вложенного объекта.
/// Подключается генератором через <c>[JsonConverter]</c> на самом record-типе.
/// </summary>
public sealed class TypedEntityIdJsonConverter<T> : JsonConverter<T> where T : class, ISpanParsable<T>
{
    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException($"Expected string value for {typeToConvert.Name}, got {reader.TokenType}.");
        }

        var value = reader.GetString();
        if (value != null && T.TryParse(value.AsSpan(), provider: null, out var result))
        {
            return result;
        }

        throw new JsonException($"Could not parse '{value}' as {typeToConvert.Name}.");
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString());

    public override T ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (value != null && T.TryParse(value.AsSpan(), provider: null, out var result))
        {
            return result;
        }

        throw new JsonException($"Could not parse property name '{value}' as {typeToConvert.Name}.");
    }

    public override void WriteAsPropertyName(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        => writer.WritePropertyName(value.ToString() ?? throw new JsonException($"{typeof(T).Name}.ToString() returned null."));
}
