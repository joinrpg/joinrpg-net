using JoinRpg.Interfaces.Notifications;
using JoinRpg.Services.Notifications.Senders.PostboxEmail;

namespace JoinRpg.Services.Email.Test;

public class VerifyPostboxBody
{
    private static RenderedEntityLink SampleLink => new(
        Markdown: "Подробнее: [комментарий](https://joinrpg.ru/1/claim/1/edit#comment42)",
        PlainText: "Подробнее: комментарий: https://joinrpg.ru/1/claim/1/edit#comment42");

    [Theory]
    [InlineData(1, "Добрый день, Клэр!\n\n## Заголовок\n\n", false)]
    [InlineData(2, "Добрый день, Клэр!\n\n## Заголовок\n\n", true)]
    public Task Text(int num, string s, bool withLink)
    {
        var body = PostboxSenderJobService.FormatBody(new MarkdownString(s), new UserDisplayName("Master", null), withLink ? SampleLink : null);

        body.Text.Charset.ShouldBe("UTF-8");

        return Verify(body.Text.Data).UseParameters(num);
    }

    [Theory]
    [InlineData(1, "Добрый день, Клэр!\n\n## Заголовок\n\n", false)]
    [InlineData(2, "Добрый день, Клэр!\n\n## Заголовок\n\n", true)]
    public Task Html(int num, string s, bool withLink)
    {
        var body = PostboxSenderJobService.FormatBody(new MarkdownString(s), new UserDisplayName("Master", null), withLink ? SampleLink : null);

        body.Html.Charset.ShouldBe("UTF-8");

        return Verify(body.Html.Data).UseParameters(num);
    }
}
