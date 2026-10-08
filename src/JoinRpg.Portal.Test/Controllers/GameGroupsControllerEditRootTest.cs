using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel.Mocks;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Portal.Controllers;
using JoinRpg.Web.Models.CharacterGroups;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Test.Controllers;

/// <summary>
/// Настройка корневой группы (<see cref="GameGroupsController.Edit"/>) — страницы для неё нет.
/// </summary>
/// <remarks>
/// Раньше оба метода отвечали <c>RedirectToActionPermanent("Index")</c> без route values. Ambient
/// <c>projectId</c> в такую ссылку не подставляется (выигрывает конвенциональный маршрут
/// <c>{controller}/{action}</c>), поэтому получался постоянный — то есть кешируемый браузером —
/// редирект на <c>/gamegroups</c>, который сам отдаёт 404.
/// </remarks>
public class GameGroupsControllerEditRootTest
{
    [Fact]
    public async Task EditGetOnRootGroup_IsNotFound()
    {
        var mock = new MockedProject();
        var rootGroupId = mock.ProjectInfo.GroupTree.RootGroupId;

        var result = await CreateController(mock)
            .Edit(rootGroupId.ProjectId.Value, rootGroupId.CharacterGroupId);

        result.ShouldBeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task EditPostOnRootGroup_IsNotFound()
    {
        var mock = new MockedProject();
        var rootGroupId = mock.ProjectInfo.GroupTree.RootGroupId;

        var result = await CreateController(mock).Edit(new EditCharacterGroupViewModel
        {
            ProjectId = rootGroupId.ProjectId.Value,
            CharacterGroupId = rootGroupId.CharacterGroupId,
            Name = "Все роли",
        });

        result.ShouldBeOfType<NotFoundResult>();
    }

    private static GameGroupsController CreateController(MockedProject mock)
        // До проверки на корневую группу контроллер ходит только в charGroupRepository.
        => new(
            characterGroupService: null!,
            new FakeProjectMetadataRepository(mock),
            new FakeCurrentUserAccessor(mock.Master.UserId),
            new FakeCharacterGroupRepository(mock));

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
