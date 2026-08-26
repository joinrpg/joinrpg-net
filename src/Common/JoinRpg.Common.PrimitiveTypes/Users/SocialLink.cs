using System.Diagnostics.CodeAnalysis;

namespace JoinRpg.Common.PrimitiveTypes;

/// <summary>
/// Идентичность пользователя в социальной сети для отображения в профиле: числовой id,
/// красивое имя, ссылка на профиль и признак верификации. Не путать с <see cref="TelegramChatId"/> —
/// тот нужен только для адресации сообщений в Telegram Bot API, без имени/ссылки/verified.
/// </summary>
public abstract record SocialLink(PrefferedName? PrettyName, bool IsVerified)
{
    public abstract long Id { get; }
    public abstract Uri? Link { get; }
}

public sealed record TelegramSocialLink : SocialLink, ISpanParsable<TelegramSocialLink>
{
    public TelegramChatId ChatId { get; }

    public override long Id => ChatId.Value;

    public override Uri? Link => PrettyName is null ? null : new Uri($"https://t.me/{PrettyName.Value}");

    public TelegramSocialLink(TelegramChatId chatId, PrefferedName? prettyName = null, bool isVerified = false)
        : base(NormalizeUserName(prettyName), isVerified)
    {
        ChatId = chatId;
    }

    public static TelegramSocialLink? FromOptional(long? id, PrefferedName? userName, bool isVerified = false)
        => id is null ? null : new TelegramSocialLink(new TelegramChatId(id.Value), userName, isVerified);

    public static TelegramSocialLink? FromOptional(string? key, PrefferedName? userName, bool isVerified = false)
        => string.IsNullOrWhiteSpace(key) ? null : new TelegramSocialLink(new TelegramChatId(long.Parse(key)), userName, isVerified);

    public static bool TryParse([NotNullWhen(true)] ReadOnlySpan<char> value, IFormatProvider? provider, [MaybeNullWhen(false)] out TelegramSocialLink result)
    {
        // Не используем общий IdentificationParseHelper.SplitIdentifier — он делит и по ',', и по
        // '-', а chat id каналов/супергрупп в Telegram отрицательный, так что '-' здесь не
        // разделитель, а часть числа. Делим только по ',' (перед именем пользователя).
        ReadOnlySpan<char> val = IdentificationParseHelper.RemovePrefixes(value, [nameof(TelegramSocialLink), "Telegram", "TelegramId"]);

        var commaIndex = val.IndexOf(',');
        if (commaIndex < 0)
        {
            if (long.TryParse(val.Trim(), provider, out var i))
            {
                result = new TelegramSocialLink(new TelegramChatId(i));
                return true;
            }

            result = null!;
            return false;
        }

        if (long.TryParse(val[..commaIndex].Trim(), provider, out var i1))
        {
            var usernameSpan = val[(commaIndex + 1)..].Trim().TrimStart('@');
            result = new TelegramSocialLink(new TelegramChatId(i1), string.IsNullOrWhiteSpace(usernameSpan.ToString()) ? null : new PrefferedName(usernameSpan.ToString()));
            return true;
        }

        result = null!;
        return false;
    }

    public static TelegramSocialLink Parse(string value, IFormatProvider? provider = null) => Parse(value.AsSpan(), provider);

    public static bool TryParse(string? value, IFormatProvider? provider, [MaybeNullWhen(false)] out TelegramSocialLink result) => TryParse(value.AsSpan(), provider, out result);

    public static TelegramSocialLink Parse(ReadOnlySpan<char> value, IFormatProvider? provider)
        => TryParse(value, provider, out var result) ? result : throw new ArgumentException("Could not parse supplied value.", nameof(value));

    private static PrefferedName? NormalizeUserName(PrefferedName? userName) => userName is null || string.IsNullOrWhiteSpace(userName.Value) ? null : userName;

    public override string ToString() => PrettyName is null ? $"Telegram({Id})" : $"Telegram({Id}, @{PrettyName.Value})";
}

public sealed record VkSocialLink : SocialLink
{
    private readonly long id;

    public override long Id => id;

    public override Uri Link => new($"https://vk.com/id{Id}");

    public VkSocialLink(long id, PrefferedName? prettyName = null, bool isVerified = false)
        : base(prettyName, isVerified)
    {
        this.id = id;
    }

    /// <summary>Строит из значения колонки UserExtra.Vk (формат "id123456").</summary>
    public static VkSocialLink? FromOptional(string? vk, bool isVerified = false)
    {
        if (string.IsNullOrWhiteSpace(vk))
        {
            return null;
        }

        var trimmed = vk.StartsWith("id", StringComparison.OrdinalIgnoreCase) ? vk[2..] : vk;
        return long.TryParse(trimmed, out var parsedId) ? new VkSocialLink(parsedId, isVerified: isVerified) : null;
    }

    public override string ToString() => $"Vk({Id})";
}
