namespace JoinRpg.Common.Telegram.Test;

public class TelegramHttpClientFactoryTest
{
    [Fact]
    public void Create_WithNullOptions_ReturnsNull()
    {
        var result = TelegramHttpClientFactory.Create(null);

        result.ShouldBeNull();
    }

    [Fact]
    public void Create_WithAddressOnly_ReturnsHttpClient()
    {
        var options = new TelegramProxyOptions { Address = "http://proxy.example.com:8080" };

        var result = TelegramHttpClientFactory.Create(options);

        result.ShouldNotBeNull();
    }

    [Fact]
    public void Create_WithSocks5AddressAndCredentials_ReturnsHttpClient()
    {
        var options = new TelegramProxyOptions
        {
            Address = "socks5://proxy.example.com:1080",
            Username = "user",
            Password = "pass",
        };

        var result = TelegramHttpClientFactory.Create(options);

        result.ShouldNotBeNull();
    }

    [Fact]
    public void Create_WithInvalidAddress_ThrowsUriFormatException()
    {
        var options = new TelegramProxyOptions { Address = "not a uri" };

        _ = Should.Throw<UriFormatException>(() => TelegramHttpClientFactory.Create(options));
    }
}
