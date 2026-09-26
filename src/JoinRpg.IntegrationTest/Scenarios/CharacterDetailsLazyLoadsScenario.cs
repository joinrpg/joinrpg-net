using System.Net;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Data.Interfaces;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Plots;
using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.IntegrationTests.TestInfrastructure;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Characters;
using JoinRpg.Services.Interfaces.Projects;

namespace JoinRpg.IntegrationTest.Scenarios;

/// <summary>
/// Публичная страница персонажа не должна делать ленивых загрузок (#4992).
/// </summary>
/// <remarks>
/// Страница рендерит вводные, а в их тексте бывают директивы <c>%список</c>/<c>%персонаж</c>.
/// Разворачивает их <c>JoinrpgMarkdownLinkRenderer</c> поверх EF-графа проекта: если граф
/// не загружен заранее, EF6 тянет персонажей, их утверждённые заявки и игроков по одному —
/// на проде это давало до 28 запросов в <c>Claims</c> за один анонимный заход.
///
/// Проверку делает не сам тест, а <c>LazyLoadAssertingHandler</c> на клиенте фабрики: маршрута
/// нет в <c>lazy-loads-baseline.json</c>, значит ленивых загрузок должно быть ноль
/// (см. docs/lazy-loads-baseline.md). Поэтому здесь важно, чтобы страница реально отрисовала
/// директиву — иначе N+1 просто не случится, и тест станет пустым.
/// </remarks>
public class CharacterDetailsLazyLoadsScenario(JoinApplicationFactory factory) : IClassFixture<JoinApplicationFactory>
{
    /// <summary>Персонажей в группе, на которую ссылается директива <c>%список</c>.</summary>
    private const int CharactersInGroup = 4;

    [Fact]
    public async Task AnonymousCharacterDetails_WithCharacterListDirective_DoesNotLazyLoad()
    {
        UserIdentification masterId;
        ProjectIdentification projectId;
        var playerIds = new List<UserIdentification>(CharactersInGroup);
        using (var scope = factory.Services.CreateScope())
        {
            masterId = await TestUserProjectHelpers.CreateTestUserAsync(scope.ServiceProvider);
            projectId = await TestUserProjectHelpers.CreateProjectAsync(
                scope.ServiceProvider, masterId, "Проект с публичной страницей персонажа");
            for (var i = 0; i < CharactersInGroup; i++)
            {
                playerIds.Add(await TestUserProjectHelpers.CreateTestUserAsync(scope.ServiceProvider));
            }
        }

        var setup = await factory.Services.RunAsAsync(masterId, async sp =>
        {
            var projectService = sp.GetRequiredService<IProjectService>();
            var metadataRepository = sp.GetRequiredService<IProjectMetadataRepository>();
            var characterService = sp.GetRequiredService<ICharacterService>();
            var characterGroupService = sp.GetRequiredService<ICharacterGroupService>();
            var plotService = sp.GetRequiredService<IPlotService>();

            var projectInfo = await metadataRepository.GetProjectMetadata(projectId);
            await projectService.SetClaimSettings(
                projectId,
                projectInfo.ClaimSettings with { AutoAcceptClaims = false, IsAcceptingClaims = true });

            var rootGroupId = projectInfo.GroupTree.RootGroupId;
            var nameFieldId = (projectInfo.CharacterNameField
                    ?? throw new InvalidOperationException("В проекте нет поля имени персонажа"))
                .Id.ProjectFieldId;

            var groupId = await characterGroupService.AddCharacterGroup(
                projectId,
                "Публичная группа",
                isPublic: true,
                parentCharacterGroupIds: [rootGroupId],
                description: "");

            var characters = new List<CharacterIdentification>(CharactersInGroup);
            var names = new List<string>(CharactersInGroup);
            for (var i = 0; i < CharactersInGroup; i++)
            {
                var name = $"Персонаж {i} {Guid.NewGuid().ToString("N")[..6]}";
                names.Add(name);
                characters.Add(await characterService.AddCharacter(
                    new AddCharacterRequest(
                        projectId,
                        ParentCharacterGroupIds: [groupId],
                        new CharacterTypeInfo(
                            CharacterType.Player,
                            IsHot: false,
                            SlotLimit: null,
                            SlotName: null,
                            CharacterVisibility.Public),
                        FieldValues: new FieldLayerContainer(
                            projectInfo,
                            new Dictionary<int, string?> { [nameFieldId] = name }))));
            }

            var plotFolderId = await plotService.CreatePlotFolder(projectId, "Сюжет с директивой", todo: "");

            // Директива %список разворачивается в перечисление всех персонажей группы с их игроками —
            // ровно тот путь, который на проде грузил заявки по одной.
            var versionId = await plotService.CreatePlotElement(
                plotFolderId,
                content: $"Во взводе служат: %список{groupId.CharacterGroupId}",
                todoField: "",
                targetGroups: [],
                targetChars: [characters[0]],
                elementType: PlotElementType.RegularPlot,
                isMasterOnly: false);
            await plotService.PublishElementVersion(versionId, sendNotification: false, commentText: null);

            return (Characters: characters, Names: names);
        });

        // У каждого персонажа свой игрок с утверждённой заявкой: иначе директива не пойдёт
        // по навигации ApprovedClaim.Player и N+1 не воспроизведётся.
        for (var i = 0; i < CharactersInGroup; i++)
        {
            var characterId = setup.Characters[i];
            var claimId = await factory.Services.RunAsAsync(playerIds[i], async sp =>
            {
                var claimService = sp.GetRequiredService<IClaimService>();
                var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>()
                    .GetProjectMetadata(projectId);
                return await claimService.AddClaimFromUser(
                    characterId,
                    "Хочу эту роль",
                    FieldLayerContainer.Empty(projectInfo),
                    sensitiveDataAllowed: false);
            });

            await factory.Services.RunAsAsync(masterId, async sp =>
            {
                await sp.GetRequiredService<IClaimService>().ApproveByMaster(claimId, "Принято");
                return claimId;
            });
        }

        // Анонимному читателю вводные видны только у проекта с опубликованными сюжетами.
        await factory.Services.RunAsAsync(masterId, async sp =>
        {
            await sp.GetRequiredService<IProjectService>().CloseProject(projectId, publishPlot: true);
            return projectId;
        });

        var client = factory.CreateClient();
        var characterIdToShow = setup.Characters[0].CharacterId;

        // Оба маршрута экшена Details — у каждого свой ключ в снапшоте ленивых загрузок.
        foreach (var url in new[]
                 {
                     $"{projectId.Value}/character/{characterIdToShow}/details",
                     $"{projectId.Value}/character/{characterIdToShow}/",
                 })
        {
            var response = await client.GetAsync(url);
            response.StatusCode.ShouldBe(HttpStatusCode.OK, $"Страница {url} не открылась");

            var document = await response.AsHtmlDocument();
            var text = WebUtility.HtmlDecode(
                document.DocumentNode.SelectSingleNode("//body")?.InnerText
                ?? throw new InvalidOperationException("Страница не содержит body"));

            // Директива должна развернуться: без этого тест не проверяет ничего.
            text.ShouldNotContain("%список");
            foreach (var name in setup.Names)
            {
                text.ShouldContain(name, customMessage: $"Директива %список не перечислила персонажа на {url}");
            }
        }
    }
}
