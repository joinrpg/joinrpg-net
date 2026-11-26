namespace JoinRpg.Services.Interfaces.Notification;

/// <summary>
/// Список чатов и каналов, в которых состоит бот. Источник — входящие апдейты
/// <c>my_chat_member</c>, которые Telegram хранит около 24 часов, поэтому видны только
/// недавно добавленные чаты (ADR010, создание канала рекламы из админки).
/// </summary>
public interface ITelegramBotChatsService
{
    Task<TelegramBotChatsResult> GetBotChats(CancellationToken cancellationToken);
}

public record TelegramBotChatsResult(bool Enabled, string? BotName, IReadOnlyCollection<TelegramBotChat> Chats)
{
    public static TelegramBotChatsResult Disabled { get; } = new(false, null, []);
}

public record TelegramBotChat(TelegramChatId ChatId, string Title);