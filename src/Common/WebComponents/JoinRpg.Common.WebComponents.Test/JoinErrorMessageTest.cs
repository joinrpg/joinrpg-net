namespace JoinRpg.Common.WebComponents.Test;

/// <summary>
/// <see cref="JoinErrorMessage"/> рендерит <see cref="JoinAlert"/> в вариации Danger —
/// после перевода алерта на собственные классы сообщение об ошибке не зависит от Bootstrap CSS.
/// </summary>
public class JoinErrorMessageTest
{
    [Fact]
    public void RendersDefaultErrorMessage()
    {
        using var ctx = new BunitContext();
        var cut = ctx.Render<JoinErrorMessage>();

        var alert = cut.Find("div.join-alert");
        alert.ClassList.ShouldContain("join-alert--danger");
        alert.TextContent.Trim().ShouldBe("Произошла ошибка...");
        alert.ClassList.ShouldAllBe(c => c != "alert" && !c.StartsWith("alert-"));
    }

    [Fact]
    public void RendersChildContent()
    {
        using var ctx = new BunitContext();
        var cut = ctx.Render<JoinErrorMessage>(p => p
            .AddChildContent("Сбой сохранения"));

        cut.Find("div.join-alert").TextContent.Trim().ShouldBe("Сбой сохранения");
    }
}
