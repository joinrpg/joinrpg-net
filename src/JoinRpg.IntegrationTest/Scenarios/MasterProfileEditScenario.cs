using System.Net;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Data.Interfaces;
using JoinRpg.DomainTypes;
using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.Services.Interfaces.ProjectAccess;
using JoinRpg.Services.Interfaces.Projects;
using JoinRpg.Web.ProjectMasterTools.Acl;
using Microsoft.AspNetCore.Mvc.Testing;

namespace JoinRpg.IntegrationTest.Scenarios;

/// <summary>
/// Профиль мастера (ADR019, §4 и §7): остров «Профиль мастера» на странице правки, форма прав, форма добавления.
/// </summary>
public class MasterProfileEditScenario(JoinApplicationFactory factory) : IClassFixture<JoinApplicationFactory>
{
    [Fact]
    public async Task MasterWithoutGrantRights_EditsOwnProfile_PermissionsKept()
    {
        var (projectId, _, masterId, masterEmail) = await CreateProjectWithMaster();
        var client = await Login(masterEmail);

        var page = await client.GetAsync($"{projectId.Value}/masters/edit?userId={masterId.Value}");
        page.StatusCode.ShouldBe(HttpStatusCode.OK);
        var html = await page.Content.ReadAsStringAsync();
        html.ShouldContain("MasterProfilePanel"); // остров профиля на странице
        html.ShouldNotContain("name=\"CanManageClaims\""); // формы прав нет — права ему не правятся

        var profile = await client.GetFromJsonAsync<MasterProfileViewModel>(ProfileUrl(projectId, "GetProfile") + $"?userId={masterId.Value}");
        profile.ShouldNotBeNull().Role.ShouldBe("Мастер");

        profile.Role = "Мастер по боёвке";
        profile.Description = "Боёвка и полигон";
        profile.IsPublic = false;
        var response = await client.PostAsJsonAsync(ProfileUrl(projectId, "SaveProfile"), profile);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var saved = await GetProfile(projectId, masterId);
        saved.Role.Value.ShouldBe("Мастер по боёвке");
        saved.Description!.Value.ShouldBe("Боёвка и полигон");
        var projectInfo = await GetProjectInfo(projectId);
        projectInfo.GetMasterById(masterId).IsPublic.ShouldBeFalse();
        projectInfo.HasMasterAccess(masterId, Permission.CanManageClaims).ShouldBeTrue();
    }

    [Fact]
    public async Task MasterWithoutGrantRights_CannotEditOthersProfile()
    {
        var (projectId, ownerId, _, masterEmail) = await CreateProjectWithMaster();
        var client = await Login(masterEmail);

        // Страница правки чужого мастера — «нет доступа» (CaptureNoAccessExceptionFilter).
        (await ReadDecoded(await client.GetAsync($"{projectId.Value}/masters/edit?userId={ownerId.Value}")))
            .ShouldContain("Нет доступа к проекту");

        // И напрямую в API: профиль не отдаётся и не сохраняется.
        (await client.GetAsync(ProfileUrl(projectId, "GetProfile") + $"?userId={ownerId.Value}"))
            .Content.Headers.ContentType?.MediaType.ShouldNotBe("application/json");
        _ = await client.PostAsJsonAsync(ProfileUrl(projectId, "SaveProfile"), new MasterProfileViewModel
        {
            ProjectId = projectId,
            UserId = ownerId,
            Role = "Взломанная роль",
            IsPublic = false,
        });
        (await GetProfile(projectId, ownerId)).Role.Value.ShouldBe("Главный мастер");
    }

    [Fact]
    public async Task MasterWithoutGrantRights_CannotPostPermissions()
    {
        var (projectId, _, masterId, masterEmail) = await CreateProjectWithMaster();
        var client = await Login(masterEmail);
        var editUrl = $"{projectId.Value}/masters/edit?userId={masterId.Value}";

        var token = await client.GetAntiforgeryTokenAsync(editUrl);
        var response = await client.PostFormAsync(editUrl, token,
            ("UserId", masterId.Value.ToString()),
            ("CanGrantRights", "true"));

        // Forbid() при cookie-аутентификации — редирект на AccessDenied, а не на список мастеров, как при успехе.
        response.Headers.Location?.ToString().ShouldContain("AccessDenied");
        (await GetProjectInfo(projectId)).HasMasterAccess(masterId, Permission.CanGrantRights).ShouldBeFalse();
    }

    [Fact]
    public async Task Granter_EditsPermissions_ProfileUntouched()
    {
        var (projectId, _, masterId, _) = await CreateProjectWithMaster();
        var client = await Login(owners[projectId]);
        var editUrl = $"{projectId.Value}/masters/edit?userId={masterId.Value}";

        // Форма прав — статический компонент: постит в экшен правки и несёт, чьи права меняются.
        var page = await ReadDecoded(await client.GetAsync(editUrl));
        page.ShouldContain($"action=\"/{projectId.Value}/masters/edit\"");
        page.ShouldContain($"name=\"UserId\" value=\"{masterId.Value}\"");

        var token = await client.GetAntiforgeryTokenAsync(editUrl);
        var response = await client.PostFormAsync(editUrl, token,
            ("UserId", masterId.Value.ToString()),
            ("CanEditRoles", "true"));
        response.StatusCode.ShouldBe(HttpStatusCode.Found);

        var projectInfo = await GetProjectInfo(projectId);
        projectInfo.HasMasterAccess(masterId, Permission.CanEditRoles).ShouldBeTrue();
        projectInfo.HasMasterAccess(masterId, Permission.CanManageClaims).ShouldBeFalse(); // чекбокса не прислали — снят
        (await GetProfile(projectId, masterId)).Role.Value.ShouldBe("Мастер");
    }

    [Fact]
    public async Task Granter_EditsOwnProfile_InArchivedProject()
    {
        // Права в архиве не меняются (ChangeAccess требует активный проект), а профиль — «титры» игры — правится.
        var (projectId, ownerId, _, _) = await CreateProjectWithMaster();
        await factory.Services.RunAsAsync(ownerId, sp =>
            sp.GetRequiredService<IProjectService>().CloseProject(projectId, publishPlot: false));
        var client = await Login(owners[projectId]);

        (await ReadDecoded(await client.GetAsync($"{projectId.Value}/masters/edit?userId={ownerId.Value}")))
            .ShouldNotContain("name=\"CanGrantRights\"");

        var response = await client.PostAsJsonAsync(ProfileUrl(projectId, "SaveProfile"), new MasterProfileViewModel
        {
            ProjectId = projectId,
            UserId = ownerId,
            Role = "Главный мастер и сценарист",
            IsPublic = true,
        });
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await GetProfile(projectId, ownerId)).Role.Value.ShouldBe("Главный мастер и сценарист");
    }

    private static string ProfileUrl(ProjectIdentification projectId, string action) => $"webapi/{projectId.Value}/master-profile/{action}";

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

    private readonly Dictionary<ProjectIdentification, string> owners = [];

    /// <summary>Проект владельца и мастер с правом вести заявки, но без права выдавать доступ.</summary>
    private async Task<(ProjectIdentification ProjectId, UserIdentification OwnerId, UserIdentification MasterId, string MasterEmail)> CreateProjectWithMaster()
    {
        using var scope = factory.Services.CreateScope();
        var (ownerId, ownerEmail) = await TestUserProjectHelpers.CreateTestUserWithEmailAsync(scope.ServiceProvider);
        var projectId = await TestUserProjectHelpers.CreateProjectAsync(scope.ServiceProvider, ownerId);
        var (masterId, masterEmail) = await TestUserProjectHelpers.CreateTestUserWithEmailAsync(scope.ServiceProvider);
        await factory.Services.RunAsAsync(ownerId, sp =>
            sp.GetRequiredService<IProjectAccessService>().GrantAccess(new GrantAccessRequest
            {
                ProjectId = projectId,
                UserId = masterId,
                Role = new("Мастер"),
                Permissions = [Permission.CanManageClaims],
            }));
        owners[projectId] = ownerEmail;
        return (projectId, ownerId, masterId, masterEmail);
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
