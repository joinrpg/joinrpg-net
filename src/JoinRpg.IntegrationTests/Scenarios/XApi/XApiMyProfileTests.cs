using JoinRpg.IntegrationTest.TestInfrastructure;

namespace JoinRpg.IntegrationTest.Scenarios.XApi;

[Collection("XApi")]
public class XApiMyProfileTests(XApiMasterFixture fixture)
{
    [Fact]
    public async Task WithoutAuth_Returns401()
    {
        await Should.ThrowAsync<HttpRequestException>(
            () => fixture.AnonymousXApiClient.GetActiveProjectsAsync());
    }

    [Fact]
    public async Task WithAuth_ReturnsProjectsList()
    {
        var projects = await fixture.MasterClient.GetActiveProjectsAsync();
        projects.ShouldContain(p => p.ProjectId == fixture.ProjectId);
    }
}
