using System.Net;
using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Characters;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.Services.Interfaces.Characters;

namespace JoinRpg.IntegrationTest.Scenarios;

/// <summary>
/// Страница удаления персонажа, который служит шаблоном по умолчанию.
/// </summary>
/// <remarks>
/// Смоук открывает удаление на персонаже с активными заявками, и страница останавливается на
/// первой проверке — до сравнения с шаблоном по умолчанию не доходит. Здесь берётся шаблон
/// свежего проекта-ролёвки: заявок на нём нет, поэтому код идёт по длинному пути и читает
/// настройки проекта (#5112).
/// </remarks>
public class CharacterDeletePageScenario(JoinApplicationFactory factory) : IClassFixture<JoinApplicationFactory>
{
    [Fact]
    public async Task DeletePage_ForDefaultTemplate_ExplainsWhyItCannotBeDeleted()
    {
        using var scope = factory.Services.CreateScope();
        var (userId, email) = await TestUserProjectHelpers.CreateTestUserWithEmailAsync(scope.ServiceProvider);
        var projectId = await TestUserProjectHelpers.CreateProjectAsync(scope.ServiceProvider, userId);

        var projectInfo = await scope.ServiceProvider.GetRequiredService<IProjectMetadataRepository>()
            .GetProjectMetadata(projectId, ignoreCache: true);
        var template = projectInfo.ClaimSettings.DefaultTemplate.ShouldNotBeNull();

        var client = await TestUserProjectHelpers.CreateAuthenticatedClientAsync(factory.CreateClient(), email);

        var response = await client.GetAsync($"{projectId.Value}/character/{template.CharacterId}/delete");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();
        html.ShouldContain("является шаблоном по умолчанию");
    }

    /// <summary>
    /// Экшены удаления принимают <see cref="CharacterIdentification"/>, собранный из маршрута:
    /// форма подтверждения шлёт только antiforgery-токен.
    /// </summary>
    [Fact]
    public async Task DeleteForm_DeletesCharacter()
    {
        using var scope = factory.Services.CreateScope();
        var (userId, email) = await TestUserProjectHelpers.CreateTestUserWithEmailAsync(scope.ServiceProvider);
        var projectId = await TestUserProjectHelpers.CreateProjectAsync(scope.ServiceProvider, userId);

        var characterId = await factory.Services.RunAsAsync(userId, async sp =>
        {
            var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>().GetProjectMetadata(projectId);
            return await sp.GetRequiredService<ICharacterService>().AddCharacter(new AddCharacterRequest(
                projectId,
                ParentCharacterGroupIds: [],
                new CharacterTypeInfo(CharacterType.Player, IsHot: false, SlotLimit: null, SlotName: null, CharacterVisibility.Public),
                FieldValues: FieldLayerContainer.Empty(projectInfo)));
        });

        var client = await TestUserProjectHelpers.CreateAuthenticatedClientAsync(factory.CreateClient(), email);
        var url = $"{projectId.Value}/character/{characterId.CharacterId}/delete";
        var token = await client.GetAntiforgeryTokenAsync(url);

        var response = await client.PostFormAsync(url, token);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var readScope = factory.Services.CreateScope();
        var character = await readScope.ServiceProvider.GetRequiredService<ICharacterInfoRepository>()
            .GetCharacterInfoOrDefault(characterId);
        // Неиспользованный персонаж может удаляться физически, использованный — деактивируется.
        (character?.IsActive ?? false).ShouldBeFalse();
    }
}
