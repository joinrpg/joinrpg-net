using JoinRpg.Common.WebInfrastructure.HtmlSanitize;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;
using Shouldly;
using Vereyon.Web;
using Xunit;

namespace JoinRpg.Common.WebInfrastructure.Test.HtmlSanitize;

public class JoinSanitizerPoolTests
{
    [Fact]
    public void DisposedSanitizer_ReturnedToPoolAndReused()
    {
        var created = 0;
        var pool = new DefaultObjectPool<HtmlSanitizer>(new CountingPolicy(() => { created++; return new HtmlSanitizer(); }));

        var s0 = new JoinSimpleHtmlSanitizer(pool);
        var s1 = new JoinSimpleHtmlSanitizer(pool);
        created.ShouldBe(2);

        s0.Dispose();

        // третий экземпляр должен взяться из пула, а не создаться заново
        var s2 = new JoinSimpleHtmlSanitizer(pool);
        created.ShouldBe(2);

        s2.Dispose();
        s1.Dispose();
    }

    [Fact]
    public void DisposeTwice_ReturnsToPoolOnlyOnce()
    {
        var created = 0;
        var pool = new DefaultObjectPool<HtmlSanitizer>(new CountingPolicy(() => { created++; return new HtmlSanitizer(); }));
        var sanitizer = new JoinTelegramHtmlSanitizer(pool);

        sanitizer.Dispose();
        // второй Dispose (например, от MS DI при разборе скоупа) — безопасный no-op
        sanitizer.Dispose();

        var next = new JoinTelegramHtmlSanitizer(pool);
        created.ShouldBe(1);
        next.Dispose();
    }

    [Fact]
    public void SanitizeAfterDispose_Throws()
    {
        var sanitizer = new JoinRemoveAllHtmlSanitizer(new DefaultObjectPool<HtmlSanitizer>(new CountingPolicy(() => new HtmlSanitizer())));

        sanitizer.Dispose();

        _ = Should.Throw<ObjectDisposedException>(() => sanitizer.Sanitize("<b>x</b>"));
    }

    [Fact]
    public void AddJoinHtmlSanitizers_RegistersAllThree()
    {
        using var provider = new ServiceCollection().AddJoinHtmlSanitizers().BuildServiceProvider();

        var simple = provider.GetRequiredService<IJoinSimpleHtmlSanitizer>();
        var removeAll = provider.GetRequiredService<IJoinRemoveAllHtmlSanitizer>();
        var telegram = provider.GetRequiredService<IJoinTelegramHtmlSanitizer>();

        simple.Sanitize("<b>x</b>").ShouldBe("<strong>x</strong>");
        removeAll.Sanitize("<b>x</b>").ShouldBe("x");
        telegram.Sanitize("<p>a</p>").ShouldBe("a");
    }

    [Fact]
    public void TelegramSanitizer_PreservesInlineMarkup()
    {
        using var provider = new ServiceCollection().AddJoinHtmlSanitizers().BuildServiceProvider();

        var telegram = provider.GetRequiredService<IJoinTelegramHtmlSanitizer>();

        telegram.Sanitize("<b>жирный</b> <script>alert(1)</script>").ShouldBe("<b>жирный</b> ");
    }

    private sealed class CountingPolicy(Func<HtmlSanitizer> factory) : IPooledObjectPolicy<HtmlSanitizer>
    {
        public HtmlSanitizer Create() => factory();

        public bool Return(HtmlSanitizer obj) => true;
    }
}
