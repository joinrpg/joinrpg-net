using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Services.Interfaces.Notification;

namespace JoinRpg.Common.Telegram.Test;

public class DefaultOnlyTelegramNotificationServiceFactoryTest
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void GetService_WithNoBotKey_ReturnsDefaultService(string? botKey)
    {
        var defaultService = new FakeTelegramNotificationService();
        var factory = new DefaultOnlyTelegramNotificationServiceFactory(defaultService);

        factory.GetService(botKey).ShouldBe(defaultService);
    }

    [Fact]
    public void GetService_WithBotKey_Throws()
    {
        var factory = new DefaultOnlyTelegramNotificationServiceFactory(new FakeTelegramNotificationService());

        Should.Throw<NotSupportedException>(() => factory.GetService("advBot"));
    }

    private sealed class FakeTelegramNotificationService : ITelegramNotificationService
    {
        public Task<SendingResult> SendTelegramNotification(TelegramId telegramId, TelegramHtmlString contents) => throw new NotSupportedException();
        public Task<string?> GetMyUserName(CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
