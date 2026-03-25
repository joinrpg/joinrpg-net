using System.Net;
using JoinRpg.IntegrationTest.TestInfrastructure;

namespace JoinRpg.IntegrationTest.Scenarios.XApi;

[Collection("XApi")]
public class XApiCheckInTests(XApiMasterFixture fixture)
{
    [Fact]
    public async Task WithoutAuth_Returns401()
    {
        var client = new XApiClient(fixture.Factory.CreateClient());
        var response = await client.GetClaimsForCheckInRawAsync(fixture.ProjectId);
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task MasterCanGetEmptyCheckInList()
    {
        var claims = await fixture.AuthorizedClient.GetClaimsForCheckInAsync(fixture.ProjectId);
        claims.ShouldNotBeNull();
        claims.ShouldBeEmpty();
    }

    [Fact]
    public async Task MasterCanGetCheckInStat()
    {
        var stat = await fixture.AuthorizedClient.GetCheckInStatAsync(fixture.ProjectId);
        stat.ShouldNotBeNull();
        stat.CheckIn.ShouldBe(0);
        stat.Ready.ShouldBe(0);
    }
}
