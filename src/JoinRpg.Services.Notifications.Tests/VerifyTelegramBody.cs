using JoinRpg.Interfaces.Notifications;
using JoinRpg.Services.Notifications.Senders;

namespace JoinRpg.Services.Notifications.Tests;

public class VerifyTelegramBody
{
    private static RenderedEntityLink SampleLink => new(
        Markdown: new MarkdownDbValue("Подробнее: [комментарий](https://joinrpg.ru/1/claim/1/edit#comment42)"),
        PlainText: "Подробнее: комментарий: https://joinrpg.ru/1/claim/1/edit#comment42");

    [Theory]
    [InlineData(1, "Заголовок", "Привет!\n\nЭто **тело** сообщения.", false)]
    [InlineData(2, "Заголовок", "Привет!\n\nЭто **тело** сообщения.", true)]
    public Task Html(int num, string header, string body, bool withLink)
    {
        var result = TelegramSenderJobService.FormatMessage(
            header,
            new MarkdownDbValue(body),
            withLink ? SampleLink : null,
            new UserDisplayName("Master", null));

        return Verify(result.Contents).UseParameters(num);
    }

    [Theory]
    [InlineData(3, "Заголовок", "Привет!\n\nЭто **тело** сообщения.", false)]
    [InlineData(4, "Заголовок", "Привет!\n\nЭто **тело** сообщения.", true)]
    public Task HtmlSkipSignature(int num, string header, string body, bool withLink)
    {
        var result = TelegramSenderJobService.FormatMessage(
            header,
            new MarkdownDbValue(body),
            withLink ? SampleLink : null);

        return Verify(result.Contents).UseParameters(num);
    }
}
