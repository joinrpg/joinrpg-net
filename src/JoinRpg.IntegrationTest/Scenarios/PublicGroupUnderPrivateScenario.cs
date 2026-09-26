using System.Data.Entity;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Dal.Impl;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.DomainTypes;
using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.IntegrationTests.TestInfrastructure;
using JoinRpg.Services.Interfaces.Projects;

namespace JoinRpg.IntegrationTest.Scenarios;

/// <summary>
/// Запрос-кандидат для разовой починки #4878: проекты, где есть публичная группа с непубличным
/// прямым родителем. Смысл интеграционного теста — сам SQL: родители лежат строкой с запятыми,
/// и ребро дерева ищется через <c>CharIndex</c>, а не join-ом.
/// </summary>
public class PublicGroupUnderPrivateScenario(JoinApplicationFactory factory) : IClassFixture<JoinApplicationFactory>
{
    [Fact]
    public async Task ProjectWithPublicGroupUnderPrivateIsFound()
    {
        var (masterId, projectId, _, parentGroupId) = await CreateTwoLevelPublicTree();

        // Состояние «до»: непубличный родитель с публичным потомком. Через сервисы его уже не
        // создать — и создание, и редактирование группы такое запрещают, — поэтому пишем в БД.
        await MakeGroupPrivate(projectId, parentGroupId);

        await factory.Services.RunAsAsync(masterId, async sp =>
        {
            var candidates = await sp.GetRequiredService<ICharacterGroupRepository>()
                .GetProjectsWithPublicGroupUnderPrivateParent();

            candidates.ShouldContain(projectId);
        });
    }

    /// <summary>
    /// Сама починка: группа без публичного пути наверх становится непубличной в БД.
    /// </summary>
    [Fact]
    public async Task FixerHidesGroupWithoutPublicPath()
    {
        var (masterId, projectId, childGroupId, parentGroupId) = await CreateTwoLevelPublicTree();
        await MakeGroupPrivate(projectId, parentGroupId);

        await factory.Services.RunAsAsync(masterId, async sp =>
        {
            var hidden = await sp.GetRequiredService<IPublicGroupVisibilityFixer>()
                .HideGroupsWithoutPublicPath(projectId);

            hidden.ShouldBe(["Потомок"]);
        });

        // Отдельный скоуп: проверяем, что изменение доехало до БД, а не осталось в кеше метаданных.
        using var scope = factory.Services.CreateScope();
        var group = await scope.ServiceProvider.GetRequiredService<MyDbContext>().Set<CharacterGroup>()
            .SingleAsync(g => g.CharacterGroupId == childGroupId.CharacterGroupId);

        group.IsPublic.ShouldBeFalse();
    }

    [Fact]
    public async Task CleanProjectIsNotFound()
    {
        var (masterId, projectId, _, _) = await CreateTwoLevelPublicTree();

        await factory.Services.RunAsAsync(masterId, async sp =>
        {
            var candidates = await sp.GetRequiredService<ICharacterGroupRepository>()
                .GetProjectsWithPublicGroupUnderPrivateParent();

            candidates.ShouldNotContain(projectId);
        });
    }

    /// <summary>
    /// Проект с двумя публичными группами: родитель под корнем и потомок под родителем.
    /// </summary>
    private async Task<(UserIdentification MasterId, ProjectIdentification ProjectId,
        CharacterGroupIdentification ChildGroupId, CharacterGroupIdentification ParentGroupId)> CreateTwoLevelPublicTree()
    {
        UserIdentification masterId;
        ProjectIdentification projectId;
        using (var scope = factory.Services.CreateScope())
        {
            masterId = await TestUserProjectHelpers.CreateTestUserAsync(scope.ServiceProvider);
            projectId = await TestUserProjectHelpers.CreateProjectAsync(
                scope.ServiceProvider, masterId, "Проект с публичной группой под непубличной");
        }

        CharacterGroupIdentification parentGroupId = default!;
        CharacterGroupIdentification childGroupId = default!;
        await factory.Services.RunAsAsync(masterId, async sp =>
        {
            var groupService = sp.GetRequiredService<ICharacterGroupService>();
            var rootGroupId = (await sp.GetRequiredService<IProjectMetadataRepository>()
                .GetProjectMetadata(projectId)).GroupTree.RootGroupId;

            parentGroupId = await groupService.AddCharacterGroup(
                projectId, "Родитель", isPublic: true, [rootGroupId], "");
        });

        await factory.Services.RunAsAsync(masterId, async sp =>
        {
            childGroupId = await sp.GetRequiredService<ICharacterGroupService>().AddCharacterGroup(
                projectId, "Потомок", isPublic: true, [parentGroupId], "");
        });

        return (masterId, projectId, childGroupId, parentGroupId);
    }

    private async Task MakeGroupPrivate(ProjectIdentification projectId, CharacterGroupIdentification groupId)
    {
        using var scope = factory.Services.CreateScope();
        var myDb = scope.ServiceProvider.GetRequiredService<MyDbContext>();

        // Project с группами нужен из-за валидации сущности на SaveChanges: CharacterGroup.Validate
        // ходит в ParentGroups, а те ищутся в Project.CharacterGroups.
        var group = await myDb.Set<CharacterGroup>()
            .Include(g => g.Project.CharacterGroups)
            .SingleAsync(g => g.ProjectId == projectId.Value && g.CharacterGroupId == groupId.CharacterGroupId);
        group.IsPublic = false;

        _ = await myDb.SaveChangesAsync();
    }
}
