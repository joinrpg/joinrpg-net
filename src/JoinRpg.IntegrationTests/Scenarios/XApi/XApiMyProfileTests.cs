using System.Net;
using JoinRpg.IntegrationTest.TestInfrastructure;

namespace JoinRpg.IntegrationTest.Scenarios.XApi;

[Collection("XApi")]
public class XApiMyProfileTests(XApiMasterFixture fixture)
{
    [Fact]
    public async Task WithoutAuth_Returns401()
    {
        var response = await fixture.AnonymousClient.GetAsync("/x-api/me/projects/active");
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task WithAuth_ReturnsProjectsList()
    {
        var projects = await fixture.MasterClient.GetActiveProjectsAsync();
        projects.ShouldContain(p => p.ProjectId == fixture.ProjectId);
    }
}
