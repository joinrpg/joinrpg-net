using System.Net;
using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.IntegrationTests.TestInfrastructure;

namespace JoinRpg.IntegrationTest.Scenarios;

public class CreateEmptyProjectScenario(JoinApplicationFactory factory) : IClassFixture<JoinApplicationFactory>
{
    [Fact]
    public async Task CreateEmptyProject_AndLoadHomePage()
    {
        // 1. Создать пользователя и проект
        using var scope = factory.Services.CreateScope();
        var userId = await TestUserProjectHelpers.CreateTestUserAsync(scope.ServiceProvider);
        var projectId = await TestUserProjectHelpers.CreateProjectAsync(scope.ServiceProvider, userId);

        // 2. Загрузить главную страницу проекта анонимно
        var client = factory.CreateClient();
        var response = await client.GetAsync($"{projectId}/home");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
