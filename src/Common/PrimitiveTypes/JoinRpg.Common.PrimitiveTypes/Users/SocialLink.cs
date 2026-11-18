using System.Diagnostics.CodeAnalysis;

namespace JoinRpg.Common.PrimitiveTypes;

/// <summary>
/// Идентичность пользователя в социальной сети для отображения в профиле: числовой id,
/// красивое имя, ссылка на профиль и признак верификации. Не путать с <see cref="TelegramChatId"/> —
/// тот нужен только для адресации сообщений в Telegram Bot API, без имени/ссылки/verified.
/// </summary>
public abstract record SocialLink(PrefferedName? PrettyName, bool IsVerified)
{
    /// <summary>Числовой id профиля в соцсети, если известен.</summary>
    public abstract long? Id { get; }

    /// <summary>Ссылка на профиль, если её можно построить.</summary>
    public abstract Uri? Link { get; }

    /// <summary>
    /// Можно ли использовать эту привязку как способ входа в аккаунт. Не то же самое, что
    /// <see cref="IsVerified"/> — Telegram-виджет умеет только привязывать контакт к уже
    /// залогиненному аккаунту, отдельного входа через него нет.
    /// </summary>
    public abstract bool CanLogin { get; }
}

/// <summary>Привязка Telegram: id чата (если есть ExternalLogin) и/или видимое имя пользователя.</summary>
public sealed record TelegramSocialLink : SocialLink, ISpanParsable<TelegramSocialLink>
{
    /// <summary>
    /// Id чата для Telegram Bot API. Нет, если не привязан ExternalLogin
    /// (есть только legacy PrettyName из UserExtra.Telegram) — тогда это неверифицированный
    /// отображаемый контакт, слать через него сообщения нельзя.
    /// </summary>
    public TelegramChatId? ChatId { get; }

    /// <inheritdoc />
    public override long? Id => ChatId?.Value;

    /// <inheritdoc />
    public override Uri? Link => PrettyName is null ? null : new Uri($"https://t.me/{PrettyName.Value}");

    /// <inheritdoc />
    public override bool CanLogin => false;

    /// <summary>
    /// Нужно указать хотя бы один из параметров: <paramref name="chatId"/> или <paramref name="prettyName"/>.
    /// Ссылка с <paramref name="chatId"/> по умолчанию считается верифицированной: chat id получают
    /// только из привязанного ExternalLogin. Для неверифицированных данных (чужой ввод, legacy-поля)
    /// передавайте <paramref name="isVerified"/>: false.
    /// </summary>
    /// <exception cref="ArgumentException">Оба параметра пустые.</exception>
    public TelegramSocialLink(TelegramChatId? chatId, PrefferedName? prettyName = null, bool isVerified = true)
        : base(NormalizeUserName(prettyName), isVerified)
    {
        var normalizedPrettyName = NormalizeUserName(prettyName);
        if (chatId is null && normalizedPrettyName is null)
        {
            throw new ArgumentException("Нужно указать либо chatId, либо prettyName.", nameof(chatId));
        }

        ChatId = chatId;
    }

    /// <summary>
    /// Строит ссылку из id, если он задан; иначе возвращает <c>null</c>. Ссылка по умолчанию
    /// считается верифицированной (chat id получают только из привязанного ExternalLogin);
    /// для неверифицированных данных передавайте <paramref name="isVerified"/>: false.
    /// </summary>
    public static TelegramSocialLink? FromOptional(long? id, PrefferedName? userName, bool isVerified = true)
        => id is null ? null : new TelegramSocialLink(new TelegramChatId(id.Value), userName, isVerified);

    /// <inheritdoc cref="FromOptional(long?, PrefferedName?, bool)" />
    public static TelegramSocialLink? FromOptional(string? key, PrefferedName? userName, bool isVerified = true)
        => string.IsNullOrWhiteSpace(key) ? null : new TelegramSocialLink(new TelegramChatId(long.Parse(key)), userName, isVerified);

    /// <inheritdoc cref="FromOptional(long?, PrefferedName?, bool)" />
    public static TelegramSocialLink? FromOptional(long? id, string? userName, bool isVerified = true)
        => FromOptional(id, PrefferedName.FromOptional(userName), isVerified);

    /// <inheritdoc cref="FromOptional(long?, PrefferedName?, bool)" />
    public static TelegramSocialLink? FromOptional(string? key, string? userName, bool isVerified = true)
        => FromOptional(key, PrefferedName.FromOptional(userName), isVerified);

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

    /// <summary>
    /// Парсит строку вида «Telegram(123, @user)» или «123, @user». Делит только по ',': chat id
    /// каналов/супергрупп отрицательный, '-' — часть числа, а не разделитель. Парсинг строки не
    /// верифицирует владельца: результат всегда неверифицированный.
    /// </summary>
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
                // Парсинг строки не верифицирует владельца — ссылка неверифицированная.
                result = new TelegramSocialLink(new TelegramChatId(i), isVerified: false);
                return true;
            }

            result = null!;
            return false;
        }

        if (long.TryParse(val[..commaIndex].Trim(), provider, out var i1))
        {
            var usernameSpan = val[(commaIndex + 1)..].Trim().TrimStart('@');
            // Парсинг строки не верифицирует владельца — ссылка неверифицированная.
            result = new TelegramSocialLink(new TelegramChatId(i1), string.IsNullOrWhiteSpace(usernameSpan.ToString()) ? null : new PrefferedName(usernameSpan.ToString()), isVerified: false);
            return true;
        }

        result = null!;
        return false;
    }

    /// <inheritdoc cref="TryParse(ReadOnlySpan{char}, IFormatProvider, out TelegramSocialLink)" />
    /// <exception cref="ArgumentException">Строку не удалось распарсить.</exception>
    public static TelegramSocialLink Parse(string value, IFormatProvider? provider = null) => Parse(value.AsSpan(), provider);

    /// <inheritdoc cref="TryParse(ReadOnlySpan{char}, IFormatProvider, out TelegramSocialLink)" />
    public static bool TryParse(string? value, IFormatProvider? provider, [MaybeNullWhen(false)] out TelegramSocialLink result) => TryParse(value.AsSpan(), provider, out result);

    /// <inheritdoc cref="TryParse(ReadOnlySpan{char}, IFormatProvider, out TelegramSocialLink)" />
    /// <exception cref="ArgumentException">Строку не удалось распарсить.</exception>
    public static TelegramSocialLink Parse(ReadOnlySpan<char> value, IFormatProvider? provider)
        => TryParse(value, provider, out var result) ? result : throw new ArgumentException("Could not parse supplied value.", nameof(value));

    private static PrefferedName? NormalizeUserName(PrefferedName? userName) => userName is null || string.IsNullOrWhiteSpace(userName.Value) ? null : userName;

    /// <inheritdoc />
    public override string ToString() => (ChatId, PrettyName) switch
    {
        (null, _) => $"Telegram(@{PrettyName?.Value})",
        (_, null) => $"Telegram({Id})",
        _ => $"Telegram({Id}, @{PrettyName.Value})",
    };
}

/// <summary>Привязка ВКонтакте: числовой id или slug профиля; при наличии верифицированного ExternalLogin можно входить через неё.</summary>
public sealed record VkSocialLink : SocialLink
{
    private readonly long? id;

    /// <inheritdoc />
    public override long? Id => id;

    /// <inheritdoc />
    // Как и у Telegram: если числового id нет, ссылка всё равно строится по PrettyName (slug профиля).
    public override Uri? Link => id is long numericId
        ? new Uri($"https://vk.com/id{numericId}")
        : PrettyName is null ? null : new Uri($"https://vk.com/{PrettyName.Value}");

    /// <inheritdoc />
    public override bool CanLogin => IsVerified;

    /// <summary>Нужно указать хотя бы один из параметров: <paramref name="id"/> или <paramref name="prettyName"/>.</summary>
    /// <exception cref="ArgumentException">Оба параметра пустые.</exception>
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

    /// <inheritdoc />
    public override string ToString() => Id is not null ? $"Vk({Id})" : $"Vk({PrettyName})";
}
