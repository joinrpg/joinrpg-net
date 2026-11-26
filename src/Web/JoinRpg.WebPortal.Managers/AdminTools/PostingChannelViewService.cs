using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DomainTypes.Posting;
using JoinRpg.Services.Interfaces.Notification;
using JoinRpg.Web.AdminTools.Advertisement;

namespace JoinRpg.WebPortal.Managers.AdminTools;

internal class PostingChannelViewService(
    ITelegramBotChatsService telegramBotChatsService,
    IPostingChannelRepository channelRepository,
    IUnitOfWork unitOfWork) : IPostingChannelClient
{
    public async Task<AvailableChatsResponse> GetAvailableChats()
    {
        var botChats = await telegramBotChatsService.GetBotChats(CancellationToken.None);
        var existing = await channelRepository.GetAllChannels();
        return new AvailableChatsResponse(botChats.Enabled, botChats.BotName, FilterAvailable(botChats, existing));
    }

    public async Task CreateChannel(CreateChannelRequest request)
    {
        var botChats = await telegramBotChatsService.GetBotChats(CancellationToken.None);
        var existing = await channelRepository.GetAllChannels();
        EnsureCanCreate(request, botChats, existing);
        unitOfWork.GetPostingChannelWriteRepository().CreateChannel(
            request.Name.Trim(),
            new TelegramChannelSettings(new TelegramChatId(request.ChatId)));
        await unitOfWork.SaveChangesAsync();
    }

    internal static IReadOnlyCollection<AvailableChatDto> FilterAvailable(
        TelegramBotChatsResult botChats, IReadOnlyCollection<PostingChannelInfo> existing)
    {
        var usedChatIds = GetUsedChatIds(existing);
        return [.. botChats.Chats
            .Where(chat => !usedChatIds.Contains(chat.ChatId.Value))
            .Select(chat => new AvailableChatDto(chat.ChatId.Value, chat.Title))];
    }

    internal static void EnsureCanCreate(
        CreateChannelRequest request, TelegramBotChatsResult botChats, IReadOnlyCollection<PostingChannelInfo> existing)
    {
        if (!botChats.Enabled)
        {
            throw new PostingChannelActionRefusedException("Telegram-бот не настроен");
        }
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new PostingChannelActionRefusedException("Название канала не указано");
        }
        if (GetUsedChatIds(existing).Contains(request.ChatId))
        {
            throw new PostingChannelActionRefusedException("Этот чат уже занят каналом рекламы");
        }
        if (botChats.Chats.All(chat => chat.ChatId.Value != request.ChatId))
        {
            throw new PostingChannelActionRefusedException("Бот не состоит в этом чате или событие старше 24 часов — добавьте бота в чат и откройте диалог заново");
        }
    }

    // Неактивные каналы тоже занимают chat id — иначе один чат можно было бы завести дважды
    private static HashSet<long> GetUsedChatIds(IReadOnlyCollection<PostingChannelInfo> existing) =>
        [.. existing
            .Select(channel => channel.Settings)
            .OfType<TelegramChannelSettings>()
            .Select(settings => settings.ChatId.Value)];
}