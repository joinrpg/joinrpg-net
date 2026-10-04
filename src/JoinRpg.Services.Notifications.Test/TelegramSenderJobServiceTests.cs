using JoinRpg.Common.Telegram;
using JoinRpg.DataModel.Mocks.Fakes;
using JoinRpg.DomainTypes.Interfaces;
using JoinRpg.DomainTypes.Notifications;
using JoinRpg.DomainTypes.Users;
using JoinRpg.Interfaces.Notifications;
using JoinRpg.Services.Interfaces.Notification;
using JoinRpg.Services.Notifications.Senders;
using Microsoft.Extensions.Options;

namespace JoinRpg.Services.Notifications.Test;

public class TelegramSenderJobServiceTests
{
    /// <summary>Инициатор уведомления — его отображаемое имя уезжает в подпись.</summary>
    private static readonly UserInfo Initiator = new(
        new UserIdentification(1),
        new UserSocialNetworks(null, null, null, null, ContactsAccessType.OnlyForMasters),
        [],
        [],
        [],
        IsAdmin: true,
        SelectedAvatarId: null,
        new JoinRpg.Common.PrimitiveTypes.Email("robot@joinrpg.ru"),
        EmailConfirmed: true,
        new UserFullName(new PrefferedName("Master"), null, null, null),
        VerifiedProfileFlag: false,
        PhoneNumber: null,
        HasPassword: false);

    private static TargetedNotificationMessageForRecipient MakeMessage(bool skipSignature, TelegramChatId chatId) =>
        new(
            new NotificationMessageForRecipient(
                new MarkdownString("Тело"),
                new UserIdentification(1),
                "Заголовок",
                new UserIdentification(1),
                EntityReference: null,
                DateTimeOffset.UtcNow,
                skipSignature),
            new NotificationAddress(chatId),
            Attempts: 0,
            new NotificationId(1));

    [Fact]
    public async Task SendAsync_SkipSignature_DoesNotResolveInitiatorDisplayName()
    {
        var telegramClient = new FakeTelegramNotificationService();
        var service = new TelegramSenderJobService(
            Options.Create(new TelegramLoginOptions { BotName = "bot", BotId = 1, BotSecret = "secret" }),
            telegramClient,
            userRepository: FakeUserRepository.MustNotBeCalled("при SkipSignature=true подпись не строится"),
            linkRenderer: new NullEntityLinkRenderer());

        var chatId = new TelegramChatId(-100);
        var result = await service.SendAsync(MakeMessage(skipSignature: true, chatId), CancellationToken.None);

        result.Succeeded.ShouldBeTrue();
        telegramClient.LastChatId.ShouldBe(chatId);
        telegramClient.LastContents!.Contents.ShouldNotContain("<em>");
    }

    [Fact]
    public async Task SendAsync_WithSignature_ResolvesInitiatorAndAppendsSignature()
    {
        var telegramClient = new FakeTelegramNotificationService();
        var service = new TelegramSenderJobService(
            Options.Create(new TelegramLoginOptions { BotName = "bot", BotId = 1, BotSecret = "secret" }),
            telegramClient,
            userRepository: FakeUserRepository.WithUsers(Initiator),
            linkRenderer: new NullEntityLinkRenderer());

        var chatId = new TelegramChatId(-100);
        var result = await service.SendAsync(MakeMessage(skipSignature: false, chatId), CancellationToken.None);

        result.Succeeded.ShouldBeTrue();
        telegramClient.LastContents!.Contents.ShouldContain("<em>Master</em>");
    }

    private sealed class FakeTelegramNotificationService : ITelegramNotificationService
    {
        public TelegramChatId? LastChatId { get; private set; }
        public TelegramHtmlString? LastContents { get; private set; }

        public Task<SendingResult> SendTelegramNotification(TelegramChatId telegramId, TelegramHtmlString contents)
        {
            LastChatId = telegramId;
            LastContents = contents;
            return Task.FromResult(SendingResult.Success());
        }

        public Task<string?> GetMyUserName(CancellationToken cancellationToken) => Task.FromResult<string?>(null);
    }

    private sealed class NullEntityLinkRenderer : INotificationEntityLinkRenderer
    {
        public RenderedEntityLink? RenderEntityLink(IProjectEntityId? entityReference) => null;
    }
}
