using JoinRpg.IntegrationTest.TestInfrastructure;

namespace JoinRpg.IntegrationTest.Scenarios;

public class SwaggerTests(JoinApplicationFactory factory) : IClassFixture<JoinApplicationFactory>
{
    [Fact]
    public async Task SwaggerShouldWork()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("swagger/v1/swagger.json");

        _ = response.EnsureSuccessStatusCode();
    }
}
