using System.Diagnostics.CodeAnalysis;

namespace JoinRpg.PrimitiveTypes;

public record KogdaIgraIdentification(int Value) : SingleValueType<int>(Value), ISpanParsable<KogdaIgraIdentification>
{
    public override string ToString() => $"KogdaIgra({Value})";

    public static KogdaIgraIdentification? FromOptional(int? value) => value is int id ? new(id) : null;
    public static KogdaIgraIdentification Parse(string value, IFormatProvider? provider) => Parse(value.AsSpan(), provider);

    public static bool TryParse(string? value, IFormatProvider? provider, [MaybeNullWhen(false)] out KogdaIgraIdentification result) => TryParse(value.AsSpan(), provider, out result);

    public static KogdaIgraIdentification Parse(ReadOnlySpan<char> value, IFormatProvider? provider)
        => TryParse(value, provider, out var result) ? result : throw new ArgumentException("Could not parse supplied value.", nameof(value));

    public static bool TryParse(ReadOnlySpan<char> value, IFormatProvider? provider, [MaybeNullWhen(false)] out KogdaIgraIdentification result)
    {
        if (IdentificationParseHelper.TryParse1(value, provider, [nameof(KogdaIgraIdentification), "KogdaIgra"]) is int id)
        {
            result = new KogdaIgraIdentification(id);
            return true;
        }
        result = new KogdaIgraIdentification(0);
        return false;
    }
}
