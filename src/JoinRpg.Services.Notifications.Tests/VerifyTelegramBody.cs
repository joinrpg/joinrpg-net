using JoinRpg.Services.Notifications.Senders;

namespace JoinRpg.Services.Notifications.Tests;

public class VerifyTelegramBody
{
    [Theory]
    [InlineData([1, "Заголовок", "Привет!\n\nЭто **тело** сообщения."])]
    public Task Html(int num, string header, string body)
    {
        var result = TelegramSenderJobService.FormatMessage(
            header,
            new MarkdownString(body),
            new UserDisplayName("Master", null));

        return Verify(result.Contents).UseParameters(num);
    }
}
