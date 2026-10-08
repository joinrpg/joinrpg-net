using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel.Mocks;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Portal.Controllers;
using JoinRpg.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Test.Controllers;

/// <summary>
/// Что показывает <c>/{projectId}/roles</c> (<see cref="GameGroupsController.Index"/>).
/// </summary>
/// <remarks>
/// Адрес <c>/{projectId}/roles</c> канонический: на нём показывается сетка ролей по умолчанию
/// (тот же остров, что и на <c>/{projectId}/roleslist/{id}</c>) — без редиректа, чтобы ссылки на
/// <c>/roles</c> не меняли адрес. Классическая (транзиентная) сетка остаётся только для проектов
/// без сохранённых сеток ролей и для <c>/roles/{characterGroupId}</c>.
/// </remarks>
public class GameGroupsControllerIndexTest
{
    [Fact]
    public async Task WithDefaultRolesList_ShowsItInPlace()
    {
        var mock = new MockedProject();
        var rolesListId = AddDefaultRolesList(mock, "Все роли");

        var result = await CreateController(mock).Index(mock.ProjectInfo.ProjectId, characterGroupId: null);

        var model = result.ShouldBeOfType<ViewResult>().Model.ShouldBeOfType<GameRolesViewModel>();
        model.RolesListId.ShouldBe(rolesListId);
        // Страница — ровно как /roleslist/{id}: ни обрамления группы, ни её имени в заголовке.
        model.Details.ShouldBeNull();
        model.RootGroupName.ShouldBeNull();
    }

    /// <summary>
    /// Редирект на <c>/roleslist/{id}</c> был заменён показом сетки на месте: адрес <c>/roles</c>
    /// должен остаться.
    /// </summary>
    [Fact]
    public async Task WithDefaultRolesList_DoesNotRedirect()
    {
        var mock = new MockedProject();
        _ = AddDefaultRolesList(mock, "Все роли");

        var result = await CreateController(mock).Index(mock.ProjectInfo.ProjectId, characterGroupId: null);

        result.ShouldBeOfType<ViewResult>();
    }

    /// <summary>
    /// У проекта без сохранённых сеток ролей (созданного до появления этой настройки) показывать
    /// на <c>/roles</c> нечего, кроме классической сетки.
    /// </summary>
    [Fact]
    public async Task WithoutRolesLists_ShowsClassicGrid()
    {
        var mock = new MockedProject();

        var result = await CreateController(mock).Index(mock.ProjectInfo.ProjectId, characterGroupId: null);

        var model = result.ShouldBeOfType<ViewResult>().Model.ShouldBeOfType<GameRolesViewModel>();
        model.RolesListId.ShouldBeNull();
        model.GridGroupId.ShouldBeNull();
        model.Details.ShouldNotBeNull();
    }

    /// <summary>
    /// Ссылка с явной группой — по-прежнему классическая сетка от этой группы, даже если у проекта
    /// есть сетка по умолчанию.
    /// </summary>
    [Fact]
    public async Task WithExplicitGroup_ShowsClassicGrid()
    {
        var mock = new MockedProject();
        _ = AddDefaultRolesList(mock, "Все роли");
        var groupId = new CharacterGroupIdentification(mock.ProjectInfo.ProjectId, mock.Group.CharacterGroupId);

        var result = await CreateController(mock).Index(mock.ProjectInfo.ProjectId, groupId.CharacterGroupId);

        var model = result.ShouldBeOfType<ViewResult>().Model.ShouldBeOfType<GameRolesViewModel>();
        model.RolesListId.ShouldBeNull();
        model.GridGroupId.ShouldBe(groupId);
    }

    private static GameGroupsController CreateController(MockedProject mock)
        // Index не мутирует проект, поэтому ICharacterGroupService ему не нужен.
        => new(
            characterGroupService: null!,
            new FakeProjectMetadataRepository(mock),
            new FakeCurrentUserAccessor(mock.Master.UserId),
            new FakeCharacterGroupRepository(mock));

    /// <summary>Добавляет сохранённую сетку ролей в мок и делает её сеткой по умолчанию.</summary>
    private static ProjectRolesListIdentification AddDefaultRolesList(MockedProject mock, string name)
    {
        var id = mock.Project.ProjectRolesLists.Count == 0
            ? 1
            : mock.Project.ProjectRolesLists.Max(x => x.ProjectRolesListId) + 1;
        mock.Project.ProjectRolesLists.Add(new DataModel.ProjectRolesList
        {
            ProjectRolesListId = id,
            ProjectId = mock.Project.ProjectId,
            Project = mock.Project,
            Name = name,
            PublicMode = true,
            GroupsViewMode = RolesGridGroupsViewMode.Tree,
            ShowRolesFilter = ShowRolesFilter.All,
            FieldsImpl = new DataModel.IntList(),
        });
        mock.Project.Details.DefaultProjectRolesListId = id;
        mock.ReInitProjectInfo();
        return new ProjectRolesListIdentification(mock.ProjectInfo.ProjectId, id);
    }

    private sealed class FakeCharacterGroupRepository(MockedProject mock) : ICharacterGroupRepository
    {
        public Task<CharacterGroupFullInfo?> GetCharacterGroupFullInfo(CharacterGroupIdentification id)
            => Task.FromResult<CharacterGroupFullInfo?>(
                Build(mock.ProjectInfo.GroupTree.AllGroups.Single(g => g.Id == id)));

        public Task<IReadOnlyList<CharacterGroupFullInfo>> GetCharacterGroupsFullInfo(
            IReadOnlyCollection<CharacterGroupIdentification> groupIds)
            => Task.FromResult<IReadOnlyList<CharacterGroupFullInfo>>(
                [.. mock.ProjectInfo.GroupTree.AllGroups.Where(g => groupIds.Contains(g.Id)).Select(Build)]);

        /// <summary>Нужен только разовой джобе починки данных, в этих тестах не участвует.</summary>
        public Task<IReadOnlyCollection<ProjectIdentification>> GetProjectsWithPublicGroupWithoutPublicParent()
            => throw new NotSupportedException();

        private static CharacterGroupFullInfo Build(CharacterGroupInfo group)
            => new(
                group,
                directChildCharactersCount: 0,
                description: new MarkdownString("Описание группы"),
                marks: new CreateUpdateMarksInfo(DateTime.UnixEpoch, null, DateTime.UnixEpoch, null));
    }
}
