using System.Net;
using System.Text;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Services.Interfaces.Notification;
using Microsoft.Extensions.Options;
using Telegram.Bot;

namespace JoinRpg.Common.Telegram.Test;

public class TelegramBotChatsServiceTest
{
    private static readonly TelegramLoginOptions Options = new() { BotName = "testbot", BotId = 123456, BotSecret = "secret" };

    [Fact]
    public async Task JoinedChannel_Returned()
    {
        var service = CreateService(Updates(
            MyChatMember(1, """{"id":-100111,"type":"channel","title":"Канал А"}""", "administrator")));

        var result = await service.GetBotChats(CancellationToken.None);

        result.Enabled.ShouldBeTrue();
        result.BotName.ShouldBe("testbot");
        result.Chats.ShouldBe([new TelegramBotChat(new TelegramChatId(-100111), "Канал А")]);
    }

    [Fact]
    public async Task KickedAfterJoin_Excluded()
    {
        var service = CreateService(Updates(
            MyChatMember(1, """{"id":-100111,"type":"channel","title":"Канал А"}""", "administrator"),
            MyChatMember(2, """{"id":-100111,"type":"channel","title":"Канал А"}""", "kicked")));

        var result = await service.GetBotChats(CancellationToken.None);

        result.Chats.ShouldBeEmpty();
    }

    [Fact]
    public async Task LeftThenRejoined_Returned()
    {
        var service = CreateService(Updates(
            MyChatMember(1, """{"id":-100222,"type":"supergroup","title":"Группа Б"}""", "left"),
            MyChatMember(2, """{"id":-100222,"type":"supergroup","title":"Группа Б"}""", "member")));

        var result = await service.GetBotChats(CancellationToken.None);

        result.Chats.ShouldBe([new TelegramBotChat(new TelegramChatId(-100222), "Группа Б")]);
    }

    [Fact]
    public async Task PrivateChatUpdate_Ignored()
    {
        // Пользователь заблокировал бота — приходит my_chat_member из личного чата
        var service = CreateService(Updates(
            MyChatMember(1, """{"id":42,"type":"private","first_name":"User"}""", "kicked")));

        var result = await service.GetBotChats(CancellationToken.None);

        result.Chats.ShouldBeEmpty();
    }

    [Fact]
    public async Task GroupAndSupergroup_Included()
    {
        var service = CreateService(Updates(
            MyChatMember(1, """{"id":-333,"type":"group","title":"Группа"}""", "member"),
            MyChatMember(2, """{"id":-100444,"type":"supergroup","title":"Супергруппа"}""", "administrator")));

        var result = await service.GetBotChats(CancellationToken.None);

        result.Chats.ShouldBe([
            new TelegramBotChat(new TelegramChatId(-333), "Группа"),
            new TelegramBotChat(new TelegramChatId(-100444), "Супергруппа"),
        ]);
    }

    [Fact]
    public async Task MissingTitle_FallsBack()
    {
        var service = CreateService(Updates(
            MyChatMember(1, """{"id":-100555,"type":"channel","username":"mychannel"}""", "administrator"),
            MyChatMember(2, """{"id":-100666,"type":"channel"}""", "administrator")));

        var result = await service.GetBotChats(CancellationToken.None);

        result.Chats.ShouldBe([
            new TelegramBotChat(new TelegramChatId(-100555), "mychannel"),
            new TelegramBotChat(new TelegramChatId(-100666), "-100666"),
        ]);
    }

    [Fact]
    public async Task RequestContainsMyChatMemberFilter()
    {
        string? capturedBody = null;
        var service = CreateService(Updates(), captureBody: body => capturedBody = body);

        _ = await service.GetBotChats(CancellationToken.None);

        // Без фильтра очередь забита посторонними сообщениями бота и лимит Telegram в 100
        // апдейтов на вызов может отрезать событие добавления бота в канал
        capturedBody.ShouldNotBeNull();
        capturedBody.ShouldContain("allowed_updates");
        capturedBody.ShouldContain("my_chat_member");
    }

    [Fact]
    public async Task Stub_ReturnsDisabled()
    {
        var stub = new StubTelegramBotChatsService();

        var result = await stub.GetBotChats(CancellationToken.None);

        result.Enabled.ShouldBeFalse();
        result.BotName.ShouldBeNull();
        result.Chats.ShouldBeEmpty();
    }

    private static string Updates(params string[] updates) =>
        "{\"ok\":true,\"result\":[" + string.Join(",", updates) + "]}";

    private static string MyChatMember(int updateId, string chatJson, string newStatus) =>
        "{\"update_id\":" + updateId
        + ",\"my_chat_member\":{\"chat\":" + chatJson
        + ",\"from\":{\"id\":5,\"is_bot\":false,\"first_name\":\"Admin\"},\"date\":1700000000,\"old_chat_member\":{\"status\":\"left\",\"user\":{\"id\":1,\"is_bot\":true,\"first_name\":\"Bot\"}},\"new_chat_member\":{\"status\":\"" + newStatus
        + "\",\"user\":{\"id\":1,\"is_bot\":true,\"first_name\":\"Bot\"}}}}";

    private static TelegramBotChatsService CreateService(string responseJson, Action<string?>? captureBody = null)
    {
        var handler = new RespondingHttpMessageHandler(responseJson, captureBody);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.telegram.org") };
        var botClient = new TelegramBotClient("123456:ABC-DEF1234ghIkl-zyx57W2v1u123ew11", httpClient);
        return new TelegramBotChatsService(botClient, Microsoft.Extensions.Options.Options.Create(Options));
    }

    private sealed class RespondingHttpMessageHandler(string json, Action<string?>? captureBody) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (captureBody is not null)
            {
                captureBody(request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken));
            }
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            };
        }
    }
}