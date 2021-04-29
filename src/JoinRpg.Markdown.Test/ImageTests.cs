using JoinRpg.Markdown;
using Microsoft.VisualBasic;
using Shouldly;
using Vereyon.Web;
using Xunit;

namespace JoinRpg.Markdown.Test
{

    public class ImageTests
    {
        [Fact]
        public void TestImage()
            => @"![Image](http://joinrpg.ru/a.png)".ShouldBeHtml("<p><img src=\"http://joinrpg.ru/a.png\" alt=\"Image\"></p>");


        [Fact]
        public void ImgTagShouldSanitizeCorrectly()
        {
            var str = "<img src=\"http://joinrpg.ru/a.png\" />";
            var sanitizer = new HtmlSanitizer();

            sanitizer.WhiteListMode = true;
            _ = sanitizer.Tag("img").AllowAttributes("src");
            sanitizer.Sanitize(str).ShouldBe("<img src=\"http://joinrpg.ru/a.png\">");
        }

    }
}
