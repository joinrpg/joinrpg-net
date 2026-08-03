using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.ProjectMetadata;
using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.IntegrationTests.TestInfrastructure;

namespace JoinRpg.IntegrationTest.Scenarios;

public class LarpDefaultRolesListScenario(JoinApplicationFactory factory) : IClassFixture<JoinApplicationFactory>
{
    [Fact]
    public async Task CreateLarpProject_AutoCreatesPublicHotAndAllRolesLists()
    {
        // 1. Пользователь и Larp-проект (CreateProjectAsync по умолчанию создаёт Larp)
        using var scope = factory.Services.CreateScope();
        var userId = await TestUserProjectHelpers.CreateTestUserAsync(scope.ServiceProvider);
        var projectId = await TestUserProjectHelpers.CreateProjectAsync(scope.ServiceProvider, userId);

        // 2. Проверяем, что автоматически созданы публичные сетки «Все роли», «Горячие роли» и «Вакантные роли»
        await factory.Services.RunAsAsync(userId, async sp =>
        {
            var rolesListRepository = sp.GetRequiredService<IProjectRolesListRepository>();
            var metadataRepository = sp.GetRequiredService<IProjectMetadataRepository>();

            var projectInfo = await metadataRepository.GetProjectMetadata(projectId);
            var descriptionFieldId = (projectInfo.CharacterDescriptionField
                ?? throw new InvalidOperationException("В проекте нет поля описания персонажа"))
                .Id;

            var rolesLists = await rolesListRepository.GetForProjectAsync(projectId);
            rolesLists.Count.ShouldBe(3);

            var allList = rolesLists.Single(l => l.ShowRolesFilter == ShowRolesFilter.All);
            allList.Name.ShouldBe("Все роли");
            allList.PublicMode.ShouldBeTrue();
            allList.CharacterGroupId.ShouldBeNull();
            allList.Fields.ShouldContain(descriptionFieldId);

            var hotList = rolesLists.Single(l => l.ShowRolesFilter == ShowRolesFilter.HotOnly);
            hotList.Name.ShouldBe("Горячие роли");
            hotList.PublicMode.ShouldBeTrue();
            hotList.CharacterGroupId.ShouldBeNull();
            hotList.Fields.ShouldContain(descriptionFieldId);

            var vacantList = rolesLists.Single(l => l.ShowRolesFilter == ShowRolesFilter.VacantOnly);
            vacantList.Name.ShouldBe("Вакантные роли");
            vacantList.PublicMode.ShouldBeTrue();
            vacantList.CharacterGroupId.ShouldBeNull();
            vacantList.Fields.ShouldContain(descriptionFieldId);
        });
    }
}
