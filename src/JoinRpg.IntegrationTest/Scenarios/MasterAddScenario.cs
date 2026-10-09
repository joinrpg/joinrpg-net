using System.Net;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Data.Interfaces;
using JoinRpg.DomainTypes;
using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.Services.Interfaces.ProjectAccess;
using JoinRpg.Web.ProjectMasterTools.Acl;
using Microsoft.AspNetCore.Mvc.Testing;

namespace JoinRpg.IntegrationTest.Scenarios;

/// <summary>
/// Добавление мастера (ADR019, §4): остров AddMasterPanel, профиль и права — через API.
/// </summary>
public class MasterAddScenario(JoinApplicationFactory factory) : IClassFixture<JoinApplicationFactory>
{
    [Fact]
    public async Task AddPage_HostsAddMasterPanel()
    {
        var (projectId, ownerEmail) = await CreateProject();
        var newMasterId = await CreateUser();
        var client = await Login(ownerEmail);

        var page = await client.GetAsync($"{projectId.Value}/masters/add/{newMasterId.Value}");

        page.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await page.Content.ReadAsStringAsync()).ShouldContain("AddMasterPanel");
    }

    [Fact]
    public async Task NewMaster_FormPrefilledWithDefaultRole()
    {
        var (projectId, ownerEmail) = await CreateProject();
        var newMasterId = await CreateUser();
        var client = await Login(ownerEmail);

        var model = await GetAddMaster(client, projectId, newMasterId);

        model.Role.ShouldBe(AddMasterViewModel.DefaultRole);
        model.Description.ShouldBe("");
        model.IsPublic.ShouldBeTrue();
        model.Permissions.ShouldAllBe(p => !p.Value);
    }

    [Fact]
    public async Task AddMaster_WithEmptyRole_Rejected()
    {
        var (projectId, ownerEmail) = await CreateProject();
        var newMasterId = await CreateUser();
        var client = await Login(ownerEmail);
        var model = await GetAddMaster(client, projectId, newMasterId);
        model.Role = "";

        var response = await client.PostAsJsonAsync(AccessUrl(projectId, "AddMaster"), model);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await GetProjectInfo(projectId)).HasMasterAccess(newMasterId).ShouldBeFalse();
    }

    [Fact]
    public async Task AddMaster_WithFullProfile()
    {
        var (projectId, ownerEmail) = await CreateProject();
        var newMasterId = await CreateUser();
        var client = await Login(ownerEmail);
        var model = await GetAddMaster(client, projectId, newMasterId);
        model.Role = "Мастер по экономике";
        model.Description = "Взносы и касса";
        model.IsPublic = false;
        model.Permissions.Single(p => p.Permission == Permission.CanManageClaims).Value = true;

        var response = await client.PostAsJsonAsync(AccessUrl(projectId, "AddMaster"), model);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var profile = await GetProfile(projectId, newMasterId);
        profile.Role.Value.ShouldBe("Мастер по экономике");
        profile.Description!.Value.ShouldBe("Взносы и касса");
        var projectInfo = await GetProjectInfo(projectId);
        projectInfo.GetMasterById(newMasterId).IsPublic.ShouldBeFalse();
        projectInfo.HasMasterAccess(newMasterId, Permission.CanManageClaims).ShouldBeTrue();
        projectInfo.HasMasterAccess(newMasterId, Permission.CanGrantRights).ShouldBeFalse();
    }

    [Fact]
    public async Task FormerMaster_ReturnedFromEditPage_WithFormerProfile()
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

        // «Добавить» бывшего ведёт на страницу правки: там только права, профиль вернётся прежним (ADR019, §1).
        var add = await client.GetAsync($"{projectId.Value}/masters/add/{formerId.Value}");
        add.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        var editUrl = add.Headers.Location.ShouldNotBeNull().ToString();
        editUrl.ShouldContain($"/{projectId.Value}/masters/edit?userId={formerId.Value}");
        var page = await (await client.GetAsync(editUrl)).Content.ReadAsStringAsync();
        page.ShouldContain("MasterPermissionsPanel");
        page.ShouldNotContain("MasterProfilePanel");

        var permissions = (await client.GetFromJsonAsync<MasterPermissionsViewModel>(
            AccessUrl(projectId, "GetPermissions") + $"?userId={formerId.Value}")).ShouldNotBeNull();
        permissions.IsFormerMaster.ShouldBeTrue();
        permissions.Permissions.Single(p => p.Permission == Permission.CanEditRoles).Value = true;
        (await client.PostAsJsonAsync(AccessUrl(projectId, "SavePermissions"), permissions)).StatusCode.ShouldBe(HttpStatusCode.OK);

        var projectInfo = await GetProjectInfo(projectId);
        projectInfo.HasMasterAccess(formerId, Permission.CanEditRoles).ShouldBeTrue();
        projectInfo.GetMasterById(formerId).IsPublic.ShouldBeFalse();
        var profile = await GetProfile(projectId, formerId);
        profile.Role.Value.ShouldBe("Мастер по боёвке");
        profile.Description!.Value.ShouldBe("Боёвка и полигон");
    }

    [Fact]
    public async Task AddPage_ForActiveMaster_RedirectsToEdit()
    {
        var (projectId, ownerEmail) = await CreateProject();
        var ownerId = await GetOwnerId(projectId);
        var client = await Login(ownerEmail);

        var add = await client.GetAsync($"{projectId.Value}/masters/add/{ownerId.Value}");

        add.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        add.Headers.Location.ShouldNotBeNull().ToString().ShouldContain($"/{projectId.Value}/masters/edit?userId={ownerId.Value}");
    }

    [Fact]
    public async Task MasterWithoutGrantRights_CannotAddMaster()
    {
        var (projectId, ownerEmail) = await CreateProject();
        var ownerId = await GetOwnerId(projectId);
        var (masterId, masterEmail) = await CreateUserWithEmail();
        await factory.Services.RunAsAsync(ownerId, sp =>
            sp.GetRequiredService<IProjectAccessService>().GrantAccess(new GrantAccessRequest
            {
                ProjectId = projectId,
                UserId = masterId,
                Role = new("Мастер"),
                Permissions = [Permission.CanManageClaims],
            }));
        var newMasterId = await CreateUser();
        var client = await Login(masterEmail);

        var response = await client.PostAsJsonAsync(AccessUrl(projectId, "AddMaster"), new AddMasterViewModel
        {
            ProjectId = projectId,
            UserId = newMasterId,
            Role = "Мастер",
            Permissions = [],
        });

        // Отказ атрибута авторизации, а не сервиса (тот дал бы 500).
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await client.GetAsync(AccessUrl(projectId, "GetAddMaster") + $"?userId={newMasterId.Value}"))
            .StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await GetProjectInfo(projectId)).HasMasterAccess(newMasterId).ShouldBeFalse();
    }

    [Fact]
    public async Task AddMaster_WithEmptyDescription_StoredWithoutDescription()
    {
        var (projectId, ownerEmail) = await CreateProject();
        var newMasterId = await CreateUser();
        var client = await Login(ownerEmail);
        var model = await GetAddMaster(client, projectId, newMasterId);
        model.Description = "   ";

        (await client.PostAsJsonAsync(AccessUrl(projectId, "AddMaster"), model)).StatusCode.ShouldBe(HttpStatusCode.OK);

        (await GetProfile(projectId, newMasterId)).Description.ShouldBeNull();
    }

    [Fact]
    public async Task AddMaster_ProjectFromBodyIgnored()
    {
        var (projectId, ownerEmail) = await CreateProject();
        var (otherProjectId, _) = await CreateProject();
        var newMasterId = await CreateUser();
        var client = await Login(ownerEmail);
        var model = await GetAddMaster(client, projectId, newMasterId);
        model.ProjectId = otherProjectId; // права есть в своём проекте, а в теле — чужой

        (await client.PostAsJsonAsync(AccessUrl(projectId, "AddMaster"), model)).StatusCode.ShouldBe(HttpStatusCode.OK);

        (await GetProjectInfo(projectId)).HasMasterAccess(newMasterId).ShouldBeTrue();
        (await GetProjectInfo(otherProjectId)).HasMasterAccess(newMasterId).ShouldBeFalse();
    }

    private static string AccessUrl(ProjectIdentification projectId, string action) => $"webapi/{projectId.Value}/master-access/{action}";

    private static async Task<AddMasterViewModel> GetAddMaster(HttpClient client, ProjectIdentification projectId, UserIdentification userId)
        => (await client.GetFromJsonAsync<AddMasterViewModel>(AccessUrl(projectId, "GetAddMaster") + $"?userId={userId.Value}")).ShouldNotBeNull();

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

    private async Task<(UserIdentification UserId, string Email)> CreateUserWithEmail()
    {
        using var scope = factory.Services.CreateScope();
        return await TestUserProjectHelpers.CreateTestUserWithEmailAsync(scope.ServiceProvider);
    }

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

    private async Task<HttpClient> Login(string email)
        => await TestUserProjectHelpers.CreateAuthenticatedClientAsync(
            factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false }),
            email,
            followsRedirects: false);
}
