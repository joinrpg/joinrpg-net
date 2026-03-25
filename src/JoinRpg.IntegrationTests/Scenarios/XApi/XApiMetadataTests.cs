using System.Net;
using JoinRpg.IntegrationTest.TestInfrastructure;

namespace JoinRpg.IntegrationTest.Scenarios.XApi;

[Collection("XApi")]
public class XApiMetadataTests(XApiMasterFixture fixture)
{
    [Fact]
    public async Task WithoutAuth_Returns401()
    {
        var client = new XApiClient(fixture.Factory.CreateClient());
        var response = await client.GetFieldsMetadataRawAsync(fixture.ProjectId);
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task MasterCanGetFieldsMetadata()
    {
        var metadata = await fixture.AuthorizedClient.GetFieldsMetadataAsync(fixture.ProjectId);
        metadata.ShouldNotBeNull();
        metadata.ProjectId.ShouldBe(fixture.ProjectId);
    }
}
