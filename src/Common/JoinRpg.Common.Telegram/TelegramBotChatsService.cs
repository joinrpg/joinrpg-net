using JoinRpg.Services.Interfaces.Notification;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace JoinRpg.Common.Telegram;

internal class TelegramBotChatsService(TelegramBotClient client, IOptions<TelegramLoginOptions> options) : ITelegramBotChatsService
{
    public async Task<TelegramBotChatsResult> GetBotChats(CancellationToken cancellationToken)
    {
        // Апдейты сознательно не подтверждаются (offset не передаём): очередь этого бота могут
        // читать и внешние потребители вне репозитория. Появление webhook или другого читателя
        // апдейтов сломает этот код. Фильтр allowed_updates серверный — лимит Telegram в 100
        // апдейтов на вызов после фильтра касается только событий my_chat_member.
        var updates = await client.GetUpdates(allowedUpdates: [UpdateType.MyChatMember], cancellationToken: cancellationToken);

        // Апдейты идут от старых к новым — последний статус на чат побеждает
        var lastStatusByChat = new Dictionary<long, (Chat Chat, ChatMemberStatus Status)>();
        foreach (var update in updates)
        {
            // my_chat_member из личного чата (пользователь заблокировал бота) — не наш случай
            if (update.MyChatMember is not { } memberUpdate
                || memberUpdate.Chat.Type is not (ChatType.Channel or ChatType.Group or ChatType.Supergroup))
            {
                continue;
            }
            lastStatusByChat[memberUpdate.Chat.Id] = (memberUpdate.Chat, memberUpdate.NewChatMember.Status);
        }

        var chats = lastStatusByChat.Values
            .Where(x => x.Status is ChatMemberStatus.Member or ChatMemberStatus.Administrator)
            .Select(x => new TelegramBotChat(new TelegramChatId(x.Chat.Id), GetTitle(x.Chat)))
            .ToList();
        return new TelegramBotChatsResult(true, options.Value.BotName, chats);
    }

    private static string GetTitle(Chat chat) => chat.Title ?? chat.Username ?? chat.Id.ToString();
}

internal class StubTelegramBotChatsService : ITelegramBotChatsService
{
    public Task<TelegramBotChatsResult> GetBotChats(CancellationToken cancellationToken) => Task.FromResult(TelegramBotChatsResult.Disabled);
}