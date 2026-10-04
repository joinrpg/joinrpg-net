using System.Net;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Data.Interfaces;
using JoinRpg.DomainTypes;
using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.Services.Interfaces.ProjectAccess;
using Microsoft.AspNetCore.Mvc.Testing;

namespace JoinRpg.IntegrationTest.Scenarios;

/// <summary>
/// Форма добавления мастера (ADR019, §4): статический Razor-компонент, постит в MVC-экшен.
/// </summary>
public class MasterAddScenario(JoinApplicationFactory factory) : IClassFixture<JoinApplicationFactory>
{
    [Fact]
    public async Task AddMaster_WithEmptyRole_ShowsFormAgain()
    {
        var (projectId, ownerEmail) = await CreateProject();
        var newMasterId = await CreateUser();
        var client = await Login(ownerEmail);
        var addUrl = $"{projectId.Value}/masters/add/{newMasterId.Value}";

        var token = await client.GetAntiforgeryTokenAsync(addUrl);
        var response = await client.PostFormAsync(addUrl, token,
            ("UserId", newMasterId.Value.ToString()),
            ("Role", ""),
            ("CanManageClaims", "true"));

        response.StatusCode.ShouldBe(HttpStatusCode.OK); // форма с ошибкой, а не редирект на список
        var html = await ReadDecoded(response);
        html.ShouldContain("Укажите роль мастера в проекте");
        html.ShouldContain("id=\"CanManageClaims\" name=\"CanManageClaims\" value=\"true\" checked"); // присланное не потерялось
        (await GetProjectInfo(projectId)).HasMasterAccess(newMasterId).ShouldBeFalse();
    }

    [Fact]
    public async Task AddMaster_WithFullProfile()
    {
        var (projectId, ownerEmail) = await CreateProject();
        var newMasterId = await CreateUser();
        var client = await Login(ownerEmail);
        var addUrl = $"{projectId.Value}/masters/add/{newMasterId.Value}";

        var token = await client.GetAntiforgeryTokenAsync(addUrl);
        var response = await client.PostFormAsync(addUrl, token,
            ("UserId", newMasterId.Value.ToString()),
            ("Role", "Мастер по экономике"),
            ("Description", "Взносы и касса"),
            // IsPublic не прислан — чекбокс снят
            ("CanManageClaims", "true"));
        response.StatusCode.ShouldBe(HttpStatusCode.Found);

        var profile = await GetProfile(projectId, newMasterId);
        profile.Role.Value.ShouldBe("Мастер по экономике");
        profile.Description!.Value.ShouldBe("Взносы и касса");
        var projectInfo = await GetProjectInfo(projectId);
        projectInfo.GetMasterById(newMasterId).IsPublic.ShouldBeFalse();
        projectInfo.HasMasterAccess(newMasterId, Permission.CanManageClaims).ShouldBeTrue();
    }

    [Fact]
    public async Task AddFormerMaster_FormPrefilledWithFormerProfile()
    {
        var (projectId, ownerEmail) = await CreateProject();
        var ownerId = await GetOwnerId(projectId);
        var formerId = await CreateUser();
        await factory.Services.RunAsAsync(ownerId, sp =>
            sp.GetRequiredService<IProjectAccessService>().GrantAccess(new GrantAccessRequest
            {
                ProjectId = projectId,
                UserId = formerId,
                Role = new("Мастер по боёвке"),
                Description = new MarkdownString("Боёвка и полигон"),
                IsPublic = false,
                Permissions = [Permission.CanManageClaims],
            }));
        await factory.Services.RunAsAsync(ownerId, sp =>
            sp.GetRequiredService<IProjectAccessService>().RemoveAccess(projectId, formerId, newResponsibleMasterId: null));

        var client = await Login(ownerEmail);
        var html = await ReadDecoded(await client.GetAsync($"{projectId.Value}/masters/add/{formerId.Value}"));

        html.ShouldContain("value=\"Мастер по боёвке\"");
        html.ShouldContain("Боёвка и полигон</textarea>");
        html.ShouldNotContain("id=\"IsPublic\" name=\"IsPublic\" value=\"true\" checked");
    }

    private async Task<ProjectMasterProfileDto> GetProfile(ProjectIdentification projectId, UserIdentification userId)
    {
        using var scope = factory.Services.CreateScope();
        return (await scope.ServiceProvider.GetRequiredService<IProjectMasterProfileRepository>().GetMasterProfiles(projectId))
            .Single(p => p.UserId == userId);
    }

    private async Task<ProjectInfo> GetProjectInfo(ProjectIdentification projectId)
    {
        using var scope = factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<IProjectMetadataRepository>().GetProjectMetadata(projectId);
    }

    private async Task<UserIdentification> GetOwnerId(ProjectIdentification projectId)
        => (await GetProjectInfo(projectId)).Masters.Single(m => m.IsOwner).UserId;

    private async Task<UserIdentification> CreateUser()
    {
        using var scope = factory.Services.CreateScope();
        return await TestUserProjectHelpers.CreateTestUserAsync(scope.ServiceProvider);
    }

    private async Task<(ProjectIdentification ProjectId, string OwnerEmail)> CreateProject()
    {
        using var scope = factory.Services.CreateScope();
        var (ownerId, ownerEmail) = await TestUserProjectHelpers.CreateTestUserWithEmailAsync(scope.ServiceProvider);
        var projectId = await TestUserProjectHelpers.CreateProjectAsync(scope.ServiceProvider, ownerId);
        return (projectId, ownerEmail);
    }

    // Динамический текст Razor кодирует кириллицу сущностями (&#x423;...) — сравниваем по раскодированному.
    private static async Task<string> ReadDecoded(HttpResponseMessage response)
        => WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

    private async Task<HttpClient> Login(string email)
        => await TestUserProjectHelpers.CreateAuthenticatedClientAsync(
            factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false }),
            email,
            followsRedirects: false);
}
