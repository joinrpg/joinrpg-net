using System.Net;
using JoinRpg.IntegrationTest.TestInfrastructure;

namespace JoinRpg.IntegrationTest.Scenarios.XApi;

[Collection("XApi")]
public class XApiMyProfileTests(XApiMasterFixture fixture)
{
    [Fact]
    public async Task WithoutAuth_Returns401()
    {
        var client = new XApiClient(fixture.Factory.CreateClient());
        var response = await client.GetActiveProjectsRawAsync();
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task WithAuth_ReturnsProjectsList()
    {
        var projects = await fixture.AuthorizedClient.GetActiveProjectsAsync();
        projects.ShouldNotBeNull();
        projects.ShouldContain(p => p.ProjectId == fixture.ProjectId);
    }
}
