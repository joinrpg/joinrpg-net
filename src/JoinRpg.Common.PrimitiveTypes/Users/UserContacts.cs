namespace JoinRpg.Common.PrimitiveTypes;

/// <summary>
/// Контакты пользователя для отображения. Заполненные поля рисуются типизированными
/// компонентами (<c>EmailLink</c>, <c>VkLink</c>, <c>TelegramLink</c>, <c>LiveJournalLink</c>);
/// пустые опускаются.
/// </summary>
public record UserContacts(Email? Email, VkId? Vk, TelegramId? Telegram, LiveJournalId? LiveJournal);
