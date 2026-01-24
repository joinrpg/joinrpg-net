
using System.Diagnostics.CodeAnalysis;

namespace JoinRpg.PrimitiveTypes;

public record TelegramId(long Id, PrefferedName? UserName) : ISpanParsable<TelegramId>
{
    public static TelegramId? FromOptional(long? id, PrefferedName? userName) => id is null ? null : new TelegramId(id.Value, userName);

    public static TelegramId? FromOptional(string? key, PrefferedName? userName) => string.IsNullOrWhiteSpace(key) ? null : new TelegramId(long.Parse(key), userName);

    public static bool TryParse([NotNullWhen(true)] ReadOnlySpan<char> value, IFormatProvider? provider, [MaybeNullWhen(false)] out TelegramId result)
    {
        ReadOnlySpan<char> val = IdentificationParseHelper.RemovePrefixes(value, [nameof(TelegramId), "Telegram"]);
        Span<Range> ranges = stackalloc Range[2];
        var count = IdentificationParseHelper.SplitIdentifier(val, ranges);
        if (count == 2
           && long.TryParse(val[ranges[0]], provider, out var i1)
           )
        {
            result = new TelegramId(i1, PrefferedName.FromOptional(val[ranges[1]].TrimStart('@').ToString()));
            return true;
        }

        if (count == 1
          && long.TryParse(val[ranges[0]], provider, out var i)
          )
        {
            result = new TelegramId(i, null);
            return true;
        }

        result = null!;
        return false;
    }

    public static TelegramId Parse(string value, IFormatProvider? provider = null) => Parse(value.AsSpan(), provider);

    public static bool TryParse(string? value, IFormatProvider? provider, [MaybeNullWhen(false)] out TelegramId result) => TryParse(value.AsSpan(), provider, out result);

    public static TelegramId Parse(ReadOnlySpan<char> value, IFormatProvider? provider)
        => TryParse(value, provider, out var result) ? result : throw new ArgumentException("Could not parse supplied value.", nameof(value));


    public override string ToString() => UserName is null ? $"Telegram({Id})" : $"Telegram({Id}, @{UserName.Value})";

}
