using JoinRpg.DomainTypes.Characters.Claims.Finances;
using JoinRpg.Web.Claims.Finance;

namespace JoinRpg.Web.Claims.Test;

/// <summary>
/// <see cref="ClaimBalanceView"/> — бейдж «уплачено / взнос» в гриде заявок. В подсказке бейджа —
/// полный статус оплаты (<see cref="ClaimBalanceStatus"/>), цвет — по статусу.
/// </summary>
public class ClaimBalanceViewTest
{
    private static IRenderedComponent<ClaimBalanceView> Render(BunitContext ctx, ClaimBalance balance)
        => ctx.Render<ClaimBalanceView>(p => p.Add(x => x.Model, balance));

    [Fact]
    public void BadgeShowsPaidOverTotal_AndStatusInTooltip()
    {
        using var ctx = new BunitContext();

        var cut = Render(ctx, new ClaimBalance(FeePaid: 500, TotalFee: 100));

        cut.Markup.ShouldContain("500 / 100");
        cut.Markup.ShouldContain("переплата");
        cut.Markup.ShouldContain("label-success");
        // В подсказке бейджа — одиночный режим ClaimBalanceStatus, без id для claim.js
        cut.Markup.ShouldNotContain("id=");
    }

    [Fact]
    public void NotPaidBadgeIsDanger()
    {
        using var ctx = new BunitContext();

        var cut = Render(ctx, new ClaimBalance(FeePaid: 0, TotalFee: 1000));

        cut.Markup.ShouldContain("label-danger");
        cut.Markup.ShouldContain("(не оплачен)");
    }
}
