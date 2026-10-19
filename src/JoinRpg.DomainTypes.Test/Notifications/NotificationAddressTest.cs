using JoinRpg.DomainTypes.Notifications;

namespace JoinRpg.DomainTypes.Test.Notifications;

public class NotificationAddressTest
{
    [Theory]
    [InlineData(351484506)]
    [InlineData(5159651684)]
    [InlineData(-1004315256401)]
    public void TelegramAddressShouldRoundTrip(long id)
    {
        var address = new NotificationAddress(new TelegramChatId(id));
        (var channel, var value) = address;

        var parsed = new NotificationAddress(channel, value);

        parsed.AsTelegram().ShouldBe(new TelegramChatId(id));
    }

    // Исторические форматы, которые могли остаться в очереди уведомлений до #4642 (#5466).
    [Theory]
    [InlineData("TelegramChatId(351484506)")]
    [InlineData("Telegram(351484506)")]
    [InlineData("Telegram(351484506, @)")]
    [InlineData("Telegram(351484506, @leo)")]
    [InlineData("TelegramId(351484506, @leo)")]
    public void TelegramAddressShouldParseHistoricalFormats(string value)
    {
        var parsed = new NotificationAddress(NotificationChannel.Telegram, value);

        parsed.AsTelegram().ShouldBe(new TelegramChatId(351484506));
    }

    [Theory]
    [InlineData("Telegram(@leo)")]
    [InlineData("garbage")]
    public void TelegramAddressWithoutChatIdShouldNotParse(string value)
        => Should.Throw<ArgumentException>(() => new NotificationAddress(NotificationChannel.Telegram, value));
}
