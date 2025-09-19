using System.Diagnostics.CodeAnalysis;

namespace JoinRpg.PrimitiveTypes;
public record class UserIdentification(int Value) : SingleValueType<int>(Value), ISpanParsable<UserIdentification>
{
    public static UserIdentification? FromOptional(int? userId)
        => userId is not null ? new UserIdentification(userId.Value) : null;

    public static UserIdentification Parse(string value, IFormatProvider? provider) => Parse(value.AsSpan(), provider);

    public static bool TryParse(string? value, IFormatProvider? provider, [MaybeNullWhen(false)] out UserIdentification result) => TryParse(value.AsSpan(), provider, out result);

    public static UserIdentification Parse(ReadOnlySpan<char> value, IFormatProvider? provider)
        => TryParse(value, provider, out var result) ? result : throw new ArgumentException("Could not parse supplied value.", nameof(value));
    public static bool TryParse(ReadOnlySpan<char> value, IFormatProvider? provider, [MaybeNullWhen(false)] out UserIdentification result)
    {
        if (IdentificationParseHelper.TryParse1(value, provider, [nameof(UserIdentification), "Project"]) is int id)
        {
            result = new UserIdentification(id);
            return true;
        }
        result = new UserIdentification(0);
        return false;
    }
}
