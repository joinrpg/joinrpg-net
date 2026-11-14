namespace JoinRpg.Common.PrimitiveTypes.Test;

public class TelegramSocialLinkParseTest
{
    [Theory]
    [InlineData("Telegram(11)")]
    [InlineData("TelegramSocialLink(11)")]
    public void TelegramSocialLinkShouldParseWithoutName(string val)
    {
        TelegramSocialLink.TryParse(val, provider: null, out var parseResult).ShouldBeTrue();
        parseResult.ShouldBe(new TelegramSocialLink(new TelegramChatId(11), isVerified: false));
    }

    [Theory]
    [InlineData("Telegram(351484506, @)")]
    public void TelegramSocialLinkShouldParseSomewhatBroken(string val)
    {
        TelegramSocialLink.TryParse(val, provider: null, out var parseResult).ShouldBeTrue();
        parseResult.ShouldBe(new TelegramSocialLink(new TelegramChatId(351484506), isVerified: false));
    }

    [Theory]
    [InlineData("Telegram(5159651684, @)")]
    [InlineData("Telegram(5159651684)")]
    public void TelegramSocialLinkShouldParseLong(string val)
    {
        TelegramSocialLink.TryParse(val, provider: null, out var parseResult).ShouldBeTrue();
        parseResult.ShouldBe(new TelegramSocialLink(new TelegramChatId(5159651684), isVerified: false));
    }

    [Fact]
    public void TelegramSocialLinkWithoutNameShouldRoundTrip()
    {
        var version = new TelegramSocialLink(new TelegramChatId(11), isVerified: false);
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
        parseResult.ShouldBe(new TelegramSocialLink(new TelegramChatId(11), isVerified: false));
    }

    [Fact]
    public void TelegramSocialLinkWithNameShouldRoundTrip()
    {
        // ToString не кодирует верификацию: round-trip через строку её теряет.
        var version = new TelegramSocialLink(new TelegramChatId(11), new PrefferedName("leo"), isVerified: false);
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
        parseResult.ShouldBe(new TelegramSocialLink(new TelegramChatId(-1004315256401), isVerified: false));
    }

    [Fact]
    public void NegativeChannelIdShouldRoundTrip()
    {
        var version = new TelegramSocialLink(new TelegramChatId(-1004315256401), isVerified: false);
        var val = version.ToString();
        TelegramSocialLink.TryParse(val, provider: null, out var parseResult).ShouldBeTrue();
        parseResult.ShouldBe(version);
    }

    [Fact]
    public void CtorWithChatIdIsVerifiedByDefault()
    {
        var result = new TelegramSocialLink(new TelegramChatId(12345), new PrefferedName("leo"));

        result.IsVerified.ShouldBeTrue();
    }

    [Fact]
    public void CtorExplicitIsVerifiedFalseNotVerified()
    {
        var result = new TelegramSocialLink(new TelegramChatId(12345), new PrefferedName("leo"), isVerified: false);

        result.IsVerified.ShouldBeFalse();
    }

    [Fact]
    public void FromOptionalIsVerifiedByDefault()
    {
        var result = TelegramSocialLink.FromOptional(12345L, new PrefferedName("leo"));

        result.ShouldNotBeNull();
        result.IsVerified.ShouldBeTrue();
    }

    [Fact]
    public void FromOptionalExplicitIsVerifiedFalseNotVerified()
    {
        var result = TelegramSocialLink.FromOptional(12345L, new PrefferedName("leo"), isVerified: false);

        result.ShouldNotBeNull();
        result.IsVerified.ShouldBeFalse();
    }

    [Fact]
    public void TryParseIsNotVerified()
    {
        TelegramSocialLink.TryParse("Telegram(11)", provider: null, out var parseResult).ShouldBeTrue();
        parseResult.IsVerified.ShouldBeFalse();
    }

    [Fact]
    public void FromUserData_WithExternalLogin_IsVerified()
    {
        var result = TelegramSocialLink.FromUserData("12345", new PrefferedName("leo"));

        result.ShouldNotBeNull();
        result.Id.ShouldBe(12345);
        result.IsVerified.ShouldBeTrue();
        result.PrettyName!.Value.ShouldBe("leo");
    }

    [Fact]
    public void FromUserData_WithoutExternalLoginButWithPrettyName_IsNotVerified()
    {
        var result = TelegramSocialLink.FromUserData(null, new PrefferedName("leo"));

        result.ShouldNotBeNull();
        result.Id.ShouldBeNull();
        result.IsVerified.ShouldBeFalse();
        result.PrettyName!.Value.ShouldBe("leo");
        result.Link.ShouldNotBeNull();
    }

    [Fact]
    public void FromUserData_WithoutExternalLoginAndWithoutPrettyName_ReturnsNull()
    {
        TelegramSocialLink.FromUserData(null, null).ShouldBeNull();
    }
}
