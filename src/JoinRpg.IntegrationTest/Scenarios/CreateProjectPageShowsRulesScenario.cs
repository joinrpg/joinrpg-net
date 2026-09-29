using System.Net;
using JoinRpg.IntegrationTest.TestInfrastructure;

namespace JoinRpg.IntegrationTest.Scenarios;

/// <summary>
/// Страница создания проекта просит согласиться с правилами сайта, поэтому сами правила на ней
/// должны быть видны. Проверяется именно отрендеренный HTML: панель правил живёт в другой сборке,
/// и стоит ей выпасть из области видимости Razor, как тег уедет в разметку текстом и просто
/// не отрисуется — страница при этом останется рабочей и по-прежнему ответит 200.
/// </summary>
public class CreateProjectPageShowsRulesScenario(JoinApplicationFactory factory) : IClassFixture<JoinApplicationFactory>
{
    [Fact]
    public async Task CreateProjectPage_ShowsSiteRules()
    {
        using var scope = factory.Services.CreateScope();
        var (_, email) = await TestUserProjectHelpers.CreateTestUserWithEmailAsync(scope.ServiceProvider);

        var client = await TestUserProjectHelpers.CreateAuthenticatedClientAsync(
            factory.CreateClient(),
            email);

        var response = await client.GetAsync("game/create");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var html = await response.Content.ReadAsStringAsync();
        html.ShouldContain("Правила сайта");
    }
}
