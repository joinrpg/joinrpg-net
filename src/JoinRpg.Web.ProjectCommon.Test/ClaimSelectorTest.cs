using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.Web.ProjectCommon.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace JoinRpg.Web.ProjectCommon.Test;

/// <summary>
/// <see cref="ClaimSelector"/> постится в MVC-форме регистрации (CheckIn), поэтому value у опций
/// должен быть полным идентификатором заявки — его разбирает ProjectEntityIdModelBinder.
/// </summary>
public class ClaimSelectorTest
{
    private static readonly ProjectIdentification ProjectId = new(1);
    private static readonly ClaimIdentification FirstClaimId = new(ProjectId, 10);
    private static readonly ClaimIdentification SecondClaimId = new(ProjectId, 20);

    private sealed class FakeClaimListClient : IClaimListClient
    {
        public Task<IReadOnlyCollection<ClaimLinkViewModel>> GetClaims(ProjectIdentification projectId, ClaimStatusSpec claimStatusSpec)
            => Task.FromResult<IReadOnlyCollection<ClaimLinkViewModel>>(
            [
                new(FirstClaimId, new UserDisplayName("Игрок1", null), "Персонаж1", "", new UserIdentification(1)),
                new(SecondClaimId, new UserDisplayName("Игрок2", null), "Персонаж2", "", new UserIdentification(2)),
            ]);
    }

    private static BunitContext CreateContext()
    {
        var ctx = new BunitContext();
        ctx.Services.AddSingleton<IClaimListClient>(new FakeClaimListClient());
        // Non-interactive: TypedSelector не пытается импортировать JS-модуль bootstrap-select.
        ctx.SetRendererInfo(new RendererInfo("Static", isInteractive: false));
        return ctx;
    }

    [Fact]
    public void OptionValues_AreFullTypedIds()
    {
        using var ctx = CreateContext();

        var cut = ctx.Render<ClaimSelector>(p => p.Add(x => x.ProjectId, ProjectId));

        var values = cut.FindAll("option").Select(o => o.GetAttribute("value")).ToArray();
        values.ShouldBe([FirstClaimId.ToString(), SecondClaimId.ToString()]);
    }

    [Fact]
    public void ClaimId_MarksOptionSelected()
    {
        using var ctx = CreateContext();

        var cut = ctx.Render<ClaimSelector>(p => p
            .Add(x => x.ProjectId, ProjectId)
            .Add(x => x.ClaimId, SecondClaimId));

        cut.Find($"option[value='{SecondClaimId}']").HasAttribute("selected").ShouldBeTrue();
    }

    [Fact]
    public void Name_IsRenderedOnSelect_SoMvcFormCanPostIt()
    {
        using var ctx = CreateContext();

        var cut = ctx.Render<ClaimSelector>(p => p
            .Add(x => x.ProjectId, ProjectId)
            .Add(x => x.Name, "ClaimId"));

        cut.Find("select").GetAttribute("name").ShouldBe("ClaimId");
    }
}
