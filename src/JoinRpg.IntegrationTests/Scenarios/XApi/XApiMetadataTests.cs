using System.Net;
using JoinRpg.IntegrationTest.TestInfrastructure;

namespace JoinRpg.IntegrationTest.Scenarios.XApi;

[Collection("XApi")]
public class XApiMetadataTests(XApiMasterFixture fixture)
{
    [Fact]
    public async Task WithoutAuth_Returns401()
    {
        var response = await fixture.Factory.CreateClient()
            .GetAsync($"x-game-api/{fixture.ProjectId}/metadata/fields");
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task MasterCanGetFieldsMetadata()
    {
        var metadata = await fixture.AuthorizedClient.GetFieldsMetadataAsync(fixture.ProjectId);

        metadata.ProjectId.ShouldBe(fixture.ProjectId);
        metadata.ProjectName.ShouldBe(XApiMasterFixture.ProjectName);
    }
}
