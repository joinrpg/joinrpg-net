namespace JoinRpg.Common.WebComponents.Test;

/// <summary>
/// <see cref="JoinLoadingMessage"/> рендерит <see cref="JoinAlert"/> — после перевода алерта
/// на собственные классы сообщение об ожидании не зависит от Bootstrap CSS.
/// </summary>
public class JoinLoadingMessageTest
{
    [Fact]
    public void RendersDefaultMessage()
    {
        using var ctx = new BunitContext();
        var cut = ctx.Render<JoinLoadingMessage>();

        var alert = cut.Find("div.join-alert");
        alert.TextContent.Trim().ShouldBe("Идет загрузка...");
        alert.ClassList.ShouldAllBe(c => c != "alert" && !c.StartsWith("alert-"));
    }

    [Fact]
    public void RendersCustomMessage()
    {
        using var ctx = new BunitContext();
        var cut = ctx.Render<JoinLoadingMessage>(p => p
            .Add(x => x.Message, "Подождите..."));

        cut.Find("div.join-alert").TextContent.Trim().ShouldBe("Подождите...");
    }
}
