using System.ComponentModel;
using System.Globalization;

namespace JoinRpg.Common.PrimitiveTypes;

/// <summary>
/// Разбирает типизированный Id из строки тем же <see cref="ISpanParsable{TSelf}"/>, что и
/// <see cref="TypedEntityIdJsonConverter{T}"/>. Нужен тем, кто ищет <see cref="TypeConverter"/>, а не
/// <see cref="IParsable{TSelf}"/>, — прежде всего биндеру конфигурации, чтобы в опциях можно было хранить Id.
/// Подключается генератором через <c>[TypeConverter]</c> на самом record-типе.
/// </summary>
public sealed class TypedEntityIdTypeConverter<T> : TypeConverter where T : class, ISpanParsable<T>
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
        => sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        if (value is not string text)
        {
            return base.ConvertFrom(context, culture, value);
        }
        return T.TryParse(text.AsSpan(), culture, out var result)
            ? result
            : throw new FormatException($"Could not parse '{text}' as {typeof(T).Name}.");
    }
}
