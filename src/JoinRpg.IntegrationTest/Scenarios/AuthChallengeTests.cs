using System.Net;
using JoinRpg.IntegrationTest.TestInfrastructure;
using Microsoft.AspNetCore.Mvc.Testing;

namespace JoinRpg.IntegrationTest.Scenarios;

public class AuthChallengeTests(JoinApplicationFactory factory) : IClassFixture<JoinApplicationFactory>
{
    [Theory]
    [InlineData("x-api/me/projects/active")]
    [InlineData("x-game-api/schedule/projects/active")]
    public async Task ApiShouldReturn401(string path)
    {
        var client = factory.CreateClient();
        var response = await client.GetAsync(path);
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
