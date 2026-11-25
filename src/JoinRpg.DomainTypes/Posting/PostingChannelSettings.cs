namespace JoinRpg.DomainTypes.Posting;

/// <summary>
/// Настройки канала публикации. Закрытая иерархия: наследники только в этой сборке,
/// поэтому switch по ней компилятор проверяет на исчерпываемость без default-ветки.
/// </summary>
public closed record PostingChannelSettings;

/// <summary>
/// <see cref="TelegramChatId"/> — единый числовой идентификатор адресата в Telegram Bot API: тот
/// же тип, что и для личных чатов, отдельного типа chat id для каналов/групп не нужно.
/// </summary>
public sealed record TelegramChannelSettings(TelegramChatId ChatId) : PostingChannelSettings;
