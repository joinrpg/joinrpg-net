using System.Net;
using JoinRpg.IntegrationTest.TestInfrastructure;

namespace JoinRpg.IntegrationTest.Scenarios.XApi;

[Collection("XApi")]
public class XApiScheduleTests(XApiMasterFixture fixture)
{
    [Fact]
    public async Task GlobalSchedule_WithoutAuth_Returns401()
    {
        var response = await fixture.Factory.CreateClient()
            .GetAsync("x-game-api/schedule/projects/active");
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task MasterCanGetGlobalScheduleProjects()
    {
        var projects = await fixture.AuthorizedClient.GetActiveScheduleProjectsAsync();

        projects.ShouldNotBeNull();
    }
}
