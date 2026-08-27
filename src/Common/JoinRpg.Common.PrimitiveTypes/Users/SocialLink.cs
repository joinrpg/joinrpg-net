using System.Diagnostics.CodeAnalysis;

namespace JoinRpg.Common.PrimitiveTypes;

/// <summary>
/// Идентичность пользователя в социальной сети для отображения в профиле: числовой id,
/// красивое имя, ссылка на профиль и признак верификации. Не путать с <see cref="TelegramChatId"/> —
/// тот нужен только для адресации сообщений в Telegram Bot API, без имени/ссылки/verified.
/// </summary>
public abstract record SocialLink(PrefferedName? PrettyName, bool IsVerified)
{
    public abstract long? Id { get; }
    public abstract Uri? Link { get; }
}

public sealed record TelegramSocialLink : SocialLink, ISpanParsable<TelegramSocialLink>
{
    // Нет, если не привязан ExternalLogin (есть только legacy PrettyName из UserExtra.Telegram) —
    // в этом случае это неверифицированный отображаемый контакт, слать через него сообщения нельзя.
    public TelegramChatId? ChatId { get; }

    public override long? Id => ChatId?.Value;

    public override Uri? Link => PrettyName is null ? null : new Uri($"https://t.me/{PrettyName.Value}");

    public TelegramSocialLink(TelegramChatId? chatId, PrefferedName? prettyName = null, bool isVerified = false)
        : base(NormalizeUserName(prettyName), isVerified)
    {
        var normalizedPrettyName = NormalizeUserName(prettyName);
        if (chatId is null && normalizedPrettyName is null)
        {
            throw new ArgumentException("Нужно указать либо chatId, либо prettyName.", nameof(chatId));
        }

        ChatId = chatId;
    }

    public static TelegramSocialLink? FromOptional(long? id, PrefferedName? userName, bool isVerified = false)
        => id is null ? null : new TelegramSocialLink(new TelegramChatId(id.Value), userName, isVerified);

    public static TelegramSocialLink? FromOptional(string? key, PrefferedName? userName, bool isVerified = false)
        => string.IsNullOrWhiteSpace(key) ? null : new TelegramSocialLink(new TelegramChatId(long.Parse(key)), userName, isVerified);

    /// <summary>
    /// Строит из привязанного ExternalLogin (даёт ChatId и верификацию) и/или legacy-поля
    /// UserExtra.Telegram (даёт PrettyName). Если ExternalLogin отсутствует, а PrettyName есть,
    /// ссылка всё равно строится (по PrettyName), но верифицированной не считается — отправлять
    /// сообщения по ней нельзя, только показывать в профиле.
    /// </summary>
    public static TelegramSocialLink? FromUserData(string? externalLoginKey, PrefferedName? prettyName)
    {
        var hasExternalLogin = long.TryParse(externalLoginKey, out var chatId);
        return hasExternalLogin || prettyName is not null
            ? new TelegramSocialLink(hasExternalLogin ? new TelegramChatId(chatId) : null, prettyName, isVerified: hasExternalLogin)
            : null;
    }

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

    public override string ToString() => (ChatId, PrettyName) switch
    {
        (null, _) => $"Telegram(@{PrettyName?.Value})",
        (_, null) => $"Telegram({Id})",
        _ => $"Telegram({Id}, @{PrettyName.Value})",
    };
}

public sealed record VkSocialLink : SocialLink
{
    private readonly long? id;

    public override long? Id => id;

    // Как и у Telegram: если числового id нет, ссылка всё равно строится по PrettyName (slug профиля).
    public override Uri? Link => id is long numericId
        ? new Uri($"https://vk.com/id{numericId}")
        : PrettyName is null ? null : new Uri($"https://vk.com/{PrettyName.Value}");

    public VkSocialLink(long? id, PrefferedName? prettyName = null, bool isVerified = false)
        : base(prettyName, isVerified)
    {
        if (id is null && prettyName is null)
        {
            throw new ArgumentException("Нужно указать либо id, либо prettyName.", nameof(id));
        }

        this.id = id;
    }

    /// <summary>
    /// Строит из привязанного ExternalLogin (приоритетно) или, если его нет, из legacy-поля
    /// UserExtra.Vk. Легаси-поле обычно хранит числовой id (формат "id123456"), но если это не
    /// число (старый slug профиля), используем его как PrettyName — ссылка всё равно будет рабочей.
    /// Верификация (<paramref name="vkVerified"/>) учитывается, только если есть привязанный
    /// ExternalLogin — легаси-запись без него верифицированной не считается.
    /// </summary>
    public static VkSocialLink? FromUserData(string? externalLoginKey, string? legacyVk, bool vkVerified)
    {
        var hasExternalLogin = long.TryParse(externalLoginKey, out var externalId);
        var id = hasExternalLogin ? externalId : ParseLegacyId(legacyVk);
        var prettyName = id is null ? PrefferedName.FromOptional(legacyVk) : null;

        return id is null && prettyName is null
            ? null
            : new VkSocialLink(id, prettyName, isVerified: hasExternalLogin && vkVerified);
    }

    private static long? ParseLegacyId(string? vk)
    {
        if (string.IsNullOrWhiteSpace(vk))
        {
            return null;
        }

        var trimmed = vk.StartsWith("id", StringComparison.OrdinalIgnoreCase) ? vk[2..] : vk;
        return long.TryParse(trimmed, out var parsedId) ? parsedId : null;
    }

    public override string ToString() => Id is not null ? $"Vk({Id})" : $"Vk({PrettyName})";
}
