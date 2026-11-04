using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.DomainTypes;
using JoinRpg.Web.ProjectCommon.Claims;
using Microsoft.Extensions.DependencyInjection;

namespace JoinRpg.Web.Claims.Test;

/// <summary>
/// Перечисление заявок в предупреждениях страницы заявки (#6089): ссылки через запятую,
/// безымянная роль подписывается «заявка без персонажа», сверх лимита — «и ещё N заявок».
/// </summary>
public class OtherClaimsWarningViewTest
{
    private static readonly ProjectIdentification ProjectId = new(1);

    [Fact]
    public void RendersLinksSeparatedByCommas_WithClaimUri()
    {
        using var ctx = CreateContext();

        var cut = Render(ctx,
        [
            CreateLink(1, "Гэндальф"),
            CreateLink(2, "Фродо"),
        ], hiddenCount: 0);

        cut.Markup.ShouldContain(">Гэндальф</a>, <a");
        cut.Markup.ShouldContain(">Фродо</a>");
        var links = cut.FindAll("a");
        links.Count.ShouldBe(2);
        links[0].GetAttribute("href").ShouldBe("https://example.org/projects/1/claim/1");
        links[1].GetAttribute("href").ShouldBe("https://example.org/projects/1/claim/2");
    }

    [Fact]
    public void RendersUnnamedCharacterAsPlainClaimLink()
    {
        using var ctx = CreateContext();

        var cut = Render(ctx, [CreateLink(1, "")], hiddenCount: 0);

        cut.Markup.ShouldContain(">заявка без персонажа</a>");
    }

    [Fact]
    public void RendersHiddenClaimsCount_WithCorrectPluralForm()
    {
        using var ctx = CreateContext();

        var cut = Render(ctx, [CreateLink(1, "Гэндальф")], hiddenCount: 3);

        cut.Markup.ShouldContain("</a>, и ещё 3 заявки");
        cut.FindAll("a").Count.ShouldBe(1);
    }

    private static BunitContext CreateContext()
    {
        var ctx = new BunitContext();
        ctx.Services.AddSingleton<IUriLocator<ClaimIdentification>>(new FakeClaimUriLocator());
        return ctx;
    }

    private static IRenderedComponent<OtherClaimsWarningView> Render(BunitContext ctx,
        IReadOnlyList<ClaimLinkViewModel> claims, int hiddenCount)
        => ctx.Render<OtherClaimsWarningView>(p => p
            .Add(x => x.DisplayedClaims, claims)
            .Add(x => x.HiddenClaimsCount, hiddenCount));

    private static ClaimLinkViewModel CreateLink(int claimId, string characterName)
        => new(new ClaimIdentification(ProjectId, claimId), new UserDisplayName("Игрок", null), characterName, "", new UserIdentification(2));

    private sealed class FakeClaimUriLocator : IUriLocator<ClaimIdentification>
    {
        public Uri GetUri(ClaimIdentification target)
            => new($"https://example.org/projects/{target.ProjectId.Value}/claim/{target.ClaimId}");
    }
}
