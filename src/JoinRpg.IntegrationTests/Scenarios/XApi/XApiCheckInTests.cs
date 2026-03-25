using System.Net;
using JoinRpg.IntegrationTest.TestInfrastructure;

namespace JoinRpg.IntegrationTest.Scenarios.XApi;

[Collection("XApi")]
public class XApiCheckInTests(XApiMasterFixture fixture)
{
    [Fact]
    public async Task WithoutAuth_Returns401()
    {
        var response = await fixture.AnonymousClient.GetAsync($"/x-game-api/{fixture.ProjectId}/checkin/allclaims");
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task MasterCanGetEmptyCheckInList()
    {
        var claims = await fixture.MasterClient.GetCheckInClaimsAsync(fixture.ProjectId);
        claims.ShouldBeEmpty();
    }

    [Fact]
    public async Task MasterCanGetCheckInStats()
    {
        var stats = await fixture.MasterClient.GetCheckInStatsAsync(fixture.ProjectId);
        stats.CheckIn.ShouldBe(0);
        stats.Ready.ShouldBe(0);
    }
}
