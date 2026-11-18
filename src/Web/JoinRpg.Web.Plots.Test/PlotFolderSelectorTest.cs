using JoinRpg.Common.WebComponents;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Plots;
using JoinRpg.Web.Plots.Elements;
using JoinRpg.Web.Plots.Folders;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;

namespace JoinRpg.Web.Plots.Test;

/// <summary>
/// Форма валидирует выбранный сюжет через <see cref="EditContext"/>: пока селектор не уведомил его
/// об изменении поля, сообщение валидации не гаснет, а кнопка отправки остаётся выключенной —
/// для пользователя это «выбрал сюжет, а форма всё равно ругается».
/// </summary>
public class PlotFolderSelectorTest
{
    private static readonly ProjectIdentification ProjectId = new(1);

    private static readonly PlotFolderDto FirstFolder =
        new(new PlotFolderIdentification(ProjectId, 10), "Первый сюжет");

    private static readonly PlotFolderDto SecondFolder =
        new(new PlotFolderIdentification(ProjectId, 20), "Второй сюжет");

    private sealed class FormModel
    {
        public PlotFolderIdentification[]? SelectedIds { get; set; }
    }

    private sealed class FakePlotClient : IPlotClient
    {
        public Task<PlotFolderDto[]> GetPlotFoldersList(ProjectIdentification projectId)
            => Task.FromResult<PlotFolderDto[]>(projectId == ProjectId ? [FirstFolder, SecondFolder] : []);

        public Task UnPublishVersion(PlotVersionIdentification version) => throw new NotSupportedException();
        public Task PublishVersion(PublishPlotElementViewModel model) => throw new NotSupportedException();
        public Task DeleteElement(PlotElementIdentification elementId) => throw new NotSupportedException();
        public Task UnDeleteElement(PlotElementIdentification elementId) => throw new NotSupportedException();
    }

    private static BunitContext CreateContext()
    {
        var ctx = new BunitContext();
        ctx.Services.AddSingleton<IPlotClient>(new FakePlotClient());
        _ = ctx.Services.AddJoinRpgWebComponents();
        _ = ctx.AddBunitPersistentComponentState();
        // Интерактивный режим: выбор пункта уходит наружу через колбэки компонента.
        ctx.SetRendererInfo(new RendererInfo("WebAssembly", isInteractive: true));
        return ctx;
    }

    /// <summary>
    /// В <c>EditForm</c> валидация перерисовывает сообщение об ошибке и состояние кнопки по событию
    /// <c>OnFieldChanged</c>. Без уведомления выбор сюжета не гасит ошибку — выглядит как «валидация
    /// не отпускает, хотя сюжет выбран».
    /// </summary>
    [Fact]
    public void Selecting_NotifiesEditContextThatFieldChanged()
    {
        var ctx = CreateContext();
        using var _unused = ctx;
        var model = new FormModel();
        var editContext = new EditContext(model);
        var changedFields = new List<string>();
        editContext.OnFieldChanged += (_, args) => changedFields.Add(args.FieldIdentifier.FieldName);

        var cut = ctx.Render<PlotFolderSelector>(p => p
            .AddCascadingValue(editContext)
            .Add(x => x.ProjectId, ProjectId)
            .Add(x => x.SelectedIdsExpression, () => model.SelectedIds)
            .Add(x => x.SelectedIdsChanged, ids => model.SelectedIds = ids));

        cut.FindAll(".join-select__list input")[1].Change(true);

        model.SelectedIds.ShouldBe([SecondFolder.PlotFolderId]);
        changedFields.ShouldBe([nameof(FormModel.SelectedIds)]);
    }
}
