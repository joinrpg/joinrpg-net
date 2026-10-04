using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;

namespace JoinRpg.Web.ProjectCommon.Test;

/// <summary>
/// <see cref="CharacterGroupSelector"/> рендерит <c>&lt;select&gt;</c>, который в MVC-формах постится
/// напрямую в модель (<c>param-Name</c>), поэтому в тестах проверяется именно формат value у опций:
/// полный идентификатор группы, который понимает ProjectEntityIdModelBinder.
/// </summary>
public class CharacterGroupSelectorTest
{
    private const int ProjectId = 1;
    private static readonly CharacterGroupIdentification FirstGroupId = new(ProjectId, 10);
    private static readonly CharacterGroupIdentification SecondGroupId = new(ProjectId, 20);

    private sealed class FakeCharacterGroupsClient : ICharacterGroupsClient
    {
        public int CallCount { get; private set; }

        private Task<List<CharacterGroupDto>> Groups()
        {
            CallCount++;
            return Task.FromResult<List<CharacterGroupDto>>(
            [
                new(FirstGroupId, "Первая", ["Корень", "Первая"], IsPublic: true, IsSpecial: false),
                new(SecondGroupId, "Вторая", ["Корень", "Вторая"], IsPublic: true, IsSpecial: false),
            ]);
        }

        public Task<List<CharacterGroupDto>> GetRealCharacterGroups(int projectId) => Groups();

        public Task<List<CharacterGroupDto>> GetCharacterGroupsWithSpecial(int projectId) => Groups();

        public Task<List<CharacterGroupDto>> GetValidParentGroups(CharacterGroupIdentification groupId) => Groups();
    }

    private static (BunitContext Context, FakeCharacterGroupsClient Client) CreateContext()
    {
        var ctx = new BunitContext();
        var client = new FakeCharacterGroupsClient();
        ctx.Services.AddSingleton<ICharacterGroupsClient>(client);
        // Non-interactive: TypedSelector не пытается импортировать JS-модуль bootstrap-select.
        ctx.SetRendererInfo(new RendererInfo("Static", isInteractive: false));
        return (ctx, client);
    }

    /// <summary>
    /// Контракт с ProjectEntityIdModelBinder: он разбирает пришедшее из формы значение через
    /// TryParse, поэтому value у опции должно им и разбираться — обратно в тот же id.
    /// </summary>
    [Fact]
    public void OptionValues_AreParsedBackIntoSameIds()
    {
        var (ctx, _) = CreateContext();
        using var _unused = ctx;

        var cut = ctx.Render<CharacterGroupSelector>(p => p.Add(x => x.ProjectId, ProjectId));

        var values = cut.FindAll("option").Select(o => o.GetAttribute("value")).ToArray();
        values.Length.ShouldBe(2);

        var parsed = values.Select(v =>
        {
            CharacterGroupIdentification.TryParse(v, null, out var id).ShouldBeTrue($"Не разобрался id: '{v}'");
            return id;
        });
        parsed.ShouldBe([FirstGroupId, SecondGroupId]);
    }

    [Fact]
    public void ChangingIncludeSpecial_RequestsGroupsAgain()
    {
        var (ctx, client) = CreateContext();
        using var _unused = ctx;

        var cut = ctx.Render<CharacterGroupSelector>(p => p
            .Add(x => x.ProjectId, ProjectId)
            .Add(x => x.IncludeSpecial, true));

        cut.Render(p => p
            .Add(x => x.ProjectId, ProjectId)
            .Add(x => x.IncludeSpecial, false));

        client.CallCount.ShouldBe(2);
    }

    [Fact]
    public void SelectedGroupIds_MarkOptionSelected()
    {
        var (ctx, _) = CreateContext();
        using var _unused = ctx;

        var cut = ctx.Render<CharacterGroupSelector>(p => p
            .Add(x => x.ProjectId, ProjectId)
            .Add(x => x.SelectedGroupIds, [SecondGroupId]));

        cut.Find($"option[value='{SecondGroupId}']").HasAttribute("selected").ShouldBeTrue();
        cut.Find($"option[value='{FirstGroupId}']").HasAttribute("selected").ShouldBeFalse();
    }

    [Fact]
    public void Name_IsRenderedOnSelect_SoMvcFormCanPostIt()
    {
        var (ctx, _) = CreateContext();
        using var _unused = ctx;

        var cut = ctx.Render<CharacterGroupSelector>(p => p
            .Add(x => x.ProjectId, ProjectId)
            .Add(x => x.Name, "ParentCharacterGroupIds"));

        cut.Find("select").GetAttribute("name").ShouldBe("ParentCharacterGroupIds");
    }

    /// <summary>
    /// Интерактивный режим: выбор в <c>&lt;select&gt;</c> уходит наружу через колбэки обёртки. От
    /// <see cref="CharacterGroupSelector.SelectedGroupsChanged"/> зависит цепочка
    /// SingleCharacterGroupSelector → формы капитанов/подписок/списков ролей, поэтому проверяются оба.
    /// </summary>
    [Fact]
    public void Selecting_RaisesBothIdAndDtoCallbacks()
    {
        var (ctx, _) = CreateContext();
        using var _unused = ctx;
        ctx.SetRendererInfo(new RendererInfo("WebAssembly", isInteractive: true));
        SetupBootstrapSelectInterop(ctx, [SecondGroupId]);

        CharacterGroupIdentification[]? changedIds = null;
        CharacterGroupDto[]? changedGroups = null;

        var cut = ctx.Render<CharacterGroupSelector>(p => p
            .Add(x => x.ProjectId, ProjectId)
            .Add(x => x.SelectedGroupIdsChanged, ids => changedIds = ids)
            .Add(x => x.SelectedGroupsChanged, groups => changedGroups = groups));

        cut.Find("select").Change(SecondGroupId.ToString());

        changedIds.ShouldBe([SecondGroupId]);
        changedGroups.ShouldNotBeNull().Select(g => g.Name).ShouldBe(["Вторая"]);
    }

    private sealed class FormModel
    {
        public CharacterGroupIdentification[]? GroupIds { get; set; }
    }

    /// <summary>
    /// В <c>EditForm</c> валидация перерисовывает сообщение об ошибке по событию <c>OnFieldChanged</c>.
    /// Без уведомления выбор группы не гасит ошибку до отправки формы — выглядит как «валидация не
    /// отпускает, хотя группу выбрал».
    /// </summary>
    [Fact]
    public void Selecting_NotifiesEditContextThatFieldChanged()
    {
        var (ctx, _) = CreateContext();
        using var _unused = ctx;
        ctx.SetRendererInfo(new RendererInfo("WebAssembly", isInteractive: true));
        SetupBootstrapSelectInterop(ctx, [SecondGroupId]);

        var model = new FormModel();
        var editContext = new EditContext(model);
        var changedFields = new List<string>();
        editContext.OnFieldChanged += (_, args) => changedFields.Add(args.FieldIdentifier.FieldName);

        var cut = ctx.Render<CharacterGroupSelector>(p => p
            .AddCascadingValue(editContext)
            .Add(x => x.ProjectId, ProjectId)
            .Add(x => x.SelectedGroupIdsExpression, () => model.GroupIds));

        cut.Find("select").Change(SecondGroupId.ToString());

        changedFields.ShouldBe([nameof(FormModel.GroupIds)]);
    }

    private static void SetupBootstrapSelectInterop(BunitContext ctx, CharacterGroupIdentification[] selected)
    {
        var module = ctx.JSInterop.SetupModule("/_content/JoinRpg.Common.WebComponents/component-interop.js");
        module.SetupVoid("initBootstrapSelect", _ => true).SetVoidResult();
        module.SetupVoid("refreshBootstrapSelect", _ => true).SetVoidResult();
        module.Setup<List<string>>("getSelectedValues", _ => true)
            .SetResult([.. selected.Select(x => x.ToString())]);
    }

    [Fact]
    public void RerenderWithSameParameters_DoesNotRequestGroupsAgain()
    {
        var (ctx, client) = CreateContext();
        using var _unused = ctx;

        var cut = ctx.Render<CharacterGroupSelector>(p => p.Add(x => x.ProjectId, ProjectId));
        client.CallCount.ShouldBe(1);

        cut.Render(p => p.Add(x => x.ProjectId, ProjectId));

        client.CallCount.ShouldBe(1);
    }
}
