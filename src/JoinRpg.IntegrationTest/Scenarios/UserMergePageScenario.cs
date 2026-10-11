using System.Net;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Dal.Impl;
using JoinRpg.Data.Interfaces;
using JoinRpg.DomainTypes;
using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.Services.Interfaces.ProjectAccess;
using JoinRpg.Web.AdminTools.UserMerge;
using Microsoft.AspNetCore.Mvc.Testing;

namespace JoinRpg.IntegrationTest.Scenarios;

/// <summary>
/// Страница слияния аккаунтов (ADR024): остров, предпросмотр и слияние через web API. Модели
/// проходят через JSON так же, как в WASM-острове.
/// </summary>
public class UserMergePageScenario(JoinApplicationFactory factory) : IClassFixture<JoinApplicationFactory>
{
    private const string PageUrl = "admin/user-merge";

    [Fact]
    public async Task Admin_SeesPage()
    {
        var client = await LoginAsAdmin();

        var page = await client.GetAsync(PageUrl);

        page.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await page.Content.ReadAsStringAsync()).ShouldContain("UserMergePanel");
    }

    [Fact]
    public async Task NotAdmin_NoAccess()
    {
        var (_, email) = await CreateUser();
        var client = await TestUserProjectHelpers.CreateAuthenticatedClientAsync(
            factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false }), email, followsRedirects: false);

        (await client.GetAsync(PageUrl)).StatusCode.ShouldNotBe(HttpStatusCode.OK);
        (await client.GetAsync("webapi/UserMerge/GetPreview?primaryUserLink=1&secondaryUserLink=2")).StatusCode.ShouldNotBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task NotAdmin_CannotMerge()
    {
        var (a, email) = await CreateUser();
        var (b, _) = await CreateUser();
        var client = await TestUserProjectHelpers.CreateAuthenticatedClientAsync(
            factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false }), email, followsRedirects: false);

        var response = await client.PostWithTokenHeaderAsync($"webapi/UserMerge/Merge?primary={a.Value}&secondary={b.Value}", null);

        response.StatusCode.ShouldBeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.Unauthorized);
        await ShouldExist(b);
    }

    [Fact]
    public async Task Merge_WithConflict_BadRequest_NothingDeleted()
    {
        var (a, _) = await CreateUser();
        var (b, _) = await CreateUser();
        await MakeBothMasters(a, b);
        var client = await LoginAsAdmin();

        var response = await client.PostWithTokenHeaderAsync($"webapi/UserMerge/Merge?primary={a.Value}&secondary={b.Value}", null);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        await ShouldExist(b);
    }

    [Fact]
    public async Task Preview_ShowsConflictWithProject()
    {
        var (a, aEmail) = await CreateUser();
        var (b, bEmail) = await CreateUser();
        var projectId = await MakeBothMasters(a, b);
        var client = await LoginAsAdmin();

        var preview = await GetPreview(client, aEmail, bEmail);

        preview.Primary.User.UserId.ShouldBe(a);
        preview.Secondary.Email.ShouldBe(bEmail);
        var conflict = preview.Conflicts.ShouldHaveSingleItem();
        conflict.Kind.ShouldBe(UserMergeConflictKind.BothMasters);
        conflict.Project.ShouldNotBeNull().ProjectId.ShouldBe(projectId);
    }

    [Fact]
    public async Task Preview_ShowsBlockedProject()
    {
        var (_, aEmail) = await CreateUser();
        var (b, bEmail) = await CreateUser();
        ProjectIdentification projectId;
        using (var scope = factory.Services.CreateScope())
        {
            projectId = await TestUserProjectHelpers.CreateProjectAsync(scope.ServiceProvider, b);
        }
        var client = await LoginAsAdmin();

        // Блокировка любого проекта запрещает слияние всем — снимаем её, чтобы не ломать соседние тесты класса.
        await SetBlocked(projectId, true);
        try
        {
            var preview = await GetPreview(client, aEmail, bEmail);

            var conflict = preview.Conflicts.ShouldHaveSingleItem();
            conflict.Kind.ShouldBe(UserMergeConflictKind.BlockedProject);
            conflict.Project.ShouldNotBeNull().ProjectId.ShouldBe(projectId);
        }
        finally
        {
            await SetBlocked(projectId, false);
        }
    }

    private async Task SetBlocked(ProjectIdentification projectId, bool blocked)
    {
        using var scope = factory.Services.CreateScope();
        _ = await scope.ServiceProvider.GetRequiredService<MyDbContext>().Database
            .ExecuteSqlCommandAsync("UPDATE [Projects] SET [IsBlocked] = @p0 WHERE [ProjectId] = @p1", blocked, projectId.Value);
    }

    [Fact]
    public async Task Preview_UnknownUser_BadRequest()
    {
        var (_, aEmail) = await CreateUser();
        var client = await LoginAsAdmin();

        var response = await client.GetAsync(PreviewUrl(aEmail, "nobody-at-all@example.com"));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Merge_ThroughApi()
    {
        var (a, aEmail) = await CreateUser();
        var (b, bEmail) = await CreateUser();
        var client = await LoginAsAdmin();
        var preview = await GetPreview(client, aEmail, bEmail);
        preview.Conflicts.ShouldBeEmpty();

        var token = await client.GetAntiforgeryTokenAsync(PageUrl);
        var response = await client.PostWithTokenHeaderAsync($"webapi/UserMerge/Merge?primary={a.Value}&secondary={b.Value}", token);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var scope = factory.Services.CreateScope();
        (await scope.ServiceProvider.GetRequiredService<IUserRepository>().GetUserInfo(b)).ShouldBeNull();
    }

    private async Task<ProjectIdentification> MakeBothMasters(UserIdentification a, UserIdentification b)
    {
        ProjectIdentification projectId;
        using (var scope = factory.Services.CreateScope())
        {
            projectId = await TestUserProjectHelpers.CreateProjectAsync(scope.ServiceProvider, a);
        }
        await factory.Services.RunAsAsync(a, sp =>
            sp.GetRequiredService<IProjectAccessService>().GrantAccess(new GrantAccessRequest
            {
                ProjectId = projectId,
                UserId = b,
                Role = new("Мастер"),
                Permissions = [],
            }));
        return projectId;
    }

    private async Task ShouldExist(UserIdentification userId)
    {
        using var scope = factory.Services.CreateScope();
        (await scope.ServiceProvider.GetRequiredService<IUserRepository>().GetUserInfo(userId)).ShouldNotBeNull();
    }

    private static string PreviewUrl(string primary, string secondary)
        => $"webapi/UserMerge/GetPreview?primaryUserLink={Uri.EscapeDataString(primary)}&secondaryUserLink={Uri.EscapeDataString(secondary)}";

    private static async Task<UserMergePreviewViewModel> GetPreview(HttpClient client, string primary, string secondary)
    {
        var response = await client.GetAsync(PreviewUrl(primary, secondary));
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<UserMergePreviewViewModel>()).ShouldNotBeNull();
    }

    private async Task<(UserIdentification UserId, string Email)> CreateUser()
    {
        using var scope = factory.Services.CreateScope();
        return await TestUserProjectHelpers.CreateTestUserWithEmailAsync(scope.ServiceProvider);
    }

    private async Task<HttpClient> LoginAsAdmin()
    {
        var (_, email) = await CreateUser();
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();
            db.Set<JoinRpg.DataModel.User>().Include("Auth").Single(u => u.Email == email).Auth.IsAdmin = true;
            _ = await db.SaveChangesAsync();
        }
        return await TestUserProjectHelpers.CreateAuthenticatedClientAsync(
            factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false }), email, followsRedirects: false);
    }
}
