namespace JoinRpg.Services.Advertisement.Test;

public class AdvSenderFactoryTests
{
    [Fact]
    public void Create_TelegramSettingsWithChatId_ReturnsSender()
    {
        var factory = new AdvSenderFactory(new FakeTelegramNotificationService(), Options.Create(new KogdaIgraOptions { HostName = new Uri("https://kogda-igra.ru") }));
        var channel = new AdvertisementChannelInfo(
            new AdvertisementChannelIdentification(1), BoundProjectId: null, new TelegramChannelSettings(new TelegramChatId(-100)));

        var sender = factory.Create(channel);

        sender.ShouldNotBeNull();
        sender.ShouldBeOfType<TelegramSingleHotRoleSender>();
    }

    [Fact]
    public void Create_TelegramSettingsWithZeroChatId_ReturnsNull()
    {
        var factory = new AdvSenderFactory(new FakeTelegramNotificationService(), Options.Create(new KogdaIgraOptions { HostName = new Uri("https://kogda-igra.ru") }));
        var channel = new AdvertisementChannelInfo(
            new AdvertisementChannelIdentification(1), BoundProjectId: null, new TelegramChannelSettings(new TelegramChatId(0)));

        factory.Create(channel).ShouldBeNull();
    }

    [Fact]
    public void Create_UnsupportedSettings_ReturnsNull()
    {
        var factory = new AdvSenderFactory(new FakeTelegramNotificationService(), Options.Create(new KogdaIgraOptions { HostName = new Uri("https://kogda-igra.ru") }));
        var channel = new AdvertisementChannelInfo(
            new AdvertisementChannelIdentification(1), BoundProjectId: null, new UnsupportedChannelSettings());

        factory.Create(channel).ShouldBeNull();
    }

    private sealed record UnsupportedChannelSettings : AdvertisementChannelSettings;

    private sealed class FakeTelegramNotificationService : ITelegramNotificationService
    {
        public Task<SendingResult> SendTelegramNotification(TelegramChatId chatId, TelegramHtmlString contents) =>
            Task.FromResult(SendingResult.Success());

        public Task<string?> GetMyUserName(CancellationToken cancellationToken) => Task.FromResult<string?>(null);
    }
}
