using System.Net;
using JoinRpg.IntegrationTest.TestInfrastructure;

namespace JoinRpg.IntegrationTest.Scenarios.XApi;

[Collection("XApi")]
public class XApiTokenTests(XApiMasterFixture fixture)
{
    [Fact]
    public async Task InvalidGrantType_Returns400()
    {
        var response = await fixture.Factory.CreateClient().PostAsync("/x-api/token",
            new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string?, string?>("username", XApiMasterFixture.MasterEmail),
                new KeyValuePair<string?, string?>("password", XApiMasterFixture.MasterPassword),
                new KeyValuePair<string?, string?>("grant_type", "client_credentials"),
            }));
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task WrongPassword_Returns403()
    {
        var response = await fixture.Factory.CreateClient().PostAsync("/x-api/token",
            new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string?, string?>("username", XApiMasterFixture.MasterEmail),
                new KeyValuePair<string?, string?>("password", "wrongpassword"),
                new KeyValuePair<string?, string?>("grant_type", "password"),
            }));
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ValidCredentials_ReturnsToken()
    {
        var auth = await fixture.AnonymousXApiClient.LoginAsync(XApiMasterFixture.MasterEmail, XApiMasterFixture.MasterPassword);
        auth.access_token.ShouldNotBeNullOrEmpty();
        auth.token_type.ShouldBe("bearer");
    }
}
