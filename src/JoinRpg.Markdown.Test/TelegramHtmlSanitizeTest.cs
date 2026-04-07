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

    [Fact]
    public void TruncateHtml_ShortMessage_NotTruncated()
    {
        var html = "<b>Короткое сообщение</b>";
        var result = HtmlSanitizeFacade.TruncateHtml(html, 100);
        result.ShouldBe(html);
    }

    [Fact]
    public void TruncateHtml_LongPlainText_TruncatedWithSuffix()
    {
        var html = new string('a', 200);
        var result = HtmlSanitizeFacade.TruncateHtml(html, 100);
        result.Length.ShouldBeLessThanOrEqualTo(100);
        result.ShouldEndWith("...");
    }

    [Fact]
    public void TruncateHtml_LongTextWithOpenTag_TagsClosed()
    {
        var html = "<b>" + new string('a', 200) + "</b>";
        var result = HtmlSanitizeFacade.TruncateHtml(html, 20);
        result.ShouldEndWith("...</b>");
        result.Length.ShouldBeLessThanOrEqualTo(20 + "</b>".Length);
    }

    [Fact]
    public void TruncateHtml_CutInsideTag_CutBeforeTag()
    {
        // The cut point falls inside the <b> tag itself — should cut before the tag
        var html = "hello<b>world</b>";
        var result = HtmlSanitizeFacade.TruncateHtml(html, 8); // limit=5, "hello" + "..." = 8
        result.ShouldBe("hello...");
    }

    [Fact]
    public void TruncateHtml_ExactLength_NotTruncated()
    {
        var html = new string('x', 4096);
        var result = HtmlSanitizeFacade.TruncateHtml(html, 4096);
        result.ShouldBe(html);
    }

    [Fact]
    public void SanitizeHtml_TooLong_TruncatedTo4096()
    {
        var longContent = new string('x', 5000);
        var message = new TelegramHtmlString(longContent);
        var sanitized = message.SanitizeHtml();
        sanitized.Length.ShouldBeLessThanOrEqualTo(4096);
        sanitized.ShouldEndWith("...");
    }
}
