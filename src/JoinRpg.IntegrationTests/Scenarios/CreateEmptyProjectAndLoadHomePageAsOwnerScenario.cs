using System.Net;
using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.IntegrationTests.TestInfrastructure;

namespace JoinRpg.IntegrationTest.Scenarios;

public class CreateEmptyProjectAndLoadHomePageAsOwnerScenario(JoinApplicationFactory factory) : IClassFixture<JoinApplicationFactory>
{
    [Fact]
    public async Task CreateEmptyProject_AndLoadHomePage_AsOwner()
    {
        using var scope = factory.Services.CreateScope();
        var password = "Password123!";
        var (userId, email) = await TestUserProjectHelpers.CreateTestUserWithEmailAsync(
            scope.ServiceProvider,
            password: password);

        var projectId = await TestUserProjectHelpers.CreateProjectAsync(
            scope.ServiceProvider,
            userId,
            "Тестовый проект владельца");

        var client = factory.CreateClient();
        client = await TestUserProjectHelpers.CreateAuthenticatedClientAsync(client, email, password);

        var response = await client.GetAsync($"{projectId}/home");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateEmptyProjectTwice_AndLoadHomePage_AsOwner()
    {
        // 1. Создать пользователя и получить его email
        using var scope = factory.Services.CreateScope();
        var password = "Password123!";
        var (userId, email) = await TestUserProjectHelpers.CreateTestUserWithEmailAsync(
            scope.ServiceProvider,
            password: password);

        var projectId = await TestUserProjectHelpers.CreateProjectAsync(
            scope.ServiceProvider,
            userId,
            "Тестовый проект владельца");

        _ = await TestUserProjectHelpers.CreateProjectAsync(
            scope.ServiceProvider,
            userId,
            "Второй тестовый проект");

        // Наличие у пользователя хотя бы двух проектов — более частый сценарий. Был баг, где это падало

        var client = factory.CreateClient();
        client = await TestUserProjectHelpers.CreateAuthenticatedClientAsync(client, email, password);

        var response = await client.GetAsync($"{projectId}/home");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
