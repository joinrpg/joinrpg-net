namespace JoinRpg.PrimitiveTypes.Test;
public class EmailParseTest
{
    [Theory]
    [InlineData("leo@bastilia.ru")]
    [InlineData("Email(leo@bastilia.ru)")]
    public void EmailLeoBastiliaShouldParse(string val)
    {
        Email.TryParse(val, provider: null, out var parseResult).ShouldBeTrue();
        parseResult.Value.ShouldBe("leo@bastilia.ru");
    }

    [Theory]
    [InlineData("xxxx")]
    [InlineData("Pr(1)")]
    [InlineData("Pr1")]
    [InlineData("Email()")]
    public void EmailFailToParse(string val)
    {
        Email.TryParse(val, provider: null, out var _).ShouldBeFalse();
    }

    [Fact]
    public void EmailShouldRoundTrip()
    {
        var val = new Email("leo@bastilia.ru");
        Email.TryParse(val.ToString(), provider: null, out var parseResult).ShouldBeTrue();
        parseResult.ShouldBe(val);
    }
}
