namespace JoinRpg.Common.WebComponents.Test;

/// <summary>
/// Меню <see cref="JoinDropdownButton"/> раскрывает сам браузер: пункты лежат внутри &lt;details&gt; рядом
/// с &lt;summary&gt;-кнопкой и видны только у открытого. Скрипт закрытия (lib.module.js) ищет меню по
/// классу <c>join-dropdown</c> у &lt;details&gt; — это его контракт с разметкой.
/// </summary>
public class JoinDropdownButtonTest
{
    [Fact]
    public void Items_AreInsideDetailsAfterSummary()
    {
        using var ctx = new BunitContext();
        var cut = ctx.Render<JoinDropdownButton>(p => p
            .Add(x => x.AriaLabel, "Другие варианты печати")
            .Add(x => x.ChildContent, b =>
            {
                b.OpenComponent<JoinMenuItem>(0);
                b.AddAttribute(1, nameof(JoinMenuItem.Href), "/badges");
                b.AddAttribute(2, nameof(JoinMenuItem.Label), "Распечатать бейджи");
                b.CloseComponent();
            }));

        cut.Find("details.join-dropdown > summary + ul a").GetAttribute("href").ShouldBe("/badges");
    }

    [Fact]
    public void StyleAndSize_AddJoinButtonModifierClasses()
    {
        using var ctx = new BunitContext();
        var cut = ctx.Render<JoinDropdownButton>(p => p
            .Add(x => x.Label, "Меню")
            .Add(x => x.Style, VariationStyleEnum.Primary)
            .Add(x => x.Size, SizeStyleEnum.Small));

        cut.Find("summary").GetAttribute("class")
            .ShouldBe("join-dropdown__toggle join-btn join-btn--primary join-btn--sm");
    }

    [Fact]
    public void ToggleContent_ReplacesLabelAndCaret()
    {
        using var ctx = new BunitContext();
        var cut = ctx.Render<JoinDropdownButton>(p => p
            .Add(x => x.AriaLabel, "действия")
            .Add(x => x.ToggleContent, b => b.AddContent(0, "≡"))
            .AddChildContent("<li><a href=\"/go\">пункт</a></li>"));

        var summary = cut.Find("summary");
        summary.TextContent.ShouldContain("≡");
        summary.QuerySelector(".join-dropdown__label, .join-dropdown__caret").ShouldBeNull();
    }
}
