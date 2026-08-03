using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.ProjectMetadata;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.IntegrationTests.TestInfrastructure;
using JoinRpg.Services.Impl.Projects;
using JoinRpg.Services.Interfaces.Characters;
using JoinRpg.Services.Interfaces.ProjectMetadata;

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
            var rolesListRepository = sp.GetRequiredService<IProjectRolesListRepository>();
            var rolesListService = sp.GetRequiredService<IProjectRolesListService>();
            var characterService = sp.GetRequiredService<ICharacterService>();

            var projectInfo = await metadataRepository.GetProjectMetadata(projectId);
            var nameFieldId = (projectInfo.CharacterNameField
                ?? throw new InvalidOperationException("В проекте нет поля имени персонажа")).Id.ProjectFieldId;
            var descriptionField = (projectInfo.CharacterDescriptionField
                ?? throw new InvalidOperationException("В проекте нет поля описания персонажа")).Id;

            foreach (var autoList in await rolesListRepository.GetForProjectAsync(projectId))
            {
                await rolesListService.RemoveAsync(autoList.ProjectRolesListId!);
            }

            await characterService.AddCharacter(new AddCharacterRequest(
                projectId,
                ParentCharacterGroupIds: [projectInfo.RootCharacterGroupId],
                new CharacterTypeInfo(CharacterType.Player, IsHot: true, SlotLimit: null, SlotName: null, CharacterVisibility.Public),
                FieldValues: new Dictionary<int, string?> { [nameFieldId] = "Горячий герой" }));

            return descriptionField;
        });

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
        await factory.Services.RunAsAsync(userId, async sp =>
        {
            var rolesListRepository = sp.GetRequiredService<IProjectRolesListRepository>();
            var rolesListService = sp.GetRequiredService<IProjectRolesListService>();

            foreach (var autoList in await rolesListRepository.GetForProjectAsync(projectId))
            {
                await rolesListService.RemoveAsync(autoList.ProjectRolesListId!);
            }
        });

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
}
