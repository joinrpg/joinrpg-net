namespace JoinRpg.Services.Advertisement.Test;

public class AdvSenderFactoryTests
{
    private static AdvSenderFactory CreateFactory() =>
        new(new FakeTelegramNotificationService(), Options.Create(new KogdaIgraOptions { HostName = new Uri("https://kogda-igra.ru") }), new FakeProjectUriLocator());

    [Fact]
    public void Create_TelegramSettingsWithChatId_ReturnsSender()
    {
        var factory = CreateFactory();
        var channel = new AdvertisementChannelInfo(
            new AdvertisementChannelIdentification(1), Name: "Test", BoundProjectId: null, new TelegramChannelSettings(new TelegramChatId(-100)));

        var sender = factory.Create(channel);

        sender.ShouldNotBeNull();
        sender.ShouldBeOfType<TelegramSingleHotRoleSender>();
    }

    [Fact]
    public void Create_TelegramSettingsWithZeroChatId_ReturnsNull()
    {
        var factory = CreateFactory();
        var channel = new AdvertisementChannelInfo(
            new AdvertisementChannelIdentification(1), Name: "Test", BoundProjectId: null, new TelegramChannelSettings(new TelegramChatId(0)));

        factory.Create(channel).ShouldBeNull();
    }

    [Fact]
    public void Create_UnsupportedSettings_ReturnsNull()
    {
        var factory = CreateFactory();
        var channel = new AdvertisementChannelInfo(
            new AdvertisementChannelIdentification(1), Name: "Test", BoundProjectId: null, new UnsupportedChannelSettings());

        factory.Create(channel).ShouldBeNull();
    }

    [Fact]
    public void CreateNewlyOpenedProjectsDigestSender_TelegramSettingsWithChatId_ReturnsSender()
    {
        var factory = CreateFactory();
        var channel = new AdvertisementChannelInfo(
            new AdvertisementChannelIdentification(1), Name: "Test", BoundProjectId: null, new TelegramChannelSettings(new TelegramChatId(-100)));

        var sender = factory.CreateNewlyOpenedProjectsDigestSender(channel);

        sender.ShouldNotBeNull();
        sender.ShouldBeOfType<TelegramNewlyOpenedProjectsDigestSender>();
    }

    [Fact]
    public void CreateNewlyOpenedProjectsDigestSender_TelegramSettingsWithZeroChatId_ReturnsNull()
    {
        var factory = CreateFactory();
        var channel = new AdvertisementChannelInfo(
            new AdvertisementChannelIdentification(1), Name: "Test", BoundProjectId: null, new TelegramChannelSettings(new TelegramChatId(0)));

        factory.CreateNewlyOpenedProjectsDigestSender(channel).ShouldBeNull();
    }

    [Fact]
    public void CreateNewlyOpenedProjectsDigestSender_UnsupportedSettings_ReturnsNull()
    {
        var factory = CreateFactory();
        var channel = new AdvertisementChannelInfo(
            new AdvertisementChannelIdentification(1), Name: "Test", BoundProjectId: null, new UnsupportedChannelSettings());

        factory.CreateNewlyOpenedProjectsDigestSender(channel).ShouldBeNull();
    }

    private sealed record UnsupportedChannelSettings : AdvertisementChannelSettings;

    private sealed class FakeTelegramNotificationService : ITelegramNotificationService
    {
        public Task<SendingResult> SendTelegramNotification(TelegramChatId chatId, TelegramHtmlString contents) =>
            Task.FromResult(SendingResult.Success());

        public Task<string?> GetMyUserName(CancellationToken cancellationToken) => Task.FromResult<string?>(null);
    }

    private sealed class FakeProjectUriLocator : IUriLocator<ProjectIdentification>
    {
        public Uri GetUri(ProjectIdentification target) => new($"https://joinrpg.ru/{target.Value}/home");
    }
}
