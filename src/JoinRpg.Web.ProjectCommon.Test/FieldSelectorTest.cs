using JoinRpg.Common.WebComponents;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Web.ProjectCommon.Fields;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;

namespace JoinRpg.Web.ProjectCommon.Test;

/// <summary>
/// Контракт <see cref="FieldSelector"/>: с заданным выражением поля модели об изменении выбора узнаёт
/// <see cref="EditContext"/> — иначе валидация формы не пересчитывается, и сообщение об ошибке не гаснет.
/// </summary>
public class FieldSelectorTest
{
    private static readonly ProjectIdentification ProjectId = new(1);
    private static readonly ProjectFieldIdentification CharacterStringId = new(ProjectId, 10);

    private sealed class StubFieldsClient : IProjectFieldsClient
    {
        public Task<List<ProjectFieldDto>> GetProjectFields(ProjectIdentification projectId)
            => Task.FromResult<List<ProjectFieldDto>>(
            [
                new ProjectFieldDto(new(projectId, 10), "Строка персонажа", ProjectFieldType.String, FieldBoundTo.Character),
                new ProjectFieldDto(new(projectId, 20), "Текст персонажа", ProjectFieldType.Text, FieldBoundTo.Character),
                new ProjectFieldDto(new(projectId, 30), "Строка заявки", ProjectFieldType.String, FieldBoundTo.Claim),
            ]);
    }

    private static BunitContext CreateContext()
    {
        var ctx = new BunitContext();
        ctx.Services.AddSingleton<IProjectFieldsClient>(new StubFieldsClient());
        _ = ctx.Services.AddJoinRpgWebComponents();
        _ = ctx.AddBunitPersistentComponentState();
        // Интерактивный режим: выбор пункта уходит наружу через колбэки компонента.
        ctx.SetRendererInfo(new RendererInfo("WebAssembly", isInteractive: true));
        return ctx;
    }

    private sealed class FormModel
    {
        public ProjectFieldIdentification[]? FieldIds { get; set; }
    }

    /// <summary>
    /// В <c>EditForm</c> валидация перерисовывает сообщение об ошибке по событию <c>OnFieldChanged</c>.
    /// Без уведомления выбор поля не гасит ошибку до отправки формы — выглядит как «валидация не
    /// отпускает, хотя поле выбрал».
    /// </summary>
    [Fact]
    public void Selecting_NotifiesEditContextThatFieldChanged()
    {
        using var ctx = CreateContext();
        var model = new FormModel();
        var editContext = new EditContext(model);
        var changedFields = new List<string>();
        editContext.OnFieldChanged += (_, args) => changedFields.Add(args.FieldIdentifier.FieldName);

        var cut = ctx.Render<FieldSelector>(p => p
            .AddCascadingValue(editContext)
            .Add(x => x.ProjectId, ProjectId)
            .Add(x => x.Multiple, false)
            .Add(x => x.SelectedFieldIdsExpression, () => model.FieldIds)
            .Add(x => x.SelectedFieldIdsChanged, ids => model.FieldIds = ids));

        cut.FindAll(".join-select__list input")[0].Change(true);

        model.FieldIds.ShouldBe([CharacterStringId]);
        changedFields.ShouldBe([nameof(FormModel.FieldIds)]);
    }
}
