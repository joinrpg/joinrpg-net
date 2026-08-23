namespace JoinRpg.Common.PrimitiveTypes.Test;

public class TelegramSocialLinkParseTest
{
    [Theory]
    [InlineData("Telegram(11)")]
    [InlineData("TelegramSocialLink(11)")]
    public void TelegramSocialLinkShouldParseWithoutName(string val)
    {
        TelegramSocialLink.TryParse(val, provider: null, out var parseResult).ShouldBeTrue();
        parseResult.ShouldBe(new TelegramSocialLink(new TelegramChatId(11)));
    }

    [Theory]
    [InlineData("Telegram(351484506, @)")]
    public void TelegramSocialLinkShouldParseSomewhatBroken(string val)
    {
        TelegramSocialLink.TryParse(val, provider: null, out var parseResult).ShouldBeTrue();
        parseResult.ShouldBe(new TelegramSocialLink(new TelegramChatId(351484506)));
    }

    [Theory]
    [InlineData("Telegram(5159651684, @)")]
    [InlineData("Telegram(5159651684)")]
    public void TelegramSocialLinkShouldParseLong(string val)
    {
        TelegramSocialLink.TryParse(val, provider: null, out var parseResult).ShouldBeTrue();
        parseResult.ShouldBe(new TelegramSocialLink(new TelegramChatId(5159651684)));
    }

    [Fact]
    public void TelegramSocialLinkWithoutNameShouldRoundTrip()
    {
        var version = new TelegramSocialLink(new TelegramChatId(11));
        var val = version.ToString();
        TelegramSocialLink.TryParse(val, provider: null, out var parseResult).ShouldBeTrue();
        parseResult.ShouldBe(version);
    }

    [Fact]
    public void TelegramSocialLinkWithEmptyNameShouldBeNormalized()
    {
        var version = new TelegramSocialLink(new TelegramChatId(11), new PrefferedName(""));
        var val = version.ToString();
        TelegramSocialLink.TryParse(val, provider: null, out var parseResult).ShouldBeTrue();
        parseResult.ShouldBe(new TelegramSocialLink(new TelegramChatId(11)));
    }

    [Fact]
    public void TelegramSocialLinkWithNameShouldRoundTrip()
    {
        var version = new TelegramSocialLink(new TelegramChatId(11), new PrefferedName("leo"));
        var val = version.ToString();
        TelegramSocialLink.TryParse(val, provider: null, out var parseResult).ShouldBeTrue();
        parseResult.ShouldBe(version);
    }

    [Theory]
    [InlineData("Telegram(-1004315256401)")]
    [InlineData("-1004315256401")]
    public void TelegramSocialLinkShouldParseNegativeChannelId(string val)
    {
        TelegramSocialLink.TryParse(val, provider: null, out var parseResult).ShouldBeTrue();
        parseResult.ShouldBe(new TelegramSocialLink(new TelegramChatId(-1004315256401)));
    }

    [Fact]
    public void NegativeChannelIdShouldRoundTrip()
    {
        var version = new TelegramSocialLink(new TelegramChatId(-1004315256401));
        var val = version.ToString();
        TelegramSocialLink.TryParse(val, provider: null, out var parseResult).ShouldBeTrue();
        parseResult.ShouldBe(version);
    }
}
