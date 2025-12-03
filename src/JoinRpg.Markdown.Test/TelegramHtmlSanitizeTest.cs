using JoinRpg.Interfaces;

namespace JoinRpg.Markdown.Test;
public class TelegramHtmlSanitizeTest
{
    [Fact]
    public void TestPRemoved()
    {
        var message = new TelegramHtmlString("<p>Добрый день, Atana!</p>\n<p>Это тестовое сообщение</p>");
        var sanitized = message.SanitizeHtml();
        sanitized.ShouldBe("Добрый день, Atana!\nЭто тестовое сообщение");
    }
}
