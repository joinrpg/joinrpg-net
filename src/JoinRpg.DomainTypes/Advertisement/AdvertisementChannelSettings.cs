namespace JoinRpg.DomainTypes.Advertisement;

public abstract record AdvertisementChannelSettings;

/// <summary>
/// Переиспользуем <see cref="TelegramId"/> вместо отдельного типа chat id — Bot API принимает
/// те же числовые идентификаторы, что и для личных чатов.
/// </summary>
public record TelegramChannelSettings(TelegramId ChatId) : AdvertisementChannelSettings;
