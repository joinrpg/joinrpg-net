using System.Net;
using JoinRpg.Dal.Impl;
using JoinRpg.DomainTypes;
using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.Web.ProjectMasterTools.Settings;

namespace JoinRpg.IntegrationTest.Scenarios;

/// <summary>
/// Штатный отказ сервиса в <c>/webapi/</c> доходит до острова кодом 4xx и русским текстом, а не 500-й.
/// </summary>
/// <remarks>
/// Юнит-тест <c>WebApiExceptionHandlerTest</c> строит контекст руками и пайплайна не видит. А в пайплайне
/// <c>UseExceptionHandler</c> со страницей ошибки переписывает путь запроса на <c>/error</c> до вызова
/// обработчика — и обработчик, смотревший на <c>Request.Path</c>, ни одного отказа /webapi не узнавал.
/// </remarks>
public class WebApiRefusalScenario(JoinApplicationFactory factory) : IClassFixture<JoinApplicationFactory>
{
    [Fact]
    public async Task ServiceRefusal_InArchivedProject_IsConflictWithReason()
    {
        string masterEmail;
        ProjectIdentification projectId;
        using (var scope = factory.Services.CreateScope())
        {
            (var masterId, masterEmail) = await TestUserProjectHelpers.CreateTestUserWithEmailAsync(scope.ServiceProvider);
            projectId = await TestUserProjectHelpers.CreateProjectAsync(scope.ServiceProvider, masterId, "Проект в архиве");
        }

        var client = await TestUserProjectHelpers.CreateAuthenticatedClientAsync(factory.CreateClient(), masterEmail);
        var settings = await client.GetFromJsonAsync<ProjectContactsSettingsViewModel>(
            $"webapi/{projectId.Value}/project/GetContactSettings");

        await ArchiveProject(projectId);

        var response = await client.PostAsJsonAsync($"webapi/{projectId.Value}/project/SaveContactSettings", settings);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict, await response.Content.ReadAsStringAsync());
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        (await response.Content.ReadAsStringAsync()).ShouldContain("Проект находится в архиве");
    }

    /// <summary>
    /// Несуществующий проект в /webapi — тоже ProblemDetails, а не HTML-страница 404.
    /// </summary>
    [Fact]
    public async Task ProjectNotFound_IsNotFoundProblem()
    {
        string userEmail;
        using (var scope = factory.Services.CreateScope())
        {
            (_, userEmail) = await TestUserProjectHelpers.CreateTestUserWithEmailAsync(scope.ServiceProvider);
        }
        var client = await TestUserProjectHelpers.CreateAuthenticatedClientAsync(factory.CreateClient(), userEmail);

        var response = await client.GetAsync($"webapi/{int.MaxValue}/project/GetContactSettings");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound, await response.Content.ReadAsStringAsync());
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        (await response.Content.ReadAsStringAsync()).ShouldContain("Проект не найден");
    }

    private async Task ArchiveProject(ProjectIdentification projectId)
    {
        using var scope = factory.Services.CreateScope();
        var myDb = scope.ServiceProvider.GetRequiredService<MyDbContext>();
        _ = await myDb.Database.ExecuteSqlCommandAsync(
            "UPDATE dbo.Projects SET Active = 0 WHERE ProjectId = @p0",
            projectId.Value);
    }
}
