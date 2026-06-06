using JoinRpg.Interfaces.Notifications;
using JoinRpg.Services.Notifications.Senders;

namespace JoinRpg.Services.Notifications.Tests;

public class VerifyTelegramBody
{
    private static RenderedEntityLink SampleLink => new(
        Markdown: new MarkdownString("Подробнее: [комментарий](https://joinrpg.ru/1/claim/1/edit#comment42)"),
        PlainText: "Подробнее: комментарий: https://joinrpg.ru/1/claim/1/edit#comment42");

    [Theory]
    [InlineData(1, "Заголовок", "Привет!\n\nЭто **тело** сообщения.", false)]
    [InlineData(2, "Заголовок", "Привет!\n\nЭто **тело** сообщения.", true)]
    public Task Html(int num, string header, string body, bool withLink)
    {
        var result = TelegramSenderJobService.FormatMessage(
            header,
            new MarkdownString(body),
            new UserDisplayName("Master", null),
            withLink ? SampleLink : null);

        return Verify(result.Contents).UseParameters(num);
    }
}
