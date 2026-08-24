using JoinRpg.Common.PrimitiveTypes.Users;
using JoinRpg.Common.Telegram;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel.Users;
using JoinRpg.DomainTypes.Interfaces;
using JoinRpg.DomainTypes.Notifications;
using JoinRpg.DomainTypes.Users;
using JoinRpg.Interfaces.Notifications;
using JoinRpg.Services.Interfaces.Notification;
using JoinRpg.Services.Notifications.Senders;
using Microsoft.Extensions.Options;

namespace JoinRpg.Services.Notifications.Tests;

public class TelegramSenderJobServiceTests
{
    private static TargetedNotificationMessageForRecipient MakeMessage(bool skipSignature, TelegramId chatId) =>
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
            userRepository: new ThrowingUserRepository(),
            linkRenderer: new NullEntityLinkRenderer());

        var chatId = new TelegramId(-100, null);
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
            userRepository: new FakeUserRepository(),
            linkRenderer: new NullEntityLinkRenderer());

        var chatId = new TelegramId(-100, null);
        var result = await service.SendAsync(MakeMessage(skipSignature: false, chatId), CancellationToken.None);

        result.Succeeded.ShouldBeTrue();
        telegramClient.LastContents!.Contents.ShouldContain("<em>Master</em>");
    }

    private sealed class FakeTelegramNotificationService : ITelegramNotificationService
    {
        public TelegramId? LastChatId { get; private set; }
        public TelegramHtmlString? LastContents { get; private set; }

        public Task<SendingResult> SendTelegramNotification(TelegramId telegramId, TelegramHtmlString contents)
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

    private sealed class FakeUserRepository : IUserRepository
    {
        public Task<UserInfo?> GetUserInfo(UserIdentification userId) => Task.FromResult<UserInfo?>(new UserInfo(
            userId,
            new UserSocialNetworks(null, null, null, null, false, ContactsAccessType.OnlyForMasters),
            [],
            [],
            [],
            IsAdmin: true,
            SelectedAvatarId: null,
            new JoinRpg.Common.PrimitiveTypes.Email("robot@joinrpg.ru"),
            EmailConfirmed: true,
            new UserFullName(new PrefferedName("Master"), null, null, null),
            VerifiedProfileFlag: false,
            PhoneNumber: null));

        public Task<User> GetById(int id) => throw new NotSupportedException();
        public Task<User> WithProfile(int userId) => throw new NotSupportedException();
        public Task<User> GetWithSubscribe(int currentUserId) => throw new NotSupportedException();
        public Task<UserAvatar> LoadAvatar(AvatarIdentification userAvatarId) => throw new NotSupportedException();
        public Task<IReadOnlyCollection<UserInfo>> GetUserInfos(IReadOnlyCollection<UserIdentification> userIds) => throw new NotSupportedException();
        public Task<IReadOnlyCollection<UserInfoHeader>> GetUserInfoHeaders(IReadOnlyCollection<UserIdentification> userIds) => throw new NotSupportedException();
        public Task<IReadOnlyCollection<UserInfoHeader>> GetAdminUserInfoHeaders() => throw new NotSupportedException();
        public Task<UserIdentification?> FindByVk(string vkId) => throw new NotSupportedException();
        public Task<UserIdentification?> FindByTelegram(string telegramUsername) => throw new NotSupportedException();
        public Task<UserIdentification?> FindByEmail(string email) => throw new NotSupportedException();
    }

    private sealed class ThrowingUserRepository : IUserRepository
    {
        public Task<UserInfo?> GetUserInfo(UserIdentification userId) => throw new NotSupportedException("Не должно вызываться при SkipSignature=true");

        public Task<User> GetById(int id) => throw new NotSupportedException();
        public Task<User> WithProfile(int userId) => throw new NotSupportedException();
        public Task<User> GetWithSubscribe(int currentUserId) => throw new NotSupportedException();
        public Task<UserAvatar> LoadAvatar(AvatarIdentification userAvatarId) => throw new NotSupportedException();
        public Task<IReadOnlyCollection<UserInfo>> GetUserInfos(IReadOnlyCollection<UserIdentification> userIds) => throw new NotSupportedException();
        public Task<IReadOnlyCollection<UserInfoHeader>> GetUserInfoHeaders(IReadOnlyCollection<UserIdentification> userIds) => throw new NotSupportedException();
        public Task<IReadOnlyCollection<UserInfoHeader>> GetAdminUserInfoHeaders() => throw new NotSupportedException();
        public Task<UserIdentification?> FindByVk(string vkId) => throw new NotSupportedException();
        public Task<UserIdentification?> FindByTelegram(string telegramUsername) => throw new NotSupportedException();
        public Task<UserIdentification?> FindByEmail(string email) => throw new NotSupportedException();
    }
}
