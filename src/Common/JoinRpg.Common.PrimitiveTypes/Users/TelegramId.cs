
using System.Diagnostics.CodeAnalysis;

namespace JoinRpg.Common.PrimitiveTypes;

public record TelegramId(long Id, PrefferedName? UserName) : ISpanParsable<TelegramId>
{
    public static TelegramId? FromOptional(long? id, PrefferedName? userName) => id is null ? null : new TelegramId(id.Value, NormalizeUserName(userName));

    public static TelegramId? FromOptional(string? key, PrefferedName? userName) => string.IsNullOrWhiteSpace(key) ? null : new TelegramId(long.Parse(key), NormalizeUserName(userName));

    public static bool TryParse([NotNullWhen(true)] ReadOnlySpan<char> value, IFormatProvider? provider, [MaybeNullWhen(false)] out TelegramId result)
    {
        // Не используем общий IdentificationParseHelper.SplitIdentifier — он делит и по ',', и по
        // '-', а chat id каналов/супергрупп в Telegram отрицательный, так что '-' здесь не
        // разделитель, а часть числа. TelegramId делит только по ',' (перед именем пользователя).
        ReadOnlySpan<char> val = IdentificationParseHelper.RemovePrefixes(value, [nameof(TelegramId), "Telegram"]);

        var commaIndex = val.IndexOf(',');
        if (commaIndex < 0)
        {
            if (long.TryParse(val.Trim(), provider, out var i))
            {
                result = new TelegramId(i, null);
                return true;
            }

            result = null!;
            return false;
        }

        if (long.TryParse(val[..commaIndex].Trim(), provider, out var i1))
        {
            var usernameSpan = val[(commaIndex + 1)..].Trim().TrimStart('@');
            result = new TelegramId(i1, string.IsNullOrWhiteSpace(usernameSpan.ToString()) ? null : new PrefferedName(usernameSpan.ToString()));
            return true;
        }

        result = null!;
        return false;
    }

    public static TelegramId Parse(string value, IFormatProvider? provider = null) => Parse(value.AsSpan(), provider);

    public static bool TryParse(string? value, IFormatProvider? provider, [MaybeNullWhen(false)] out TelegramId result) => TryParse(value.AsSpan(), provider, out result);

    public static TelegramId Parse(ReadOnlySpan<char> value, IFormatProvider? provider)
        => TryParse(value, provider, out var result) ? result : throw new ArgumentException("Could not parse supplied value.", nameof(value));

    private static PrefferedName? NormalizeUserName(PrefferedName? userName) => userName is null || string.IsNullOrWhiteSpace(userName.Value) ? null : userName;

    public override string ToString() => UserName is null || string.IsNullOrWhiteSpace(UserName.Value) ? $"Telegram({Id})" : $"Telegram({Id}, @{UserName.Value})";

}
