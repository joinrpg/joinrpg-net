using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.ProjectMetadata;
using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.IntegrationTests.TestInfrastructure;

namespace JoinRpg.IntegrationTest.Scenarios;

public class LarpDefaultRolesListScenario(JoinApplicationFactory factory) : IClassFixture<JoinApplicationFactory>
{
    [Fact]
    public async Task CreateLarpProject_AutoCreatesPublicHotRolesList()
    {
        // 1. Пользователь и Larp-проект (CreateProjectAsync по умолчанию создаёт Larp)
        using var scope = factory.Services.CreateScope();
        var userId = await TestUserProjectHelpers.CreateTestUserAsync(scope.ServiceProvider);
        var projectId = await TestUserProjectHelpers.CreateProjectAsync(scope.ServiceProvider, userId);

        // 2. Проверяем, что автоматически создана публичная сетка «Горячие роли»
        await factory.Services.RunAsAsync(userId, async sp =>
        {
            var rolesListRepository = sp.GetRequiredService<IProjectRolesListRepository>();
            var metadataRepository = sp.GetRequiredService<IProjectMetadataRepository>();

            var projectInfo = await metadataRepository.GetProjectMetadata(projectId);
            var descriptionFieldId = (projectInfo.CharacterDescriptionField
                ?? throw new InvalidOperationException("В проекте нет поля описания персонажа"))
                .Id;

            var rolesLists = await rolesListRepository.GetForProjectAsync(projectId);

            var hotList = rolesLists.ShouldHaveSingleItem();
            hotList.Name.ShouldBe("Горячие роли");
            hotList.PublicMode.ShouldBeTrue();
            hotList.CharacterGroupId.ShouldBeNull();
            hotList.ShowRolesFilter.ShouldBe(ShowRolesFilter.HotOnly);
            hotList.Fields.ShouldContain(descriptionFieldId);
        });
    }
}
