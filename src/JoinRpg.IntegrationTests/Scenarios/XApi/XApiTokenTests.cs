using System.Net;
using JoinRpg.IntegrationTest.TestInfrastructure;

namespace JoinRpg.IntegrationTest.Scenarios.XApi;

[Collection("XApi")]
public class XApiTokenTests(XApiMasterFixture fixture)
{
    [Fact]
    public async Task InvalidGrantType_Returns400()
    {
        var client = fixture.Factory.CreateClient();
        var response = await client.PostAsync("x-api/token",
            new FormUrlEncodedContent(
            [
                new KeyValuePair<string?, string?>("username", XApiMasterFixture.MasterEmail),
                new KeyValuePair<string?, string?>("password", XApiMasterFixture.MasterPassword),
                new KeyValuePair<string?, string?>("grant_type", "client_credentials"),
            ]));
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task WrongPassword_ReturnsForbid()
    {
        var client = fixture.Factory.CreateClient();
        var response = await client.PostAsync("x-api/token",
            new FormUrlEncodedContent(
            [
                new KeyValuePair<string?, string?>("username", XApiMasterFixture.MasterEmail),
                new KeyValuePair<string?, string?>("password", "wrong-password"),
                new KeyValuePair<string?, string?>("grant_type", "password"),
            ]));
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ValidCredentials_ReturnsToken()
    {
        var xApiClient = new XApiClient(fixture.Factory.CreateClient());
        var tokenResponse = await xApiClient.GetTokenAsync(
            XApiMasterFixture.MasterEmail,
            XApiMasterFixture.MasterPassword);

        tokenResponse.access_token.ShouldNotBeNullOrEmpty();
        tokenResponse.token_type.ShouldBe("bearer");
        tokenResponse.expires_in.ShouldBeGreaterThan(0);
    }
}
