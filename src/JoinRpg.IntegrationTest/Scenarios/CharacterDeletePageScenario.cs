using System.Net;
using JoinRpg.Data.Interfaces;
using JoinRpg.IntegrationTest.TestInfrastructure;

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
}
