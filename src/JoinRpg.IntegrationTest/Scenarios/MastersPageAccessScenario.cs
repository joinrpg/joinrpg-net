using System.Net;
using JoinRpg.Dal.Impl;
using JoinRpg.DomainTypes;
using JoinRpg.IntegrationTest.TestInfrastructure;
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

    private static async Task<string> GetMastersPage(HttpClient client, ProjectIdentification projectId)
    {
        var response = await client.GetAsync($"{projectId.Value}/masters");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return await response.Content.ReadAsStringAsync();
    }
}
