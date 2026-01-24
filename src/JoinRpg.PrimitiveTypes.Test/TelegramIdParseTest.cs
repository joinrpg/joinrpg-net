namespace JoinRpg.PrimitiveTypes.Test;

public class TelegramIdParseTest
{
    [Theory]
    [InlineData("Telegram(11)")]
    [InlineData("TelegramId(11)")]
    public void TelegramIdShouldParseWithoutName(string val)
    {
        TelegramId.TryParse(val, provider: null, out var parseResult).ShouldBeTrue();
        parseResult.ShouldBe(new TelegramId(11, null));
    }

    [Theory]
    [InlineData("Telegram(351484506, @)")]
    public void TelegramIdShouldParseSomewhatBroken(string val)
    {
        TelegramId.TryParse(val, provider: null, out var parseResult).ShouldBeTrue();
        parseResult.ShouldBe(new TelegramId(351484506, null));
    }

    [Fact]
    public void TelegramIdWithoutNameShouldRoundTrip()
    {
        var version = new TelegramId(11, null);
        var val = version.ToString();
        TelegramId.TryParse(val, provider: null, out var parseResult).ShouldBeTrue();
        parseResult.ShouldBe(version);
    }

    [Fact]
    public void TelegramIdWithEmptyNameShouldBeNormalized()
    {
        var version = new TelegramId(11, new PrefferedName(""));
        var val = version.ToString();
        TelegramId.TryParse(val, provider: null, out var parseResult).ShouldBeTrue();
        parseResult.ShouldBe(new TelegramId(11, null));
    }

    [Fact]
    public void TelegramIdWithNameShouldRoundTrip()
    {
        var version = new TelegramId(11, new PrefferedName("leo"));
        var val = version.ToString();
        TelegramId.TryParse(val, provider: null, out var parseResult).ShouldBeTrue();
        parseResult.ShouldBe(version);
    }
}
