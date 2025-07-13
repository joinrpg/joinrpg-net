namespace JoinRpg.Markdown.Test;
public class RoundTripUrls
{
    [Fact]
    public void RoundTripUrl()
    {
        var x = "https://localhost:5001/account/resetpassword?userId=1&code=CfDJ8EPySJiU9fJDu%2F";

        new MarkdownString(x).ToPlainTextWithoutHtmlEscape().ShouldBe(x);
    }
}
