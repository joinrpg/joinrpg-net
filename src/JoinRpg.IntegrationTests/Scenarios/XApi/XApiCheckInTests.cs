using System.Net;
using JoinRpg.IntegrationTest.TestInfrastructure;

namespace JoinRpg.IntegrationTest.Scenarios.XApi;

[Collection("XApi")]
public class XApiCheckInTests(XApiMasterFixture fixture)
{
    [Fact]
    public async Task WithoutAuth_Returns401()
    {
        var response = await fixture.Factory.CreateClient()
            .GetAsync($"x-game-api/{fixture.ProjectId}/checkin/allclaims");
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task MasterCanGetEmptyCheckInList()
    {
        var claims = await fixture.AuthorizedClient.GetCheckInClaimsAsync(fixture.ProjectId);

        claims.ShouldBeEmpty();
    }

    [Fact]
    public async Task MasterCanGetCheckInStat()
    {
        var stat = await fixture.AuthorizedClient.GetCheckInStatAsync(fixture.ProjectId);

        stat.Ready.ShouldBe(0);
        stat.CheckIn.ShouldBe(0);
    }
}
