using System.Threading.Tasks;
using Xunit;

namespace JoinRpg.Portal.Test.Integration
{
    public class SwaggerTests : IClassFixture<JoinApplicationFactory>
    {
        private readonly JoinApplicationFactory factory;

        public SwaggerTests(JoinApplicationFactory factory)
        {
            this.factory = factory;
        }

        [Fact]
        public async Task SwaggerShouldWork()
        {
            var client = factory.CreateClient();

            var response = await client.GetAsync("swagger/v1/swagger.json");

            response.EnsureSuccessStatusCode();
        }
    }
}
