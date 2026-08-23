namespace JoinRpg.DomainTypes.Advertisement;

public abstract record AdvertisementChannelSettings;

/// <summary>
/// <see cref="TelegramChatId"/> — единый числовой идентификатор адресата в Telegram Bot API: тот
/// же тип, что и для личных чатов, отдельного типа chat id для каналов/групп не нужно.
/// </summary>
public record TelegramChannelSettings(TelegramChatId ChatId) : AdvertisementChannelSettings;
