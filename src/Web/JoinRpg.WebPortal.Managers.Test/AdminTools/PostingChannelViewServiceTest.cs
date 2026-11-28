using JoinRpg.DomainTypes.Posting;
using JoinRpg.Services.Interfaces.Notification;
using JoinRpg.Web.AdminTools.Advertisement;
using JoinRpg.WebPortal.Managers.AdminTools;

namespace JoinRpg.WebPortal.Managers.Test.AdminTools;

public class PostingChannelViewServiceTest
{
    private static readonly TelegramBotChatsResult BotChats = new(true, "testbot",
        [new TelegramBotChat(new TelegramChatId(-100111), "Канал А"), new TelegramBotChat(new TelegramChatId(-100222), "Канал Б")]);

    [Fact]
    public void FilterAvailable_ExcludesChatOfActiveChannel()
    {
        var chats = PostingChannelViewService.FilterAvailable(BotChats, [CreateChannel(-100111, isActive: true)]);

        chats.ShouldBe([new AvailableChatDto(new TelegramChatId(-100222), "Канал Б")]);
    }

    [Fact]
    public void FilterAvailable_ExcludesChatOfInactiveChannel()
    {
        // Неактивный канал тоже занимает chat id — иначе один чат можно завести дважды
        var chats = PostingChannelViewService.FilterAvailable(BotChats, [CreateChannel(-100111, isActive: false)]);

        chats.ShouldBe([new AvailableChatDto(new TelegramChatId(-100222), "Канал Б")]);
    }

    [Fact]
    public void EnsureCanCreate_DisabledBot_Refuses()
    {
        var refusal = Should.Throw<PostingChannelActionRefusedException>(() =>
            PostingChannelViewService.EnsureCanCreate(new CreateChannelRequest(new TelegramChatId(-100111), "Канал"),
                TelegramBotChatsResult.Disabled, []));

        refusal.Message.ShouldBe("Telegram-бот не настроен");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void EnsureCanCreate_EmptyName_Refuses(string name)
    {
        var refusal = Should.Throw<PostingChannelActionRefusedException>(() =>
            PostingChannelViewService.EnsureCanCreate(new CreateChannelRequest(new TelegramChatId(-100111), name), BotChats, []));

        refusal.Message.ShouldBe("Название канала не указано");
    }

    [Fact]
    public void EnsureCanCreate_ChatAlreadyUsed_Refuses()
    {
        var refusal = Should.Throw<PostingChannelActionRefusedException>(() =>
            PostingChannelViewService.EnsureCanCreate(new CreateChannelRequest(new TelegramChatId(-100111), "Канал"),
                BotChats, [CreateChannel(-100111, isActive: true)]));

        refusal.Message.ShouldBe("Этот чат уже занят каналом рекламы");
    }

    [Fact]
    public void EnsureCanCreate_ChatNotAvailableForBot_Refuses()
    {
        var refusal = Should.Throw<PostingChannelActionRefusedException>(() =>
            PostingChannelViewService.EnsureCanCreate(new CreateChannelRequest(new TelegramChatId(-100999), "Канал"), BotChats, []));

        refusal.Message.ShouldBe("Бот не состоит в этом чате или событие старше 24 часов — добавьте бота в чат и откройте диалог заново");
    }

    [Fact]
    public void EnsureCanCreate_ValidRequest_DoesNotThrow() =>
        Should.NotThrow(() =>
            PostingChannelViewService.EnsureCanCreate(new CreateChannelRequest(new TelegramChatId(-100111), "Канал"), BotChats, []));

    private static PostingChannelInfo CreateChannel(long chatId, bool isActive) =>
        new(
            new PostingChannelIdentification(1),
            "Существующий",
            BoundProjectId: null,
            new TelegramChannelSettings(new TelegramChatId(chatId)),
            isActive);
}
