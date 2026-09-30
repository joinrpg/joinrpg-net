using System.Net;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Dal.Impl;
using JoinRpg.Data.Interfaces;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Characters;
using JoinRpg.Services.Interfaces.Projects;
using JoinRpg.WebPortal.Managers.Characters;

namespace JoinRpg.IntegrationTest.Scenarios;

/// <summary>
/// Глобальный поиск (/search без привязки к проекту).
/// <para>
/// Регрессия: провайдеры поиска фильтровали проект прямо в дереве выражений
/// (<c>projectId == null || cg.ProjectId == projectId.Value</c>). EF6 вычисляет замыкания
/// без короткого замыкания <c>||</c>, поэтому при глобальном поиске (projectId = null) лез за
/// <c>projectId.Value</c> у null-ссылки и валил страницу пятисоткой
/// (<c>TargetException: Non-static method requires a target</c>).
/// Поймать это можно только на настоящем EF6-контексте: LINQ to Objects в in-memory фейке
/// отрабатывает <c>||</c> лениво и бага не видит.
/// </para>
/// </summary>
public class GlobalSearchScenario(JoinApplicationFactory factory) : IClassFixture<JoinApplicationFactory>
{
    private const string Password = "Password123!";
    private const string CharacterName = "Вантала";

    /// <summary>Сколько персонажей с искомым именем в каждом проекте.</summary>
    /// <remarks>
    /// Долг по ленивым загрузкам на этой странице — тройка <c>Projects</c>/<c>ProjectDetails</c>/
    /// <c>ProjectAcls</c> на каждый найденный объект (#4991). На одном результате в одном проекте
    /// она не отличима от одиночного запроса, поэтому объектов должно быть много и в разных
    /// проектах — иначе замер честно показывает ноль при живом N+1 на проде.
    /// </remarks>
    private const int CharactersPerProject = 3;

    [Fact]
    public async Task GlobalSearch_WithoutProjectScope_Works()
    {
        var (masterId, email) = await CreateMasterAsync();
        var projectId = await SeedProjectWithMatchesAsync(masterId, "Проект для глобального поиска");

        // Второй и третий проекты — чужие: в них ищущий не мастер, поэтому проверка видимости
        // скрытых находок доходит до последнего операнда и лезет за правами (#4991).
        var (otherMasterId, _) = await CreateMasterAsync();
        _ = await SeedProjectWithMatchesAsync(otherMasterId, "Чужой проект для глобального поиска");
        _ = await SeedProjectWithMatchesAsync(otherMasterId, "Второй чужой проект для глобального поиска");

        var client = factory.CreateClient();
        client = await TestUserProjectHelpers.CreateAuthenticatedClientAsync(client, email, Password);

        // Текстовый поиск: работают провайдеры групп, персонажей и сюжетов
        var byNameResponse = await client.GetAsync($"search?searchString={Uri.EscapeDataString(CharacterName)}");
        byNameResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Числовой поиск: дополнительно включается провайдер заявок по id
        var byIdResponse = await client.GetAsync("search?searchString=999999");
        byIdResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Для контраста — поиск, сужённый до проекта (x-api/MCP), должен фильтровать по проекту и находить персонажа
        var scopedResults = await factory.Services.RunAsAsync(masterId, async sp =>
            await sp.GetRequiredService<ISearchApiViewService>().SearchCharacters(projectId, CharacterName));

        scopedResults.Select(r => r.CharacterName).ShouldContain(CharacterName);
    }

    /// <summary>
    /// Глобальный поиск не должен догружать проект, его настройки и права на каждую находку (#4991).
    /// </summary>
    /// <remarks>
    /// Замер идёт вокруг сервисного вызова, а не вокруг HTTP-запроса: <c>SearchController</c> висит
    /// на конвенциональном маршруте, и в снапшоте <c>lazy-loads-baseline.json</c> он неотличим от
    /// остальных страниц маршрута <c>GET /{controller=Home}/{action=Index}/{id?}</c>, где долг ещё есть.
    /// </remarks>
    [Fact]
    public async Task GlobalSearch_DoesNotLazyLoadProjectPerResult()
    {
        var (masterId, _) = await CreateMasterAsync();
        _ = await SeedProjectWithMatchesAsync(masterId, "Свой проект для замера ленивых загрузок");

        // Чужие проекты: ищущий в них не мастер, поэтому на скрытых находках проверка видимости
        // доходит до последнего операнда и на EF-сущностях полезла бы за Project/Details/Acls.
        var (otherMasterId, _) = await CreateMasterAsync();
        _ = await SeedProjectWithMatchesAsync(otherMasterId, "Чужой проект для замера ленивых загрузок");
        _ = await SeedProjectWithMatchesAsync(otherMasterId, "Второй чужой проект для замера ленивых загрузок");

        var (count, resultsCount) = await factory.Services.RunAsAsync(masterId, async sp =>
        {
            var searchService = sp.GetRequiredService<ISearchService>();

            using var lazyLoads = LazyLoadCounter.BeginScope();
            var results = await searchService.SearchAsync(masterId.Value, CharacterName);
            return (lazyLoads.Count, results.Count);
        });

        // Находок должно быть много и из разных проектов — иначе замер ничего не сторожит.
        resultsCount.ShouldBeGreaterThan(CharactersPerProject);
        count.ShouldBe(0, $"Глобальный поиск дал {count} ленивых загрузок на {resultsCount} находок, см. #4991");
    }

    private async Task<(UserIdentification MasterId, string Email)> CreateMasterAsync()
    {
        using var scope = factory.Services.CreateScope();
        return await TestUserProjectHelpers.CreateTestUserWithEmailAsync(
            scope.ServiceProvider, password: Password);
    }

    /// <summary>
    /// Проект, в котором искомую строку находят и персонажи, и группы: провайдеров поиска
    /// несколько, и каждый найденный объект — отдельная строка на странице результатов.
    /// </summary>
    /// <remarks>
    /// Часть находок намеренно непубличные. Проверка видимости
    /// (<c>WorldObjectExtensions.IsVisible</c>) выглядит как
    /// <c>IsPublic || Project.Details.PublishPlot || HasMasterAccess(...)</c>: на публичном объекте
    /// она останавливается на первом операнде и проекта не касается вовсе, а на непубличном идёт
    /// за проектом, его настройками и правами — это и есть догружаемая тройка
    /// <c>Projects</c>/<c>ProjectDetails</c>/<c>ProjectAcls</c> на каждую находку (#4991).
    /// </remarks>
    private async Task<ProjectIdentification> SeedProjectWithMatchesAsync(
        UserIdentification masterId,
        string projectName)
    {
        ProjectIdentification projectId;
        using (var scope = factory.Services.CreateScope())
        {
            projectId = await TestUserProjectHelpers.CreateProjectAsync(
                scope.ServiceProvider, masterId, projectName);
        }

        await factory.Services.RunAsAsync(masterId, async sp =>
        {
            var metadataRepository = sp.GetRequiredService<IProjectMetadataRepository>();
            var characterService = sp.GetRequiredService<ICharacterService>();

            var projectInfo = await metadataRepository.GetProjectMetadata(projectId);
            var nameFieldId = (projectInfo.CharacterNameField
                ?? throw new InvalidOperationException("В проекте нет поля имени персонажа"))
                .Id.ProjectFieldId;
            var rootGroupId = projectInfo.GroupTree.RootGroupId;

            var characterGroupService = sp.GetRequiredService<ICharacterGroupService>();
            var groupId = await characterGroupService.AddCharacterGroup(
                projectId,
                $"Отряд {CharacterName}",
                isPublic: true,
                parentCharacterGroupIds: [rootGroupId],
                description: "");
            _ = await characterGroupService.AddCharacterGroup(
                projectId,
                $"Тайный отряд {CharacterName}",
                isPublic: false,
                parentCharacterGroupIds: [rootGroupId],
                description: "");

            for (var i = 1; i <= CharactersPerProject; i++)
            {
                // Первый персонаж публичный и назван ровно искомой строкой: на него смотрит
                // проверка сужённого до проекта поиска. Остальные скрытые.
                var name = i == 1 ? CharacterName : $"{CharacterName} {i}";
                var visibility = i == 1 ? CharacterVisibility.Public : CharacterVisibility.Private;
                await characterService.AddCharacter(new AddCharacterRequest(
                    projectId,
                    ParentCharacterGroupIds: [groupId],
                    new CharacterTypeInfo(CharacterType.Player, IsHot: false, SlotLimit: null, SlotName: null, visibility),
                    FieldValues: new FieldLayerContainer(projectInfo, new Dictionary<int, string?> { [nameFieldId] = name })));
            }
        });

        return projectId;
    }
}
