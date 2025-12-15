using System.Net;
using JoinRpg.IntegrationTest.TestInfrastructure;
using Microsoft.AspNetCore.Mvc.Testing;

namespace JoinRpg.IntegrationTest.Scenarios;

public class AuthChallengeTests(JoinApplicationFactory factory) : IClassFixture<JoinApplicationFactory>
{
    [Fact(Skip = "DB not working on CI")]
    public async Task ApiShouldReturn401()
    {
        var client = factory.CreateClient();
        var response = await client.GetAsync("x-api/me/projects/active");
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }


    [Fact(Skip = "DB not working on CI")]
    public async Task PortalShouldRedirect()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await client.GetAsync("game/create");
        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
    }
}
