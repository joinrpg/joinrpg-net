using JoinRpg.IntegrationTests.TestInfrastructure;
using Xunit;

namespace JoinRpg.IntegrationTests.Scenarios
{
    public class SwaggerTests : IClassFixture<JoinApplicationFactory>
    {
        private readonly JoinApplicationFactory factory;

        public SwaggerTests(JoinApplicationFactory factory) => this.factory = factory;

        [Fact]
        public async Task SwaggerShouldWork()
        {
            var client = factory.CreateClient();

            var response = await client.GetAsync("swagger/v1/swagger.json");

            _ = response.EnsureSuccessStatusCode();
        }
    }
}
