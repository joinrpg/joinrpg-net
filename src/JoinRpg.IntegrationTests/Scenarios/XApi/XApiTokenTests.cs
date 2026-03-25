using System.Net;
using System.Net.Http.Json;
using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.XGameApi.Contract;

namespace JoinRpg.IntegrationTest.Scenarios.XApi;

[Collection("XApi")]
public class XApiTokenTests(XApiMasterFixture fixture)
{
    [Fact]
    public async Task InvalidGrantType_Returns400()
    {
        var client = new XApiClient(fixture.Factory.CreateClient());
        var response = await client.PostLoginRawAsync("user@test.com", "password", grantType: "client_credentials");
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task WrongPassword_ReturnsForbid()
    {
        var client = new XApiClient(fixture.Factory.CreateClient());
        var response = await client.PostLoginRawAsync(XApiMasterFixture.MasterEmail, "wrong_password!");
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ValidCredentials_ReturnsTokenResponse()
    {
        var client = new XApiClient(fixture.Factory.CreateClient());
        var response = await client.PostLoginRawAsync(XApiMasterFixture.MasterEmail, XApiMasterFixture.MasterPassword);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var auth = await response.Content.ReadFromJsonAsync<AuthenticationResponse>();
        auth.ShouldNotBeNull();
        auth.access_token.ShouldNotBeNullOrEmpty();
    }
}
