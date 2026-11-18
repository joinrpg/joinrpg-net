using JoinRpg.Common.WebComponents;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;

namespace JoinRpg.Web.ProjectCommon.Test;

/// <summary>
/// Контракт обёртки <see cref="SingleCharacterGroupSelector"/>: одиночный выбор уходит наружу через
/// <see cref="SingleCharacterGroupSelector.SelectedGroupIdChanged"/>, а с заданным выражением поля модели
/// об изменении узнаёт <see cref="EditContext"/> — иначе валидация формы не пересчитывается.
/// </summary>
public class SingleCharacterGroupSelectorTest
{
    private const int ProjectId = 1;
    private static readonly CharacterGroupIdentification RootGroupId = new(ProjectId, 1);
    private static readonly CharacterGroupIdentification FirstGroupId = new(ProjectId, 10);
    private static readonly CharacterGroupIdentification SecondGroupId = new(ProjectId, 20);

    private sealed class StubCharacterGroupsClient : ICharacterGroupsClient
    {
        private Task<List<CharacterGroupDto>> Groups()
            => Task.FromResult<List<CharacterGroupDto>>(
            [
                new(RootGroupId, "Все роли", [], IsPublic: true, IsSpecial: false, IsRoot: true),
                new(FirstGroupId, "Первая", [], IsPublic: true, IsSpecial: false),
                new(SecondGroupId, "Вторая", [], IsPublic: true, IsSpecial: false),
            ]);

        public Task<List<CharacterGroupDto>> GetRealCharacterGroups(int projectId) => Groups();

        public Task<List<CharacterGroupDto>> GetCharacterGroupsWithSpecial(int projectId) => Groups();

        public Task<List<CharacterGroupDto>> GetValidParentGroups(CharacterGroupIdentification groupId) => Groups();
    }

    private static BunitContext CreateContext()
    {
        var ctx = new BunitContext();
        ctx.Services.AddSingleton<ICharacterGroupsClient>(new StubCharacterGroupsClient());
        _ = ctx.Services.AddJoinRpgWebComponents();
        _ = ctx.AddBunitPersistentComponentState();
        // Интерактивный режим: выбор пункта уходит наружу через колбэки компонента.
        ctx.SetRendererInfo(new RendererInfo("WebAssembly", isInteractive: true));
        return ctx;
    }

    private sealed class FormModel
    {
        public CharacterGroupIdentification? GroupId { get; set; }
    }

    /// <summary>
    /// В <c>EditForm</c> валидация перерисовывает сообщение об ошибке по событию <c>OnFieldChanged</c>.
    /// Без уведомления выбор группы не гасит ошибку до отправки формы — выглядит как «валидация не
    /// отпускает, хотя группу выбрал».
    /// </summary>
    [Fact]
    public void Selecting_NotifiesEditContextThatFieldChanged()
    {
        using var ctx = CreateContext();
        var model = new FormModel();
        var editContext = new EditContext(model);
        var changedFields = new List<string>();
        editContext.OnFieldChanged += (_, args) => changedFields.Add(args.FieldIdentifier.FieldName);

        var cut = ctx.Render<SingleCharacterGroupSelector>(p => p
            .AddCascadingValue(editContext)
            .Add(x => x.ProjectId, ProjectId)
            .Add(x => x.SelectedGroupIdExpression, () => model.GroupId)
            .Add(x => x.SelectedGroupIdChanged, (CharacterGroupIdentification? id) => model.GroupId = id));

        cut.Find($"input[value='{SecondGroupId}']").Change(true);

        model.GroupId.ShouldBe(SecondGroupId);
        changedFields.ShouldBe([nameof(FormModel.GroupId)]);
    }
}
