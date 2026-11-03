using System.Net;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.DomainTypes;
using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.Services.Interfaces.Projects;
using Microsoft.AspNetCore.Mvc.Testing;

namespace JoinRpg.IntegrationTest.Scenarios;

/// <summary>
/// Страницы с [ProjectShouldBeActive] на заблокированном/архивном проекте вместо 500 показывают
/// «Проект заблокирован»/«Проект находится в архиве», а [ProjectShouldNotBeArchived] в блокировке по-прежнему пускает (#5983).
/// </summary>
public class BlockedProjectErrorPageScenario(JoinApplicationFactory factory) : IClassFixture<JoinApplicationFactory>
{
    [Fact]
    public async Task InvitePlayer_InBlockedProject_ShowsBlockedPage()
    {
        var (projectId, _, ownerEmail) = await CreateProjectWithOwner();
        await TestUserProjectHelpers.BlockProjectAsync(factory.Services, projectId);
        var client = await Login(ownerEmail);

        var response = await client.GetAsync($"{projectId.Value}/claims/invite-player");

        response.StatusCode.ShouldBe(HttpStatusCode.OK); // до #5983 был 500
        var html = await ReadDecoded(response);
        // Тело ErrorNotActiveProject: в меню мастера тоже есть «Проект заблокирован» (ProjectStatusLabel), поэтому проверяем текст ошибки целиком.
        html.ShouldContain("изменения в нём сейчас невозможны");
    }

    [Fact]
    public async Task InvitePlayer_InArchivedProject_ShowsArchivedPage()
    {
        var (projectId, ownerId, ownerEmail) = await CreateProjectWithOwner();
        await factory.Services.RunAsAsync(ownerId, sp =>
            sp.GetRequiredService<IProjectService>().CloseProject(projectId, publishPlot: false));
        var client = await Login(ownerEmail);

        var response = await client.GetAsync($"{projectId.Value}/claims/invite-player");

        response.StatusCode.ShouldBe(HttpStatusCode.OK); // до #5983 был 500
        var html = await ReadDecoded(response);
        html.ShouldContain("Эта функция не доступна для неактивных проектов");
    }

    [Fact]
    public async Task RolesListEdit_InBlockedProject_StillAvailable()
    {
        var (projectId, _, ownerEmail) = await CreateProjectWithOwner();
        await TestUserProjectHelpers.BlockProjectAsync(factory.Services, projectId);
        var client = await Login(ownerEmail);

        var response = await client.GetAsync($"{projectId.Value}/tools/roleslist/edit");

        // [ProjectShouldNotBeArchived] (AllowBlocked) блокировку пропускает — страница открывается, а не страница ошибки.
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var html = await ReadDecoded(response);
        html.ShouldContain("Создание сетки ролей");
        html.ShouldNotContain("изменения в нём сейчас невозможны");
    }

    private async Task<(ProjectIdentification projectId, UserIdentification ownerId, string ownerEmail)> CreateProjectWithOwner()
    {
        using var scope = factory.Services.CreateScope();
        var (ownerId, ownerEmail) = await TestUserProjectHelpers.CreateTestUserWithEmailAsync(scope.ServiceProvider);
        var projectId = await TestUserProjectHelpers.CreateProjectAsync(scope.ServiceProvider, ownerId, "Проект для страниц ошибок");
        return (projectId, ownerId, ownerEmail);
    }

    private async Task<HttpClient> Login(string email)
        => await TestUserProjectHelpers.CreateAuthenticatedClientAsync(
            factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false }),
            email,
            followsRedirects: false);

    // Динамический текст Razor кодирует кириллицу сущностями (&#x423;...) — сравниваем по раскодированному.
    private static async Task<string> ReadDecoded(HttpResponseMessage response)
        => WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
}
