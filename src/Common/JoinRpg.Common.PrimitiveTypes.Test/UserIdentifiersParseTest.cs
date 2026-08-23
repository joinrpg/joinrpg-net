namespace JoinRpg.Common.PrimitiveTypes.Test;

public class UserIdentifiersParseTest
{
    [Theory]
    [InlineData("1")]
    [InlineData("TelegramId(1)")]
    public void TelegramShouldParseTo1(string val)
    {
        TelegramSocialLink.TryParse(val, provider: null, out var parseResult).ShouldBeTrue();
        parseResult.Id.ShouldBe(1);
    }

    [Theory]
    [InlineData("TelegramId(1, leotsarev)")]
    public void TelegramShouldParseTo1_andusername(string val)
    {
        TelegramSocialLink.TryParse(val, provider: null, out var parseResult).ShouldBeTrue();
        parseResult.Id.ShouldBe(1);
        parseResult.PrettyName.ShouldBe(new PrefferedName("leotsarev"));
    }


    [Theory]
    [InlineData("xxxx")]
    [InlineData("Pr(1)")]
    [InlineData("Pr1")]
    public void TelegramFailToParse(string val)
    {
        TelegramSocialLink.TryParse(val, provider: null, out var _).ShouldBeFalse();
    }

    [Fact]
    public void TelegramShouldRoundTrip()
    {
        var val = new TelegramSocialLink(new TelegramChatId(12), new PrefferedName("leotsarev"));
        TelegramSocialLink.TryParse(val.ToString(), provider: null, out var parseResult).ShouldBeTrue();
        parseResult.ShouldBe(val);
    }
}
