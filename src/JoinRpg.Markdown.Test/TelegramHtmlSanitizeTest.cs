using JoinRpg.Interfaces;

namespace JoinRpg.Markdown.Test;

public class TelegramHtmlSanitizeTest
{
    [Fact]
    public void TestPRemoved()
    {
        var message = new TelegramHtmlString("<p>Добрый день, Atana!</p>\n<p>Это тестовое сообщение</p>");
        var sanitized = message.SanitizeHtml(4096);
        sanitized.ShouldBe("Добрый день, Atana!\nЭто тестовое сообщение");
    }

    [Fact]
    public void SanitizeHtml_ShortMessage_NotTruncated()
    {
        var message = new TelegramHtmlString("<b>Короткое сообщение</b>");
        var sanitized = message.SanitizeHtml(100);
        sanitized.ShouldBe("<b>Короткое сообщение</b>");
    }

    [Fact]
    public void SanitizeHtml_TooLong_TruncatedTo4096()
    {
        var longContent = new string('x', 5000);
        var message = new TelegramHtmlString(longContent);
        var sanitized = message.SanitizeHtml(4096);
        sanitized.Length.ShouldBeLessThanOrEqualTo(4096);
        sanitized.ShouldEndWith("...");
    }
}
