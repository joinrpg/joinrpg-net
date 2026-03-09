using JoinRpg.IntegrationTest.TestInfrastructure;

namespace JoinRpg.IntegrationTest.Scenarios;

public class SwaggerTests(JoinApplicationFactory factory) : IClassFixture<JoinApplicationFactory>
{
    [Fact]
    public async Task SwaggerShouldWork()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/openapi/v1.json");

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            Assert.Fail(
                $"Expected success from /openapi/v1.json but got {(int)response.StatusCode}.\n" +
                $"Response body:\n{body}");
        }
    }
}
