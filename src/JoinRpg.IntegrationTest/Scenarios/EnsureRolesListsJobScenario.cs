using JoinRpg.Dal.Impl;
using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.ProjectMetadata;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.Services.Impl.Projects;
using JoinRpg.Services.Interfaces.Characters;

namespace JoinRpg.IntegrationTest.Scenarios;

public class EnsureRolesListsJobScenario(JoinApplicationFactory factory) : IClassFixture<JoinApplicationFactory>
{
    [Fact]
    public async Task Job_AddsAllRolesListAndHotRolesList_ToActiveProjectWithHotRole()
    {
        // 1. Larp-проект (в нём уже есть поле «Описание персонажа» и авто-сетки «Все роли»/«Горячие роли»)
        using var scope = factory.Services.CreateScope();
        var userId = await TestUserProjectHelpers.CreateTestUserAsync(scope.ServiceProvider);
        var projectId = await TestUserProjectHelpers.CreateProjectAsync(scope.ServiceProvider, userId);

        // 2. Удаляем авто-сетки и добавляем одну горячую роль — воспроизводим «старый» проект
        var descriptionFieldId = await factory.Services.RunAsAsync(userId, async sp =>
        {
            var metadataRepository = sp.GetRequiredService<IProjectMetadataRepository>();
            var characterService = sp.GetRequiredService<ICharacterService>();

            var projectInfo = await metadataRepository.GetProjectMetadata(projectId);
            var descriptionField = (projectInfo.CharacterDescriptionField
                ?? throw new InvalidOperationException("В проекте нет поля описания персонажа")).Id;

            await characterService.AddCharacter(new AddCharacterRequest(
                projectId,
                ParentCharacterGroupIds: [],
                new CharacterTypeInfo(CharacterType.Player, IsHot: true, SlotLimit: null, SlotName: null, CharacterVisibility.Public),
                FieldValues: FieldLayerContainer.Empty(projectInfo)));

            return descriptionField;
        });
        await RemoveAllRolesLists(projectId);

        // 3. Запускаем джобу
        await factory.Services.RunAsAsync(userId, sp =>
            sp.GetRequiredService<EnsureRolesListsJob>().RunOnce(CancellationToken.None));

        // 4. Джоба создала публичные сетки «Все роли» и «Горячие роли»
        await factory.Services.RunAsAsync(userId, async sp =>
        {
            var rolesListRepository = sp.GetRequiredService<IProjectRolesListRepository>();
            var lists = await rolesListRepository.GetForProjectAsync(projectId);
            lists.Count.ShouldBe(2);

            var allList = lists.Single(l => l.ShowRolesFilter == ShowRolesFilter.All);
            allList.Name.ShouldBe("Все роли");
            allList.PublicMode.ShouldBeTrue();
            allList.CharacterGroupId.ShouldBeNull();
            allList.Fields.ShouldContain(descriptionFieldId);

            var hotList = lists.Single(l => l.ShowRolesFilter == ShowRolesFilter.HotOnly);
            hotList.Name.ShouldBe("Горячие роли");
            hotList.PublicMode.ShouldBeTrue();
            hotList.CharacterGroupId.ShouldBeNull();
            hotList.Fields.ShouldContain(descriptionFieldId);
        });

        // 5. Повторный запуск идемпотентен — новых сеток не появляется
        await factory.Services.RunAsAsync(userId, sp =>
            sp.GetRequiredService<EnsureRolesListsJob>().RunOnce(CancellationToken.None));

        await factory.Services.RunAsAsync(userId, async sp =>
        {
            var rolesListRepository = sp.GetRequiredService<IProjectRolesListRepository>();
            var lists = await rolesListRepository.GetForProjectAsync(projectId);
            lists.Count(l => l.ShowRolesFilter == ShowRolesFilter.All).ShouldBe(1);
            lists.Count(l => l.ShowRolesFilter == ShowRolesFilter.HotOnly).ShouldBe(1);
        });
    }

    [Fact]
    public async Task Job_AddsOnlyAllRolesList_ToActiveProjectWithoutHotRoles()
    {
        // 1. Larp-проект без горячих ролей
        using var scope = factory.Services.CreateScope();
        var userId = await TestUserProjectHelpers.CreateTestUserAsync(scope.ServiceProvider);
        var projectId = await TestUserProjectHelpers.CreateProjectAsync(scope.ServiceProvider, userId);

        // 2. Удаляем авто-сетки — воспроизводим «старый» проект без единой сетки ролей
        await RemoveAllRolesLists(projectId);

        // 3. Запускаем джобу
        await factory.Services.RunAsAsync(userId, sp =>
            sp.GetRequiredService<EnsureRolesListsJob>().RunOnce(CancellationToken.None));

        // 4. Джоба создала только сетку «Все роли» — горячих ролей в проекте нет
        await factory.Services.RunAsAsync(userId, async sp =>
        {
            var rolesListRepository = sp.GetRequiredService<IProjectRolesListRepository>();
            var lists = await rolesListRepository.GetForProjectAsync(projectId);

            var allList = lists.ShouldHaveSingleItem();
            allList.Name.ShouldBe("Все роли");
            allList.ShowRolesFilter.ShouldBe(ShowRolesFilter.All);
            allList.CharacterGroupId.ShouldBeNull();
        });
    }

    /// <summary>
    /// Сетку по умолчанию сервис удалить не даёт, а у «старого» проекта сеток нет совсем,
    /// поэтому авто-сетки сносим прямо в БД.
    /// </summary>
    private async Task RemoveAllRolesLists(ProjectIdentification projectId)
    {
        using var scope = factory.Services.CreateScope();
        var myDb = scope.ServiceProvider.GetRequiredService<MyDbContext>();
        await myDb.Database.ExecuteSqlCommandAsync(
            """
            UPDATE dbo.ProjectDetails SET DefaultProjectRolesListId = NULL WHERE ProjectId = @p0;
            DELETE FROM dbo.ProjectRolesLists WHERE ProjectId = @p0;
            """,
            projectId.Value);
    }
}
