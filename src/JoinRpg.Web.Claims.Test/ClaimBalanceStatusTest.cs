using JoinRpg.DomainTypes.Characters.Claims.Finances;
using JoinRpg.Web.Claims.Finance;

namespace JoinRpg.Web.Claims.Test;

/// <summary>
/// <see cref="ClaimBalanceStatus"/> показывает вариант только актуального статуса оплаты.
/// В режиме <c>ForClaimJs</c> (страница заявки) рендерятся все четыре варианта с id — их
/// переключает claim.js, поэтому id — контракт.
/// </summary>
public class ClaimBalanceStatusTest
{
    private static IRenderedComponent<ClaimBalanceStatus> Render(
        BunitContext ctx,
        ClaimBalance balance,
        bool forClaimJs = false,
        bool plainText = false)
        => ctx.Render<ClaimBalanceStatus>(p => p
            .Add(x => x.Model, balance)
            .Add(x => x.ForClaimJs, forClaimJs)
            .Add(x => x.PlainText, plainText));

    [Fact]
    public void Overpaid_ShowsOverpaidSum()
    {
        using var ctx = new BunitContext();

        var cut = Render(ctx, new ClaimBalance(FeePaid: 500, TotalFee: 100));

        cut.Markup.ShouldContain("(переплата <strong");
        cut.Markup.ShouldContain(">400<");
        cut.Markup.ShouldNotContain("осталось оплатить");
    }

    [Fact]
    public void MoreToPay_ShowsPaidAndDue()
    {
        using var ctx = new BunitContext();

        var cut = Render(ctx, new ClaimBalance(FeePaid: 300, TotalFee: 1000));

        cut.Markup.ShouldContain("(оплачено <strong");
        cut.Markup.ShouldContain("осталось оплатить <strong");
        cut.Markup.ShouldContain(">300<");
        cut.Markup.ShouldContain(">700<");
        cut.Markup.ShouldNotContain("переплата");
    }

    [Fact]
    public void Paid_ShowsPaidInFull()
    {
        using var ctx = new BunitContext();

        var cut = Render(ctx, new ClaimBalance(FeePaid: 1000, TotalFee: 1000));

        cut.Markup.ShouldContain("(оплачен полностью)");
        cut.Markup.ShouldNotContain("переплата");
    }

    [Fact]
    public void NotPaid_ShowsNotPaid()
    {
        using var ctx = new BunitContext();

        var cut = Render(ctx, new ClaimBalance(FeePaid: 0, TotalFee: 1000));

        cut.Markup.ShouldContain("(не оплачен)");
        cut.Markup.ShouldNotContain("осталось оплатить");
    }

    /// <summary>Без ForClaimJs id не рендерятся: вариант один, переключать скриптом нечего.</summary>
    [Fact]
    public void WithoutForClaimJs_NoIdsRendered()
    {
        using var ctx = new BunitContext();

        var cut = Render(ctx, new ClaimBalance(FeePaid: 500, TotalFee: 100));

        cut.Markup.ShouldNotContain("id=");
    }

    /// <summary>
    /// PlainText — тот же текст без оформления (цветные суммы, Money-разметка), для тёмного
    /// tooltip грида заявок.
    /// </summary>
    [Fact]
    public void PlainText_SameWordingWithoutStyling()
    {
        using var ctx = new BunitContext();

        var cut = Render(ctx, new ClaimBalance(FeePaid: 300, TotalFee: 1000), plainText: true);

        cut.Markup.ShouldBe("(оплачено 300, осталось оплатить 700)");
        cut.Markup.ShouldNotContain("text-danger");
        cut.Markup.ShouldNotContain("price-value");
    }

    [Fact]
    public void PlainText_OverpaidAndNotPaid()
    {
        using var ctx = new BunitContext();

        Render(ctx, new ClaimBalance(FeePaid: 500, TotalFee: 100), plainText: true)
            .Markup.ShouldBe("(переплата 400)");
        Render(ctx, new ClaimBalance(FeePaid: 0, TotalFee: 1000), plainText: true)
            .Markup.ShouldBe("(не оплачен)");
        Render(ctx, new ClaimBalance(FeePaid: 1000, TotalFee: 1000), plainText: true)
            .Markup.ShouldBe("(оплачен полностью)");
    }

    /// <summary>Контракт claim.js сильнее: даже с PlainText рендерятся все варианты с id.</summary>
    [Fact]
    public void ForClaimJs_WinsOverPlainText()
    {
        using var ctx = new BunitContext();

        var cut = Render(ctx, new ClaimBalance(FeePaid: 500, TotalFee: 100), forClaimJs: true, plainText: true);

        cut.Markup.ShouldContain("id=\"feeInfo1\"");
        cut.Markup.ShouldContain("id=\"feeInfo3\"");
        Visible(cut, "feeInfo1").ShouldBeTrue();
        Visible(cut, "feeInfo2").ShouldBeFalse();
    }

    /// <summary>В режиме ForClaimJs все четыре варианта с id должны быть в разметке, актуальный видим.</summary>
    [Fact]
    public void ForClaimJs_AllVariantsWithIdsArePresent()
    {
        using var ctx = new BunitContext();

        var cut = Render(ctx, new ClaimBalance(FeePaid: 500, TotalFee: 100), forClaimJs: true);

        cut.Markup.ShouldContain("id=\"feeInfo0\"");
        cut.Markup.ShouldContain("id=\"feeInfo1\"");
        cut.Markup.ShouldContain("id=\"feeInfo2\"");
        cut.Markup.ShouldContain("id=\"feeInfo3\"");
        cut.Markup.ShouldContain("id=\"feeOverpaidDisp2\"");
        cut.Markup.ShouldContain("id=\"feeMoreToPayDisp2\"");
        Visible(cut, "feeInfo1").ShouldBeTrue();
        Visible(cut, "feeInfo2").ShouldBeFalse();
    }

    private static bool Visible(IRenderedComponent<ClaimBalanceStatus> cut, string id)
        => cut.Find($"#{id}").GetAttribute("style") is not { } style || !style.Contains("display: none");
}
