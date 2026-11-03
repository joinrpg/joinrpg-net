using System.Net;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Data.Interfaces;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Plots;
using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Characters;
using JoinRpg.Services.Interfaces.ProjectMetadata;
using JoinRpg.Services.Interfaces.Projects;
using JoinRpg.Web.CharacterGroups.ProjectRoleGrid;

namespace JoinRpg.IntegrationTest.Scenarios;

/// <summary>Сквозной сценарий вводной-сетки (ADR029): создание, публикация и доступ игрока.</summary>
public class RoleGridPlotScenario(JoinApplicationFactory factory) : IClassFixture<JoinApplicationFactory>
{
    [Fact]
    public async Task PrivateGridInPublishedPlot_PlayerSeesItOnCharacterPage_AnonymousDoesNot()
    {
        UserIdentification masterId;
        UserIdentification playerId;
        string playerEmail;
        ProjectIdentification projectId;
        using (var scope = factory.Services.CreateScope())
        {
            (masterId, _) = await TestUserProjectHelpers.CreateTestUserWithEmailAsync(scope.ServiceProvider);
            projectId = await TestUserProjectHelpers.CreateProjectAsync(scope.ServiceProvider, masterId, "Проект с вводной-сеткой");
            (playerId, playerEmail) = await TestUserProjectHelpers.CreateTestUserWithEmailAsync(scope.ServiceProvider);
        }

        var seed = await factory.Services.RunAsAsync(masterId, async sp =>
        {
            var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>().GetProjectMetadata(projectId);
            await sp.GetRequiredService<IProjectService>().SetClaimSettings(
                projectId,
                projectInfo.ClaimSettings with { AutoAcceptClaims = false, IsAcceptingClaims = true });

            var plot = await TestPlotHelpers.SeedPlotFolderAsync(sp, projectId, elementCount: 1);

            // Этот персонаж есть только в сетке, но не является заголовком страницы: его имя в HTML
            // доказывает, что вложенный ProjectRoleGrid действительно отрендерился при пререндере.
            var gridOnlyCharacterName = $"Только в сетке {Guid.NewGuid().ToString("N")[..8]}";
            _ = await sp.GetRequiredService<ICharacterService>().AddCharacter(new AddCharacterRequest(
                projectId,
                ParentCharacterGroupIds: [projectInfo.GroupTree.RootGroupId],
                new CharacterTypeInfo(CharacterType.Player, IsHot: false, SlotLimit: null, SlotName: null, CharacterVisibility.Public),
                FieldValues: new FieldLayerContainer(
                    projectInfo,
                    new Dictionary<int, string?> { [projectInfo.CharacterNameField!.Id.ProjectFieldId] = gridOnlyCharacterName })));

            var rolesListId = (await sp.GetRequiredService<IProjectRolesListService>().CreateAsync(new ProjectRolesList(
                new ProjectRolesListIdentification(projectId, -1),
                "Соседи по сюжету",
                CharacterGroupId: null,
                PublicMode: false,
                Fields: [],
                ContactsColumn: PlayerColumnMode.NameOnly,
                GroupsColumn: ProjectRolesListVisibilityMode.None,
                GroupsViewMode: RolesGridGroupsViewMode.None,
                ShowRolesFilter: ShowRolesFilter.All))).ProjectRolesListId!;

            var plotService = sp.GetRequiredService<IPlotService>();
            var versionId = await plotService.CreatePlotElement(
                plot.PlotFolderId,
                content: "Текст у вводной-сетки игнорируется",
                todoField: "",
                targetGroups: [],
                targetChars: [plot.TargetCharacterId],
                elementType: PlotElementType.RoleGrid,
                isMasterOnly: false,
                projectRolesListId: rolesListId);
            await plotService.PublishElementVersion(versionId, sendNotification: false, commentText: null);

            // Форма редактирования не постит Content: пустое сохранение не должно плодить версию
            // и сбрасывать публикацию (регрессия, найденная ревью PR 2).
            await plotService.EditPlotElement(
                versionId.PlotElementId,
                contents: null!,
                todoField: "",
                targetGroups: [],
                targetChars: [plot.TargetCharacterId],
                isMasterOnly: false,
                projectRolesListId: rolesListId);

            var element = await sp.GetRequiredService<IPlotRepository>().GetPlotElementDetails(versionId.PlotElementId);
            element.ShouldNotBeNull();
            element.LastVersionNumber.ShouldBe(0);
            element.PublishedVersion.ShouldBe(0);
            element.IsLastVersionPublished.ShouldBeTrue();

            return (Plot: plot, RolesListId: rolesListId, ElementId: versionId.PlotElementId, GridOnlyCharacterName: gridOnlyCharacterName);
        });

        var claimId = await factory.Services.RunAsAsync(playerId, async sp =>
        {
            var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>().GetProjectMetadata(projectId);
            return await sp.GetRequiredService<IClaimService>().AddClaimFromUser(
                seed.Plot.TargetCharacterId,
                "Хочу эту роль",
                FieldLayerContainer.Empty(projectInfo),
                sensitiveDataAllowed: false);
        });
        await factory.Services.RunAsAsync(masterId, sp => sp.GetRequiredService<IClaimService>().ApproveByMaster(claimId, "Принято"));

        var plotApiUrl = $"webapi/project-role-grid/getforplot?projectId={projectId.Value}&plotElementId={seed.ElementId}";
        var directApiUrl = $"webapi/project-role-grid/get?projectId={projectId.Value}&rolesListId={seed.RolesListId}";

        var playerClient = await TestUserProjectHelpers.CreateAuthenticatedClientAsync(factory.CreateClient(), playerEmail);

        var plotResult = await playerClient.GetFromJsonAsync<ProjectRoleGridViewResult>(plotApiUrl);
        plotResult.ShouldNotBeNull();
        plotResult.HasAccess.ShouldBeTrue();
        plotResult.NotFound.ShouldBeFalse();
        plotResult.Grid.ShouldNotBeNull();

        // Прямая ссылка на приватную сетку игроку по-прежнему закрыта — доступ даёт именно вводная.
        var directResult = await playerClient.GetFromJsonAsync<ProjectRoleGridViewResult>(directApiUrl);
        directResult.ShouldNotBeNull();
        directResult.HasAccess.ShouldBeFalse();

        var characterPage = WebUtility.HtmlDecode(await playerClient.GetStringAsync(
            $"{projectId.Value}/character/{seed.Plot.TargetCharacterId.CharacterId}/details"));
        characterPage.ShouldContain("Сетка ролей: Соседи по сюжету");
        characterPage.ShouldContain(seed.GridOnlyCharacterName);
        // Обычная вводная в смешанном списке по-прежнему рендерится как markdown.
        characterPage.ShouldContain(seed.Plot.ElementContents.Single());

        var anonymousResult = await factory.CreateClient().GetFromJsonAsync<ProjectRoleGridViewResult>(plotApiUrl);
        anonymousResult.ShouldNotBeNull();
        anonymousResult.HasAccess.ShouldBeFalse();
    }
}
