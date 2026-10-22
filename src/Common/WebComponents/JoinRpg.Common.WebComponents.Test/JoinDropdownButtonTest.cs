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
}
