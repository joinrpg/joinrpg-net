using System.Net;
using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.IntegrationTests.TestInfrastructure;

namespace JoinRpg.IntegrationTest.Scenarios;

public class CreateEmptyProjectAndLoadHomePageAsOwnerScenario(JoinApplicationFactory factory) : IClassFixture<JoinApplicationFactory>
{
    [Fact]
    public async Task CreateEmptyProject_AndLoadHomePage_AsOwner_ShouldFailWith500()
    {
        // 1. Создать пользователя и получить его email
        using var scope = factory.Services.CreateScope();
        var password = "Password123!";
        var (userId, email) = await TestUserProjectHelpers.CreateTestUserWithEmailAsync(
            scope.ServiceProvider,
            password: password);

        // 2. Создать проект от имени этого пользователя
        var projectId = await TestUserProjectHelpers.CreateProjectAsync(
            scope.ServiceProvider,
            userId,
            "Тестовый проект владельца");

        // 3. Создать аутентифицированный HTTP-клиент
        var client = factory.CreateClient();
        client = await TestUserProjectHelpers.CreateAuthenticatedClientAsync(client, email, password);

        // 4. Загрузить главную страницу проекта с аутентифицированным клиентом
        var response = await client.GetAsync($"{projectId}/home");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
