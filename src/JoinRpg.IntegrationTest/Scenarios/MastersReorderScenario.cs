using System.Net;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Data.Interfaces;
using JoinRpg.DomainTypes;
using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.Services.Interfaces.ProjectAccess;
using Microsoft.AspNetCore.Mvc.Testing;

namespace JoinRpg.IntegrationTest.Scenarios;

/// <summary>
/// Перестановка мастеров (ADR019, §5): форма JoinMoveControlNonInteractive и общий API перестановки (JoinMoveControl).
/// </summary>
public class MastersReorderScenario(JoinApplicationFactory factory) : IClassFixture<JoinApplicationFactory>
{
    [Fact]
    public async Task Granter_ReordersMasters()
    {
        UserIdentification ownerId, masterId;
        ProjectIdentification projectId;
        string ownerEmail;
        using (var scope = factory.Services.CreateScope())
        {
            (ownerId, ownerEmail) = await TestUserProjectHelpers.CreateTestUserWithEmailAsync(scope.ServiceProvider);
            projectId = await TestUserProjectHelpers.CreateProjectAsync(scope.ServiceProvider, ownerId);
            masterId = await TestUserProjectHelpers.CreateTestUserAsync(scope.ServiceProvider);
        }
        await factory.Services.RunAsAsync(ownerId, sp =>
            sp.GetRequiredService<IProjectAccessService>().GrantAccess(new GrantAccessRequest
            {
                ProjectId = projectId,
                UserId = masterId,
                Role = new("Мастер"),
                Permissions = [Permission.CanManageClaims],
            }));
        (await GetMasterOrder(projectId)).ShouldBe([ownerId, masterId]);

        var client = await TestUserProjectHelpers.CreateAuthenticatedClientAsync(
            factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false }),
            ownerEmail,
            followsRedirects: false);
        var mastersUrl = $"{projectId.Value}/masters";
        var token = await client.GetAntiforgeryTokenAsync(mastersUrl);
        var response = await client.PostFormAsync($"{mastersUrl}/reorder", token,
            ("ElementIdentification", masterId.Value.ToString()),
            ("MoveAfterIdentification", ""));
        response.StatusCode.ShouldBe(HttpStatusCode.Found);

        (await GetMasterOrder(projectId)).ShouldBe([masterId, ownerId]);
    }

    [Fact]
    public async Task Granter_ReordersMasters_ViaMoveApi()
    {
        var (projectId, ownerId, masterId, ownerEmail, _) = await CreateProjectWithTwoMasters();
        var client = await Login(ownerEmail);

        // Так переставляет JoinMoveControl: общий эндпоинт перестановки, мастер — ProjectMasterIdentification.
        var response = await client.PostAsJsonAsync($"webapi/move/MoveAfter?ProjectId={projectId.Value}", new
        {
            SelfId = new ProjectMasterIdentification(projectId, masterId.Value).ToString(),
            ParentId = projectId.ToString(),
            MoveAfterId = (string?)null,
        });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<string[]>()).ShouldBe(
            [new ProjectMasterIdentification(projectId, masterId.Value).ToString(), new ProjectMasterIdentification(projectId, ownerId.Value).ToString()]);
        (await GetMasterOrder(projectId)).ShouldBe([masterId, ownerId]);
    }

    [Fact]
    public async Task MasterWithoutGrantRights_CannotReorder_ViaMoveApi()
    {
        var (projectId, ownerId, masterId, _, masterEmail) = await CreateProjectWithTwoMasters();
        var client = await Login(masterEmail);

        var response = await client.PostAsJsonAsync($"webapi/move/MoveAfter?ProjectId={projectId.Value}", new
        {
            SelfId = new ProjectMasterIdentification(projectId, masterId.Value).ToString(),
            ParentId = projectId.ToString(),
            MoveAfterId = (string?)null,
        });

        response.IsSuccessStatusCode.ShouldBeFalse();
        (await GetMasterOrder(projectId)).ShouldBe([ownerId, masterId]);
    }

    /// <summary>Владелец и второй мастер — с правом вести заявки, но без права выдавать доступ.</summary>
    private async Task<(ProjectIdentification ProjectId, UserIdentification OwnerId, UserIdentification MasterId, string OwnerEmail, string MasterEmail)> CreateProjectWithTwoMasters()
    {
        UserIdentification ownerId, masterId;
        ProjectIdentification projectId;
        string ownerEmail, masterEmail;
        using (var scope = factory.Services.CreateScope())
        {
            (ownerId, ownerEmail) = await TestUserProjectHelpers.CreateTestUserWithEmailAsync(scope.ServiceProvider);
            projectId = await TestUserProjectHelpers.CreateProjectAsync(scope.ServiceProvider, ownerId);
            (masterId, masterEmail) = await TestUserProjectHelpers.CreateTestUserWithEmailAsync(scope.ServiceProvider);
        }
        await factory.Services.RunAsAsync(ownerId, sp =>
            sp.GetRequiredService<IProjectAccessService>().GrantAccess(new GrantAccessRequest
            {
                ProjectId = projectId,
                UserId = masterId,
                Role = new("Мастер"),
                Permissions = [Permission.CanManageClaims],
            }));
        return (projectId, ownerId, masterId, ownerEmail, masterEmail);
    }

    private async Task<HttpClient> Login(string email)
        => await TestUserProjectHelpers.CreateAuthenticatedClientAsync(
            factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false }),
            email,
            followsRedirects: false);

    private async Task<UserIdentification[]> GetMasterOrder(ProjectIdentification projectId)
    {
        using var scope = factory.Services.CreateScope();
        var projectInfo = await scope.ServiceProvider.GetRequiredService<IProjectMetadataRepository>().GetProjectMetadata(projectId);
        return [.. projectInfo.Masters.Select(m => m.UserId)];
    }
}
