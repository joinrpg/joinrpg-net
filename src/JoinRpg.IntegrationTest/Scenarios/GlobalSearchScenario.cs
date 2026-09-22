using System.Net;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Data.Interfaces;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.IntegrationTests.TestInfrastructure;
using JoinRpg.Services.Interfaces.Characters;
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

    [Fact]
    public async Task GlobalSearch_WithoutProjectScope_Works()
    {
        var (masterId, email, projectId) = await CreateMasterWithProjectAndCharacterAsync();

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

    private async Task<(UserIdentification masterId, string email, ProjectIdentification projectId)> CreateMasterWithProjectAndCharacterAsync()
    {
        UserIdentification masterId;
        string email;
        ProjectIdentification projectId;

        using (var scope = factory.Services.CreateScope())
        {
            (masterId, email) = await TestUserProjectHelpers.CreateTestUserWithEmailAsync(
                scope.ServiceProvider, password: Password);
            projectId = await TestUserProjectHelpers.CreateProjectAsync(
                scope.ServiceProvider, masterId, "Проект для глобального поиска");
        }

        await factory.Services.RunAsAsync(masterId, async sp =>
        {
            var metadataRepository = sp.GetRequiredService<IProjectMetadataRepository>();
            var characterService = sp.GetRequiredService<ICharacterService>();

            var projectInfo = await metadataRepository.GetProjectMetadata(projectId);
            var nameFieldId = (projectInfo.CharacterNameField
                ?? throw new InvalidOperationException("В проекте нет поля имени персонажа"))
                .Id.ProjectFieldId;

            await characterService.AddCharacter(new AddCharacterRequest(
                projectId,
                ParentCharacterGroupIds: [projectInfo.RootCharacterGroupId],
                new CharacterTypeInfo(CharacterType.Player, IsHot: false, SlotLimit: null, SlotName: null, CharacterVisibility.Public),
                FieldValues: new FieldLayerContainer(projectInfo, new Dictionary<int, string?> { [nameFieldId] = CharacterName })));
        });

        return (masterId, email, projectId);
    }
}
