using Vereyon.Web;

namespace JoinRpg.Markdown.Test;

public class ImageTests
{
    [Fact]
    public void TestImage()
        => @"![Image](https://joinrpg.ru/a.png)".ShouldBeHtml("<p><img src=\"https://joinrpg.ru/a.png\" alt=\"Image\"></p>");


    [Fact]
    public void ImgTagShouldSanitizeCorrectly()
    {
        var str = "<img src=\"https://joinrpg.ru/a.png\" />";
        var sanitizer = new HtmlSanitizer();

        sanitizer.WhiteListMode = true;
        _ = HtmlSanitizerFluentHelper.Tag(sanitizer, "img").AllowAttributes("src");
        sanitizer.Sanitize(str).ShouldBe("<img src=\"https://joinrpg.ru/a.png\">");
    }

}
