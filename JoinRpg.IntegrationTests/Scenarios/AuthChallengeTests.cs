using System.Net;
using System.Threading.Tasks;
using JoinRpg.IntegrationTests.TestInfrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Shouldly;
using Xunit;

namespace JoinRpg.IntegrationTests.Scenarios
{
    public class AuthChallengeTests : IClassFixture<JoinApplicationFactory>
    {
        private readonly JoinApplicationFactory factory;

        public AuthChallengeTests(JoinApplicationFactory factory) => this.factory = factory;

        [Fact]
        public async Task ApiShouldReturn401()
        {
            var client = factory.CreateClient();
            var response = await client.GetAsync("x-api/me/projects/active");
            response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }


        [Fact]
        public async Task PortalShouldRedirect()
        {
            var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
            var response = await client.GetAsync("game/create");
            response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        }
    }
}
