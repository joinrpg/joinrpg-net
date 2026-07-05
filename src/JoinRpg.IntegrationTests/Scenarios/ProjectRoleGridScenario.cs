using System.Net;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Data.Interfaces;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.IntegrationTests.TestInfrastructure;
using JoinRpg.Services.Interfaces.Characters;
using JoinRpg.Services.Interfaces.ProjectMetadata;
using JoinRpg.Web.CharacterGroups.ProjectRoleGrid;

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

        // 2. Публичные персонажи + приватный персонаж в корневой группе + 3. сетка ролей «от верха»
        const string privateName = "Скрытный";
        var (rolesListId, publicNames) = await factory.Services.RunAsAsync(masterId, async sp =>
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

            // Приватный персонаж: на публичной сетке виден только мастеру.
            await characterService.AddCharacter(new AddCharacterRequest(
                projectId,
                ParentCharacterGroupIds: [rootGroupId],
                new CharacterTypeInfo(CharacterType.Player, IsHot: false, SlotLimit: null, SlotName: null, CharacterVisibility.Private),
                FieldValues: new Dictionary<int, string?> { [nameFieldId] = privateName }));

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

        var apiUrl = $"webapi/project-role-grid/get?projectId={projectId.Value}&projectRolesListId={rolesListId.ProjectRolesListId}";

        var masterClient = factory.CreateClient();
        masterClient = await TestUserProjectHelpers.CreateAuthenticatedClientAsync(masterClient, email, password);

        // 4. Страница сетки ролей открывается
        var pageResponse = await masterClient.GetAsync($"{projectId.Value}/roleslist/{rolesListId.ProjectRolesListId}");
        pageResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        // 5. Мастер видит публичных и приватного персонажа
        var masterResponse = await masterClient.GetAsync(apiUrl);
        masterResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var masterJson = await masterResponse.Content.ReadAsStringAsync();
        foreach (var name in publicNames)
        {
            masterJson.ShouldContain(name);
        }
        masterJson.ShouldContain(privateName);

        // 6. Аноним (публичная сетка) видит публичных, но НЕ приватного персонажа
        var anonClient = factory.CreateClient();
        var anonResponse = await anonClient.GetAsync(apiUrl);
        anonResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var anonJson = await anonResponse.Content.ReadAsStringAsync();
        foreach (var name in publicNames)
        {
            anonJson.ShouldContain(name);
        }
        anonJson.ShouldNotContain(privateName);

        // 7. Непубличная сетка: мастер видит данные, аноним получает «нет доступа» (200, а не ошибка)
        var privateRolesListId = await factory.Services.RunAsAsync(masterId, async sp =>
        {
            var rolesListService = sp.GetRequiredService<IProjectRolesListService>();
            var created = await rolesListService.CreateAsync(new ProjectRolesList(
                new ProjectRolesListIdentification(projectId, -1),
                "Мастерская сетка",
                CharacterGroupId: null,
                PublicMode: false,
                Fields: [],
                ContactsColumn: ProjectRolesListVisibilityMode.None,
                GroupsColumn: ProjectRolesListVisibilityMode.None));
            return created.ProjectRolesListId;
        });

        var privateApiUrl = $"webapi/project-role-grid/get?projectId={projectId.Value}&projectRolesListId={privateRolesListId.ProjectRolesListId}";

        var masterPrivateResult = await masterClient.GetFromJsonAsync<ProjectRoleGridViewResult>(privateApiUrl);
        masterPrivateResult!.HasAccess.ShouldBeTrue();
        masterPrivateResult.Grid.ShouldNotBeNull();
        masterPrivateResult.NoAccess.ShouldBeNull();

        var anonPrivateResponse = await anonClient.GetAsync(privateApiUrl);
        anonPrivateResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var anonPrivateResult = await anonPrivateResponse.Content.ReadFromJsonAsync<ProjectRoleGridViewResult>();
        anonPrivateResult!.HasAccess.ShouldBeFalse();
        anonPrivateResult.Grid.ShouldBeNull();
        anonPrivateResult.NoAccess.ShouldNotBeNull();
    }
}
