using System.Net;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Data.Interfaces;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.IntegrationTests.TestInfrastructure;
using JoinRpg.Services.Interfaces.Characters;
using JoinRpg.Services.Interfaces.ProjectMetadata;

namespace JoinRpg.IntegrationTest.Scenarios;

public class ProjectRoleGridScenario(JoinApplicationFactory factory) : IClassFixture<JoinApplicationFactory>
{
    [Fact]
    public async Task CreateProjectWithCharacters_AndRolesList_GridPageAndApiWork()
    {
        // 1. Мастер и проект
        UserIdentification masterId;
        string email;
        ProjectIdentification projectId;
        const string password = "Password123!";
        using (var scope = factory.Services.CreateScope())
        {
            (masterId, email) = await TestUserProjectHelpers.CreateTestUserWithEmailAsync(
                scope.ServiceProvider, password: password);
            projectId = await TestUserProjectHelpers.CreateProjectAsync(
                scope.ServiceProvider, masterId, "Проект с сеткой ролей");
        }

        // 2. Пара персонажей в корневой группе + 3. сетка ролей «от верха»
        var (rolesListId, characterNames) = await factory.Services.RunAsAsync(masterId, async sp =>
        {
            var metadataRepository = sp.GetRequiredService<IProjectMetadataRepository>();
            var characterService = sp.GetRequiredService<ICharacterService>();
            var rolesListService = sp.GetRequiredService<IProjectRolesListService>();

            var projectInfo = await metadataRepository.GetProjectMetadata(projectId);
            var rootGroupId = projectInfo.RootCharacterGroupId;
            var nameFieldId = (projectInfo.CharacterNameField
                ?? throw new InvalidOperationException("В проекте нет поля имени персонажа"))
                .Id.ProjectFieldId;

            string[] names = ["Вантала", "Боромир"];
            foreach (var name in names)
            {
                await characterService.AddCharacter(new AddCharacterRequest(
                    projectId,
                    ParentCharacterGroupIds: [rootGroupId],
                    new CharacterTypeInfo(CharacterType.Player, IsHot: false, SlotLimit: null, SlotName: null, CharacterVisibility.Public),
                    FieldValues: new Dictionary<int, string?> { [nameFieldId] = name }));
            }

            var created = await rolesListService.CreateAsync(new ProjectRolesList(
                new ProjectRolesListIdentification(projectId, -1),
                "Все роли",
                CharacterGroupId: null,
                PublicMode: true,
                Fields: [],
                ContactsColumn: ProjectRolesListVisibilityMode.None,
                GroupsColumn: ProjectRolesListVisibilityMode.None));

            return (created.ProjectRolesListId, names);
        });

        var client = factory.CreateClient();
        client = await TestUserProjectHelpers.CreateAuthenticatedClientAsync(client, email, password);

        // 4. Страница сетки ролей открывается
        var pageResponse = await client.GetAsync($"{projectId.Value}/roleslist/{rolesListId.ProjectRolesListId}");
        pageResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        // 5. WebAPI острова возвращает сетку с обоими персонажами
        var apiResponse = await client.GetAsync(
            $"webapi/project-role-grid/get?projectId={projectId.Value}&projectRolesListId={rolesListId.ProjectRolesListId}");
        apiResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var json = await apiResponse.Content.ReadAsStringAsync();
        foreach (var name in characterNames)
        {
            json.ShouldContain(name);
        }
    }
}
