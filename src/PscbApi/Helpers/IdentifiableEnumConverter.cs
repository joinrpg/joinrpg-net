// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming
// ReSharper disable CheckNamespace
using Newtonsoft.Json;

namespace PscbApi;

/// <summary>
/// Must be used for enums with values that marked with <see cref="IdentifierAttribute"/>
/// </summary>
internal class IdentifiableEnumConverter : JsonConverter
{
    /// <inheritdoc />
    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        if (value != null)
        {
            writer.WriteValue(value.GetType().IsEnum ? (value as Enum).GetIdentifier() : value);
        }
        else
        {
            writer.WriteNull();
        }
    }

    /// <inheritdoc />
    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        var nullable = false;
        Type actualType = objectType;
        if (!objectType.IsEnum)
        {
            actualType = Nullable.GetUnderlyingType(objectType);
            if (!actualType?.IsEnum ?? false)
            {
                actualType = null;
            }

            nullable = actualType != null;
        }

        var value = reader.Value;
        if (actualType != null)
        {
            if (value != null)
            {
                if (value is string sValue)
                {
                    object result = Enum.GetValues(actualType)
                        .Cast<Enum>()
                        .FirstOrDefault(v => v.GetIdentifier().Equals(sValue, StringComparison.InvariantCultureIgnoreCase));
                    if (result != null)
                    {
                        return result;
                    }
                }
            }
            else if (nullable)
            {
                return null;
            }
        }

        throw new InvalidCastException($"\"{value}\" could not be converted to {actualType?.Name ?? objectType.Name}");
    }

    /// <inheritdoc />
    public override bool CanConvert(Type objectType)
        => objectType.IsEnum;
}
