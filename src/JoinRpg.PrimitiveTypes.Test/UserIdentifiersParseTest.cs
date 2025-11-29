namespace JoinRpg.PrimitiveTypes.Test;
public class UserIdentifiersParseTest
{
    [Theory]
    [InlineData("1")]
    [InlineData("TelegramId(1)")]
    public void TelegramShouldParseTo1(string val)
    {
        TelegramId.TryParse(val, provider: null, out var parseResult).ShouldBeTrue();
        parseResult.Id.ShouldBe(1);
    }

    [Theory]
    [InlineData("TelegramId(1, leotsarev)")]
    public void TelegramShouldParseTo1_andusername(string val)
    {
        TelegramId.TryParse(val, provider: null, out var parseResult).ShouldBeTrue();
        parseResult.Id.ShouldBe(1);
        parseResult.UserName.ShouldBe(new PrefferedName("leotsarev"));
    }


    [Theory]
    [InlineData("xxxx")]
    [InlineData("Pr(1)")]
    [InlineData("Pr1")]
    public void TelegramFailToParse(string val)
    {
        TelegramId.TryParse(val, provider: null, out var _).ShouldBeFalse();
    }

    [Fact]
    public void TelegramShouldRoundTrip()
    {
        var val = new TelegramId(12, new PrefferedName("leotsarev"));
        TelegramId.TryParse(val.ToString(), provider: null, out var parseResult).ShouldBeTrue();
        parseResult.ShouldBe(val);
    }
}
