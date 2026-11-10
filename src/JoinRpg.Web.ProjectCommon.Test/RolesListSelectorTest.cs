using JoinRpg.Common.WebComponents;
using JoinRpg.DomainTypes.ProjectMetadata;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;

namespace JoinRpg.Web.ProjectCommon.Test;

public class RolesListSelectorTest
{
    private static readonly ProjectIdentification ProjectId = new(1);
    private static readonly ProjectRolesListIdentification SecondListId = new(ProjectId, 20);

    private sealed class FormModel
    {
        public ProjectRolesListIdentification[]? SelectedIds { get; set; }
    }

    private sealed class FakeSelectorClient : IRolesListSelectorClient
    {
        public int Calls { get; private set; }

        public Task<ProjectRolesListDto[]> GetRolesListsList(ProjectIdentification projectId)
        {
            Calls++;
            return Task.FromResult<ProjectRolesListDto[]>(
            [
                new ProjectRolesListDto(new ProjectRolesListIdentification(projectId, 10), "Все роли"),
                new ProjectRolesListDto(new ProjectRolesListIdentification(projectId, 20), "Горячие роли"),
            ]);
        }
    }

    private static (BunitContext Context, FakeSelectorClient Client) CreateContext()
    {
        var ctx = new BunitContext();
        var client = new FakeSelectorClient();
        ctx.Services.AddSingleton<IRolesListSelectorClient>(client);
        _ = ctx.Services.AddJoinRpgWebComponents();
        _ = ctx.AddBunitPersistentComponentState();
        // Non-interactive: так TypedSelector рендерится в MVC-формах (render-mode="Static").
        ctx.SetRendererInfo(new RendererInfo("Static", isInteractive: false));
        return (ctx, client);
    }

    [Fact]
    public void RendersSavedRolesLists()
    {
        var (ctx, client) = CreateContext();
        using var _ = ctx;

        var cut = ctx.Render<RolesListSelector>(p => p.Add(x => x.ProjectId, ProjectId));

        cut.Markup.ShouldContain("Все роли");
        cut.Markup.ShouldContain("Горячие роли");
        client.Calls.ShouldBe(1);
    }

    /// <summary>
    /// В <c>EditForm</c> валидация перерисовывает сообщение об ошибке и состояние кнопки по событию
    /// <c>OnFieldChanged</c>. Без уведомления выбор сетки ролей не гасит ошибку — выглядит как
    /// «валидация не отпускает, хотя сетку выбрал».
    /// </summary>
    [Fact]
    public void Selecting_NotifiesEditContextThatFieldChanged()
    {
        var (ctx, _) = CreateContext();
        using var _unused = ctx;
        ctx.SetRendererInfo(new RendererInfo("WebAssembly", isInteractive: true));
        var model = new FormModel();
        var editContext = new EditContext(model);
        var changedFields = new List<string>();
        editContext.OnFieldChanged += (_, args) => changedFields.Add(args.FieldIdentifier.FieldName);

        var cut = ctx.Render<RolesListSelector>(p => p
            .AddCascadingValue(editContext)
            .Add(x => x.ProjectId, ProjectId)
            .Add(x => x.SelectedIdsExpression, () => model.SelectedIds)
            .Add(x => x.SelectedIdsChanged, ids => model.SelectedIds = ids));

        cut.FindAll(".join-select__list input")[1].Change(true);

        model.SelectedIds.ShouldBe([SecondListId]);
        changedFields.ShouldBe([nameof(FormModel.SelectedIds)]);
    }
}
