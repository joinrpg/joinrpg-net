using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Common.WebComponents;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.Web.ProjectCommon.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;

namespace JoinRpg.Web.ProjectCommon.Test;

/// <summary>
/// <see cref="ClaimSelector"/> постится в MVC-форме регистрации (CheckIn), поэтому value у пунктов
/// должен быть полным идентификатором заявки — его разбирает ProjectEntityIdModelBinder.
/// </summary>
public class ClaimSelectorTest
{
    private static readonly ProjectIdentification ProjectId = new(1);
    private static readonly ClaimIdentification FirstClaimId = new(ProjectId, 10);
    private static readonly ClaimIdentification SecondClaimId = new(ProjectId, 20);

    private sealed class FormModel
    {
        public ClaimIdentification? ClaimId { get; set; }
    }

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
        _ = ctx.Services.AddJoinRpgWebComponents();
        _ = ctx.AddBunitPersistentComponentState();
        // Non-interactive: так TypedSelector рендерится в MVC-формах (render-mode="Static").
        ctx.SetRendererInfo(new RendererInfo("Static", isInteractive: false));
        return ctx;
    }

    /// <summary>
    /// Контракт с ProjectEntityIdModelBinder: он разбирает пришедшее из формы значение через
    /// TryParse, поэтому value у пункта должно им и разбираться — обратно в тот же id.
    /// </summary>
    [Fact]
    public void OptionValues_AreParsedBackIntoSameIds()
    {
        using var ctx = CreateContext();

        var cut = ctx.Render<ClaimSelector>(p => p.Add(x => x.ProjectId, ProjectId));

        var values = cut.FindAll(".join-select__list input").Select(o => o.GetAttribute("value")).ToArray();
        values.Length.ShouldBe(2);

        var parsed = values.Select(v =>
        {
            ClaimIdentification.TryParse(v, null, out var id).ShouldBeTrue($"Не разобрался id: '{v}'");
            return id;
        });
        parsed.ShouldBe([FirstClaimId, SecondClaimId]);
    }

    /// <summary>Интерактивный режим: выбор уходит наружу и как id, и как заявка целиком.</summary>
    [Fact]
    public void Selecting_RaisesBothIdAndClaimCallbacks()
    {
        using var ctx = CreateContext();
        ctx.SetRendererInfo(new RendererInfo("WebAssembly", isInteractive: true));

        ClaimIdentification? changedId = null;
        ClaimLinkViewModel? changedClaim = null;

        var cut = ctx.Render<ClaimSelector>(p => p
            .Add(x => x.ProjectId, ProjectId)
            .Add(x => x.ClaimIdChanged, id => changedId = id)
            .Add(x => x.ClaimChanged, claim => changedClaim = claim));

        cut.Find($"input[value='{SecondClaimId}']").Change(true);

        changedId.ShouldBe(SecondClaimId);
        changedClaim.ShouldNotBeNull().CharacterName.ShouldBe("Персонаж2");
    }

    [Fact]
    public void ClaimId_MarksOptionSelected()
    {
        using var ctx = CreateContext();

        var cut = ctx.Render<ClaimSelector>(p => p
            .Add(x => x.ProjectId, ProjectId)
            .Add(x => x.ClaimId, SecondClaimId));

        cut.Find($"input[value='{SecondClaimId}']").HasAttribute("checked").ShouldBeTrue();
    }

    [Fact]
    public void Name_IsRenderedOnInputs_SoMvcFormCanPostIt()
    {
        using var ctx = CreateContext();

        var cut = ctx.Render<ClaimSelector>(p => p
            .Add(x => x.ProjectId, ProjectId)
            .Add(x => x.Name, "ClaimId"));

        cut.FindAll(".join-select__list input").Select(i => i.GetAttribute("name")).ShouldBe(["ClaimId", "ClaimId"]);
    }

    /// <summary>Перевод взноса: заявку-источник нельзя выбрать получателем.</summary>
    [Fact]
    public void ExceptClaimId_IsNotOffered()
    {
        using var ctx = CreateContext();

        var cut = ctx.Render<ClaimSelector>(p => p
            .Add(x => x.ProjectId, ProjectId)
            .Add(x => x.ExceptClaimId, FirstClaimId));

        cut.FindAll(".join-select__list input").Select(o => o.GetAttribute("value")).ShouldBe([SecondClaimId.ToString()]);
    }

    /// <summary>
    /// В <c>EditForm</c> валидация перерисовывает сообщение об ошибке и состояние кнопки по событию
    /// <c>OnFieldChanged</c>. Без уведомления выбор заявки не гасит ошибку — выглядит как «валидация
    /// не отпускает, хотя заявку выбрал».
    /// </summary>
    [Fact]
    public void Selecting_NotifiesEditContextThatFieldChanged()
    {
        using var ctx = CreateContext();
        ctx.SetRendererInfo(new RendererInfo("WebAssembly", isInteractive: true));
        var model = new FormModel();
        var editContext = new EditContext(model);
        var changedFields = new List<string>();
        editContext.OnFieldChanged += (_, args) => changedFields.Add(args.FieldIdentifier.FieldName);

        var cut = ctx.Render<ClaimSelector>(p => p
            .AddCascadingValue(editContext)
            .Add(x => x.ProjectId, ProjectId)
            .Add(x => x.ClaimIdExpression, () => model.ClaimId)
            .Add(x => x.ClaimIdChanged, id => model.ClaimId = id));

        cut.FindAll(".join-select__list input")[1].Change(true);

        model.ClaimId.ShouldBe(SecondClaimId);
        changedFields.ShouldBe([nameof(FormModel.ClaimId)]);
    }
}
