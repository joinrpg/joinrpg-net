namespace JoinRpg.Common.WebComponents.Test;

/// <summary>
/// Проверки контрактов, на которые опирается MVC-разметка: тег-хелпер <c>&lt;component&gt;</c>
/// не умеет передавать вложенное содержимое, поэтому подсказку к тексту задают параметром
/// <see cref="Tooltip.Text"/>, а html-атрибуты кнопке — параметром <c>AdditionalAttributes</c>.
/// </summary>
public class TooltipTest
{
    [Fact]
    public void WithTextAndTitle_RendersTooltipAroundText()
    {
        using var ctx = new BunitContext();
        var cut = ctx.Render<Tooltip>(p => p
            .Add(x => x.Text, "42")
            .Add(x => x.Title, "Подсказка"));

        cut.Markup.ShouldContain("join-tooltip");
        cut.Markup.ShouldContain("42");
        cut.Markup.ShouldContain("Подсказка");
    }

    [Fact]
    public void WithTextAndWithoutTitle_RendersBareText()
    {
        using var ctx = new BunitContext();
        var cut = ctx.Render<Tooltip>(p => p.Add(x => x.Text, "42"));

        cut.Markup.Trim().ShouldBe("42");
    }

    /// <summary>
    /// В MVC-разметке обработчик подтверждения кнопке передают словарём атрибутов: диалог
    /// <c>AutoConfirm</c> требует интерактивности, которой на статически отрисованной странице нет.
    /// </summary>
    [Fact]
    public void JoinButtonWithAdditionalAttributes_RendersThemOnLink()
    {
        using var ctx = new BunitContext();
        var cut = ctx.Render<JoinButton>(p => p
            .Add(x => x.Link, "/1/rooms/DeleteRoomType")
            .Add(x => x.Icon, JoinIconType.Delete)
            .Add(x => x.Title, "Удалить")
            .Add(x => x.AdditionalAttributes, new Dictionary<string, object>
            {
                ["onclick"] = "return confirm('Удалить?')",
            }));

        cut.Markup.ShouldContain("join-tooltip");
        cut.Markup.ShouldContain("onclick");
        cut.Markup.ShouldContain("return confirm(");
    }
}
