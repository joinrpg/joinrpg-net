using System.Net;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Data.Interfaces;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Characters;
using JoinRpg.Services.Interfaces.ProjectMetadata;
using JoinRpg.Services.Interfaces.Projects;
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
            var rootGroupId = projectInfo.GroupTree.RootGroupId;
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
                    FieldValues: new FieldLayerContainer(projectInfo, new Dictionary<int, string?> { [nameFieldId] = name })));
            }

            // Приватный персонаж: на публичной сетке виден только мастеру.
            await characterService.AddCharacter(new AddCharacterRequest(
                projectId,
                ParentCharacterGroupIds: [rootGroupId],
                new CharacterTypeInfo(CharacterType.Player, IsHot: false, SlotLimit: null, SlotName: null, CharacterVisibility.Private),
                FieldValues: new FieldLayerContainer(projectInfo, new Dictionary<int, string?> { [nameFieldId] = privateName })));

            var created = await rolesListService.CreateAsync(new ProjectRolesList(
                new ProjectRolesListIdentification(projectId, -1),
                "Все роли",
                CharacterGroupId: null,
                PublicMode: true,
                Fields: [],
                ContactsColumn: PlayerColumnMode.NameOnly,
                GroupsColumn: ProjectRolesListVisibilityMode.None,
                GroupsViewMode: RolesGridGroupsViewMode.Sections,
                ShowRolesFilter: ShowRolesFilter.All));

            return (created.ProjectRolesListId!, names);
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
                ContactsColumn: PlayerColumnMode.NameOnly,
                GroupsColumn: ProjectRolesListVisibilityMode.None,
                GroupsViewMode: RolesGridGroupsViewMode.Sections,
                ShowRolesFilter: ShowRolesFilter.All));
            return created.ProjectRolesListId!;
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

    /// <summary>
    /// Игрок утверждённой заявки доезжает до сетки ролей и списка персонажей через
    /// <c>IClaimInfoRepository.GetApprovedClaimInfos</c> (ADR021). Юнит-тест билдера собирает
    /// словарь сам, поэтому ошибку в репозитории (не тот ключ, потерянный отбор) ловит только этот.
    /// </summary>
    [Fact]
    public async Task ApprovedClaimPlayer_IsShownInGridAndCharacterList()
    {
        UserIdentification masterId;
        string email;
        ProjectIdentification projectId;
        UserIdentification playerId;
        string playerEmail;
        const string password = "Password123!";
        using (var scope = factory.Services.CreateScope())
        {
            (masterId, email) = await TestUserProjectHelpers.CreateTestUserWithEmailAsync(
                scope.ServiceProvider, password: password);
            projectId = await TestUserProjectHelpers.CreateProjectAsync(
                scope.ServiceProvider, masterId, "Проект с игроком в сетке");
            (playerId, playerEmail) = await TestUserProjectHelpers.CreateTestUserWithEmailAsync(scope.ServiceProvider);
        }

        var (characterId, rolesListId) = await factory.Services.RunAsAsync(masterId, async sp =>
        {
            var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>().GetProjectMetadata(projectId);
            await sp.GetRequiredService<IProjectService>().SetClaimSettings(
                projectId,
                projectInfo.ClaimSettings with { AutoAcceptClaims = false, IsAcceptingClaims = true });

            var characterId = await sp.GetRequiredService<ICharacterService>().AddCharacter(new AddCharacterRequest(
                projectId,
                ParentCharacterGroupIds: [projectInfo.GroupTree.RootGroupId],
                new CharacterTypeInfo(CharacterType.Player, IsHot: false, SlotLimit: null, SlotName: null, CharacterVisibility.Public),
                FieldValues: FieldLayerContainer.Empty(projectInfo)));

            var created = await sp.GetRequiredService<IProjectRolesListService>().CreateAsync(new ProjectRolesList(
                new ProjectRolesListIdentification(projectId, -1),
                "Все роли",
                CharacterGroupId: null,
                PublicMode: true,
                Fields: [],
                ContactsColumn: PlayerColumnMode.NameOnly,
                GroupsColumn: ProjectRolesListVisibilityMode.None,
                GroupsViewMode: RolesGridGroupsViewMode.Sections,
                ShowRolesFilter: ShowRolesFilter.All));

            return (characterId, created.ProjectRolesListId!);
        });

        var claimId = await factory.Services.RunAsAsync(playerId, async sp =>
        {
            var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>().GetProjectMetadata(projectId);
            return await sp.GetRequiredService<IClaimService>().AddClaimFromUser(
                characterId, "Заявка", FieldLayerContainer.Empty(projectInfo), sensitiveDataAllowed: false);
        });
        await factory.Services.RunAsAsync(masterId, sp => sp.GetRequiredService<IClaimService>().ApproveByMaster(claimId, "Принято"));

        // Отображаемое имя игрока без ФИО — часть почты до «@».
        var playerName = playerEmail.Split('@')[0];

        var masterClient = await TestUserProjectHelpers.CreateAuthenticatedClientAsync(factory.CreateClient(), email, password);

        var gridJson = await masterClient.GetStringAsync(
            $"webapi/project-role-grid/get?projectId={projectId.Value}&projectRolesListId={rolesListId.ProjectRolesListId}");
        gridJson.ShouldContain(playerName);

        var listResponse = await masterClient.GetAsync($"{projectId.Value}/characters/withplayers");
        listResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await listResponse.Content.ReadAsStringAsync()).ShouldContain(playerName);
    }

    [Fact]
    public async Task ClassicGrid_HotOnly_ShowsOnlyHotCharacters()
    {
        UserIdentification masterId;
        ProjectIdentification projectId;
        using (var scope = factory.Services.CreateScope())
        {
            (masterId, _) = await TestUserProjectHelpers.CreateTestUserWithEmailAsync(scope.ServiceProvider);
            projectId = await TestUserProjectHelpers.CreateProjectAsync(
                scope.ServiceProvider, masterId, "Проект с горячими ролями");
        }

        const string hotName = "Горячая роль";
        const string coldName = "Обычная роль";
        await factory.Services.RunAsAsync(masterId, async sp =>
        {
            var metadataRepository = sp.GetRequiredService<IProjectMetadataRepository>();
            var characterService = sp.GetRequiredService<ICharacterService>();

            var projectInfo = await metadataRepository.GetProjectMetadata(projectId);
            var rootGroupId = projectInfo.GroupTree.RootGroupId;
            var nameFieldId = (projectInfo.CharacterNameField
                ?? throw new InvalidOperationException("В проекте нет поля имени персонажа"))
                .Id.ProjectFieldId;

            await characterService.AddCharacter(new AddCharacterRequest(
                projectId,
                ParentCharacterGroupIds: [rootGroupId],
                new CharacterTypeInfo(CharacterType.Player, IsHot: true, SlotLimit: null, SlotName: null, CharacterVisibility.Public),
                FieldValues: new FieldLayerContainer(projectInfo, new Dictionary<int, string?> { [nameFieldId] = hotName })));

            await characterService.AddCharacter(new AddCharacterRequest(
                projectId,
                ParentCharacterGroupIds: [rootGroupId],
                new CharacterTypeInfo(CharacterType.Player, IsHot: false, SlotLimit: null, SlotName: null, CharacterVisibility.Public),
                FieldValues: new FieldLayerContainer(projectInfo, new Dictionary<int, string?> { [nameFieldId] = coldName })));
        });

        var anonClient = factory.CreateClient();

        // Страница /{projectId}/roles/hot открывается.
        var hotPageResponse = await anonClient.GetAsync($"{projectId.Value}/roles/hot");
        hotPageResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        // hotOnly=true — только горячая роль.
        var hotResult = await anonClient.GetFromJsonAsync<ProjectRoleGridViewResult>(
            $"webapi/project-role-grid/getclassic?projectId={projectId.Value}&hotOnly=true");
        hotResult!.HasAccess.ShouldBeTrue();
        var hotCharacterNames = CharacterNames(hotResult.Grid!);
        hotCharacterNames.ShouldContain(hotName);
        hotCharacterNames.ShouldNotContain(coldName);

        // Без hotOnly (обычная классическая сетка) — видны обе роли.
        var classicResult = await anonClient.GetFromJsonAsync<ProjectRoleGridViewResult>(
            $"webapi/project-role-grid/getclassic?projectId={projectId.Value}");
        var classicCharacterNames = CharacterNames(classicResult!.Grid!);
        classicCharacterNames.ShouldContain(hotName);
        classicCharacterNames.ShouldContain(coldName);
    }

    /// <summary>
    /// Битая ссылка (id группы или сетки, которых в проекте нет) — не 500, а осмысленное
    /// «не найдено», иначе остров навсегда зависает в «Идет загрузка...» (issue #5113).
    /// </summary>
    [Fact]
    public async Task MissingGroupOrRolesList_ReturnsNotFound_NotServerError()
    {
        UserIdentification masterId;
        ProjectIdentification projectId;
        using (var scope = factory.Services.CreateScope())
        {
            (masterId, _) = await TestUserProjectHelpers.CreateTestUserWithEmailAsync(scope.ServiceProvider);
            projectId = await TestUserProjectHelpers.CreateProjectAsync(
                scope.ServiceProvider, masterId, "Проект с битыми ссылками на сетку");
        }

        // Заведомо несуществующие id: в свежем проекте групп с таким номером точно нет.
        const int missingGroupId = 999999;
        const int missingRolesListId = 999999;

        var anonClient = factory.CreateClient();

        foreach (var url in new[]
        {
            $"webapi/project-role-grid/getclassic?projectId={projectId.Value}&characterGroupId={missingGroupId}",
            $"webapi/project-role-grid/getclassic?projectId={projectId.Value}&characterGroupId={missingGroupId}&hotOnly=true",
            $"webapi/project-role-grid/get?projectId={projectId.Value}&projectRolesListId={missingRolesListId}",
        })
        {
            var response = await anonClient.GetAsync(url);
            response.StatusCode.ShouldBe(HttpStatusCode.OK, url);

            var result = await response.Content.ReadFromJsonAsync<ProjectRoleGridViewResult>();
            result!.NotFound.ShouldBeTrue(url);
            result.Grid.ShouldBeNull(url);
        }
    }

    private static List<string> CharacterNames(ProjectRoleGridViewModel grid) =>
        [.. grid.Rows.OfType<ProjectRoleGridCharacterRowViewModel>().Select(r => r.Character.Character.Name)];
}
