using Vereyon.Web;

namespace JoinRpg.Markdown.Test;

public class SanitizerPoolTests
{
    [Fact]
    public void DisposableSanitizer_Get_Return_Get()
    {
        var counter = 0;
        var pool = new HtmlSanitizers.HtmlSanitizerPool(() => new MockSanitizer((counter++).ToString()));

        var s0 = pool.Get();
        var s1 = pool.Get();
        s0.Sanitize("").ShouldBe("0");
        s1.Sanitize("").ShouldBe("1");
        s0.Dispose();
        pool.Count.ShouldBe(1);
        s0 = pool.Get();
        pool.Count.ShouldBe(0);
        s0.Sanitize("").ShouldBe("0");
        s1.Dispose();
        pool.Count.ShouldBe(1);
        s1 = pool.Get();
        pool.Count.ShouldBe(0);
        s1.Sanitize("").ShouldBe("1");
        s0.Dispose();
        s1.Dispose();
        pool.Count.ShouldBe(2);
    }

    private class MockSanitizer(string result) : IHtmlSanitizer
    {
        /// <inheritdoc />
        public string Sanitize(string html) => result;
    }
}
