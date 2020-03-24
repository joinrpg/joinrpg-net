using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace JoinRpg.Portal.Test.Integration
{
    public class SwaggerTests : IClassFixture<WebApplicationFactory<Startup>>
    {
        private readonly WebApplicationFactory<Startup> factory;

        public SwaggerTests(WebApplicationFactory<Startup> factory)
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
