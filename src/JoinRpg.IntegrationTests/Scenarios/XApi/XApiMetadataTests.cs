using JoinRpg.IntegrationTest.TestInfrastructure;

namespace JoinRpg.IntegrationTest.Scenarios.XApi;

[Collection("XApi")]
public class XApiMetadataTests(XApiMasterFixture fixture)
{
    [Fact]
    public async Task WithoutAuth_Returns401()
    {
        await Should.ThrowAsync<HttpRequestException>(
            () => fixture.AnonymousXApiClient.GetFieldsMetadataAsync(fixture.ProjectId));
    }

    [Fact]
    public async Task MasterCanGetFieldsMetadata()
    {
        var metadata = await fixture.MasterClient.GetFieldsMetadataAsync(fixture.ProjectId);
        metadata.ProjectId.ShouldBe(fixture.ProjectId);
        metadata.Fields.ShouldNotBeNull();
    }
}
