using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.ProjectMetadata;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.IntegrationTests.TestInfrastructure;
using JoinRpg.Services.Impl.Projects;
using JoinRpg.Services.Interfaces.Characters;
using JoinRpg.Services.Interfaces.ProjectMetadata;

namespace JoinRpg.IntegrationTest.Scenarios;

public class AddHotRolesListJobScenario(JoinApplicationFactory factory) : IClassFixture<JoinApplicationFactory>
{
    [Fact]
    public async Task Job_AddsPublicHotRolesList_ToActiveProjectWithHotRole()
    {
        // 1. Larp-проект (в нём уже есть поле «Описание персонажа» и авто-сетка «Горячие роли»)
        using var scope = factory.Services.CreateScope();
        var userId = await TestUserProjectHelpers.CreateTestUserAsync(scope.ServiceProvider);
        var projectId = await TestUserProjectHelpers.CreateProjectAsync(scope.ServiceProvider, userId);

        // 2. Удаляем авто-сетку и добавляем одну горячую роль — воспроизводим «старый» проект
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
                await rolesListService.RemoveAsync(autoList.ProjectRolesListId);
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
            sp.GetRequiredService<AddHotRolesListJob>().RunOnce(CancellationToken.None));

        // 4. Джоба создала публичную сетку «Горячие роли» с полем описания
        await factory.Services.RunAsAsync(userId, async sp =>
        {
            var rolesListRepository = sp.GetRequiredService<IProjectRolesListRepository>();
            var lists = await rolesListRepository.GetForProjectAsync(projectId);

            var hotList = lists.ShouldHaveSingleItem();
            hotList.Name.ShouldBe("Горячие роли");
            hotList.PublicMode.ShouldBeTrue();
            hotList.CharacterGroupId.ShouldBeNull();
            hotList.ShowRolesFilter.ShouldBe(ShowRolesFilter.HotOnly);
            hotList.Fields.ShouldContain(descriptionFieldId);
        });

        // 5. Повторный запуск идемпотентен — второй сетки не появляется
        await factory.Services.RunAsAsync(userId, sp =>
            sp.GetRequiredService<AddHotRolesListJob>().RunOnce(CancellationToken.None));

        await factory.Services.RunAsAsync(userId, async sp =>
        {
            var rolesListRepository = sp.GetRequiredService<IProjectRolesListRepository>();
            var lists = await rolesListRepository.GetForProjectAsync(projectId);
            lists.Count(l => l.ShowRolesFilter == ShowRolesFilter.HotOnly).ShouldBe(1);
        });
    }
}
