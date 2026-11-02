namespace JoinRpg.Common.WebComponents.Test;

/// <summary>
/// <see cref="JoinBadge.TooltipContent"/> — подсказка бейджа разметкой, когда простой строки
/// <see cref="JoinBadge.Title"/> мало (статус оплаты заявки в гриде заявок).
/// </summary>
public class JoinBadgeTooltipContentTest
{
    [Fact]
    public void TooltipContent_IsRenderedInsideTooltip()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<JoinBadge>(p => p
            .Add(x => x.ChildContent, "<b>500 / 1000</b>")
            .Add(x => x.TooltipContent, "<span>(переплата 400)</span>"));

        cut.Find(".join-tooltip").InnerHtml.ShouldBe("<span>(переплата 400)</span>");
    }

    [Fact]
    public void WithoutTitleAndTooltipContent_NoTooltipWrapper()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<JoinBadge>(p => p.Add(x => x.Text, "бейдж"));

        cut.FindAll(".join-tooltip").ShouldBeEmpty();
    }
}
