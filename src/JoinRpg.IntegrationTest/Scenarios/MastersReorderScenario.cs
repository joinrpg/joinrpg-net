using System.Net;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Data.Interfaces;
using JoinRpg.DomainTypes;
using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.Services.Interfaces.ProjectAccess;
using Microsoft.AspNetCore.Mvc.Testing;

namespace JoinRpg.IntegrationTest.Scenarios;

/// <summary>
/// Перестановка мастеров (ADR019, §5): форма JoinMoveControlNonInteractive постит «кого» и «после кого».
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

    private async Task<UserIdentification[]> GetMasterOrder(ProjectIdentification projectId)
    {
        using var scope = factory.Services.CreateScope();
        var projectInfo = await scope.ServiceProvider.GetRequiredService<IProjectMetadataRepository>().GetProjectMetadata(projectId);
        return [.. projectInfo.Masters.Select(m => m.UserId)];
    }
}
