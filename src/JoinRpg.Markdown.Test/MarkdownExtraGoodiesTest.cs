using Xunit;

namespace JoinRpg.Markdown.Test;

public class MarkdownExtraGoodiesTest
{

    [Fact]
    public void RenderYandex()
    {
        "![Audio4](https://music.yandex.ru/album/411845/track/4402274)"
            .ShouldBeHtml("<p><iframe src=\"https://music.yandex.ru/iframe/#track/4402274/411845/\" width=\"500\" height=\"281\" frameborder=\"0\"></iframe></p>");
    }
}
