using JoinRpg.IntegrationTest.TestInfrastructure;

namespace JoinRpg.IntegrationTest.Scenarios;

public class SwaggerTests(JoinApplicationFactory factory) : IClassFixture<JoinApplicationFactory>
{
    [Fact]
    public async Task SwaggerShouldWork()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/openapi/v1.json");

        _ = response.EnsureSuccessStatusCode();
    }
}
