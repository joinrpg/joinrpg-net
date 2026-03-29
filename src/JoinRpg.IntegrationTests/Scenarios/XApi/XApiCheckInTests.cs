using JoinRpg.IntegrationTest.TestInfrastructure;

namespace JoinRpg.IntegrationTest.Scenarios.XApi;

[Collection("XApi")]
public class XApiCheckInTests(XApiMasterFixture fixture)
{
    [Fact]
    public async Task WithoutAuth_Returns401()
    {
        await Should.ThrowAsync<HttpRequestException>(
            () => fixture.AnonymousXApiClient.GetCheckInClaimsAsync(fixture.ProjectId));
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
