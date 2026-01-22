using JoinRpg.Services.Notifications.Senders.PostboxEmail;

namespace JoinRpg.Services.Email.Test;

public class VerifyPostboxBody
{

    [Theory]
    [InlineData([1, "Добрый день, Клэр!\n\n## Заголовок\n\n"])]
    public Task Text(int num, string s)
    {
        var body = PostboxSenderJobService.FormatBody(new MarkdownString(s), new UserDisplayName("Master", null));

        body.Text.Charset.ShouldBe("UTF-8");

        return Verify(body.Text.Data).UseParameters(num);
    }

    [Theory]
    [InlineData([1, "Добрый день, Клэр!\n\n## Заголовок\n\n"])]
    public Task Html(int num, string s)
    {
        var body = PostboxSenderJobService.FormatBody(new MarkdownString(s), new UserDisplayName("Master", null));

        body.Html.Charset.ShouldBe("UTF-8");

        return Verify(body.Html.Data).UseParameters(num);
    }
}
