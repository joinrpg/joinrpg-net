using System.Diagnostics.CodeAnalysis;
using JoinRpg.Helpers;
namespace JoinRpg.PrimitiveTypes;

public record Email : SingleValueType<string>, ISpanParsable<Email>
{
    public Email(string value)
        : base(CheckEmail(value))
    {

    }
    public static string CheckEmail(string value)
    {
        if (!value.Contains('@'))
        {
            throw new ArgumentException("Incorrect email", nameof(value));
        }
        return value;
    }

    public string UserPart => Value.TakeWhile(ch => ch != '@').AsString();

    public override string ToString() => $"Email({Value})";
    public static bool TryParse(ReadOnlySpan<char> value, IFormatProvider? provider, [MaybeNullWhen(false)] out Email result)
    {
        ReadOnlySpan<char> val = IdentificationParseHelper.RemovePrefixes(value, [nameof(Email)]);
        if (val.Contains('@'))
        {
            result = new Email(val.ToString());
            return true;
        }
        result = null!;
        return false;
    }
    public static Email Parse(string value, IFormatProvider? provider = null) => Parse(value.AsSpan(), provider);

    public static bool TryParse(string? value, IFormatProvider? provider, [MaybeNullWhen(false)] out Email result) => TryParse(value.AsSpan(), provider, out result);

    public static Email Parse(ReadOnlySpan<char> value, IFormatProvider? provider)
        => TryParse(value, provider, out var result) ? result : throw new ArgumentException("Could not parse supplied value.", nameof(value));
}
