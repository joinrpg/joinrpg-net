using System.Net;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Dal.Impl;
using JoinRpg.DomainTypes;
using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.Services.Interfaces.ProjectAccess;
using Microsoft.AspNetCore.Mvc.Testing;

namespace JoinRpg.IntegrationTest.Scenarios;

/// <summary>
/// Страница мастеров открыта всем (ADR019): немастер видит список мастеров, но не права и не управление.
/// </summary>
public class MastersPageAccessScenario(JoinApplicationFactory factory) : IClassFixture<JoinApplicationFactory>
{
    private const string MasterOnlyMarker = "Права доступа";

    [Fact]
    public async Task Anonymous_SeesMastersList_WithoutManagement()
    {
        using var scope = factory.Services.CreateScope();
        var (ownerId, _) = await TestUserProjectHelpers.CreateTestUserWithEmailAsync(scope.ServiceProvider);
        var projectId = await TestUserProjectHelpers.CreateProjectAsync(scope.ServiceProvider, ownerId);

        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var html = await GetMastersPage(client, projectId);

        html.ShouldContain("class=\"join-user\"");
        html.ShouldNotContain(MasterOnlyMarker);
    }

    [Fact]
    public async Task AuthenticatedNonMaster_SeesMastersList_WithoutManagement()
    {
        using var scope = factory.Services.CreateScope();
        var (ownerId, _) = await TestUserProjectHelpers.CreateTestUserWithEmailAsync(scope.ServiceProvider);
        var projectId = await TestUserProjectHelpers.CreateProjectAsync(scope.ServiceProvider, ownerId);
        var (_, playerEmail) = await TestUserProjectHelpers.CreateTestUserWithEmailAsync(scope.ServiceProvider);

        var client = await TestUserProjectHelpers.CreateAuthenticatedClientAsync(
            factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false }),
            playerEmail,
            followsRedirects: false);

        var html = await GetMastersPage(client, projectId);

        html.ShouldContain("class=\"join-user\"");
        html.ShouldNotContain(MasterOnlyMarker);
    }

    [Fact]
    public async Task Master_SeesManagement()
    {
        using var scope = factory.Services.CreateScope();
        var (ownerId, ownerEmail) = await TestUserProjectHelpers.CreateTestUserWithEmailAsync(scope.ServiceProvider);
        var projectId = await TestUserProjectHelpers.CreateProjectAsync(scope.ServiceProvider, ownerId);

        var client = await TestUserProjectHelpers.CreateAuthenticatedClientAsync(
            factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false }),
            ownerEmail,
            followsRedirects: false);

        var html = await GetMastersPage(client, projectId);

        html.ShouldContain(MasterOnlyMarker);
    }

    [Fact]
    public async Task AdminNotMaster_SeesManagement()
    {
        // Регрессия: админ сайта, не будучи мастером проекта, проходил авторизацию и падал на
        // Masters.Single(текущий пользователь) в MastersListViewModel.
        using var scope = factory.Services.CreateScope();
        var (ownerId, _) = await TestUserProjectHelpers.CreateTestUserWithEmailAsync(scope.ServiceProvider);
        var projectId = await TestUserProjectHelpers.CreateProjectAsync(scope.ServiceProvider, ownerId);
        var (_, adminEmail) = await TestUserProjectHelpers.CreateTestUserWithEmailAsync(scope.ServiceProvider);

        var myDb = scope.ServiceProvider.GetRequiredService<MyDbContext>();
        var dbAdmin = myDb.Set<JoinRpg.DataModel.User>().Include("Auth").Single(u => u.Email == adminEmail);
        dbAdmin.Auth.IsAdmin = true;
        await myDb.SaveChangesAsync();

        var client = await TestUserProjectHelpers.CreateAuthenticatedClientAsync(
            factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false }),
            adminEmail,
            followsRedirects: false);

        var html = await GetMastersPage(client, projectId);

        html.ShouldContain(MasterOnlyMarker);
    }

    [Theory]
    [InlineData("masters")]
    [InlineData("home")]
    [InlineData("Account/AccessDenied?projectId={0}")] // панель «нет доступа»: к кому обратиться за правами
    public async Task Anonymous_DoesNotSeeNonPublicMaster(string page)
    {
        // ADR019, §3: непубличного мастера видят только мастера проекта.
        var (projectId, _, _) = await CreateProjectWithHiddenMaster();

        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var html = await GetPage(client, projectId, page);

        // Один пользователь в списке — публичный владелец; скрытого мастера нет.
        CountUserLinks(html).ShouldBe(1);
    }

    [Fact]
    public async Task Owner_SeesNonPublicMaster_OnProjectHome()
    {
        // Позитивный контроль к тесту выше: фильтр завязан на смотрящего, а не прячет всех непубличных подряд.
        var (projectId, _, ownerEmail) = await CreateProjectWithHiddenMaster();

        var client = await TestUserProjectHelpers.CreateAuthenticatedClientAsync(
            factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false }),
            ownerEmail,
            followsRedirects: false);
        var html = await GetPage(client, projectId, "{0}/home");

        CountUserLinks(html).ShouldBe(2);
    }

    /// <summary>Проект, где у владельца (публичного) есть непубличный мастер с правом выдавать доступ.</summary>
    private async Task<(ProjectIdentification ProjectId, UserIdentification HiddenMasterId, string OwnerEmail)> CreateProjectWithHiddenMaster()
    {
        using var scope = factory.Services.CreateScope();
        var (ownerId, ownerEmail) = await TestUserProjectHelpers.CreateTestUserWithEmailAsync(scope.ServiceProvider);
        var projectId = await TestUserProjectHelpers.CreateProjectAsync(scope.ServiceProvider, ownerId);
        var hiddenMasterId = await TestUserProjectHelpers.CreateTestUserAsync(scope.ServiceProvider);
        await factory.Services.RunAsAsync(ownerId, sp =>
            sp.GetRequiredService<IProjectAccessService>().GrantAccess(new GrantAccessRequest
            {
                ProjectId = projectId,
                UserId = hiddenMasterId,
                Role = new("Мастер"),
                IsPublic = false,
                Permissions = [Permission.CanManageClaims, Permission.CanGrantRights],
            }));
        return (projectId, hiddenMasterId, ownerEmail);
    }

    private static async Task<string> GetPage(HttpClient client, ProjectIdentification projectId, string pageFormat)
    {
        var page = pageFormat.Contains("{0}") ? string.Format(pageFormat, projectId.Value) : $"{projectId.Value}/{pageFormat}";
        var response = await client.GetAsync(page);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return await response.Content.ReadAsStringAsync();
    }

    private static int CountUserLinks(string html) => html.Split("class=\"join-user\"").Length - 1;

    private static async Task<string> GetMastersPage(HttpClient client, ProjectIdentification projectId)
    {
        var response = await client.GetAsync($"{projectId.Value}/masters");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return await response.Content.ReadAsStringAsync();
    }
}
