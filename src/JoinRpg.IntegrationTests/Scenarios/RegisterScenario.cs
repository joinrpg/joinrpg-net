using System.Net;
using HtmlAgilityPack;
using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.IntegrationTests.TestInfrastructure;

namespace JoinRpg.IntegrationTest.Scenarios;

public class RegisterScenario(JoinApplicationFactory factory) : IClassFixture<JoinApplicationFactory>
{
    [Fact]
    public async Task RegistrationPageShouldOpen()
    {
        var client = factory.CreateClient();
        var response = await client.GetAsync("account/register");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RegistrationShouldBePossible()
    {
        var client = factory.CreateClient();

        // Get the registration page first to obtain the antiforgery token
        var getResponse = await client.GetAsync("account/register");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var getDoc = await getResponse.AsHtmlDocument();
        var antiforgeryToken = getDoc.DocumentNode
            .SelectSingleNode("//input[@name='__RequestVerificationToken']")?
            .GetAttributeValue("value", "");

        var response = await client.PostAsync("account/register",
            new FormUrlEncodedContent(
                new[]
                {
                    new KeyValuePair<string?, string?>("Email", "player@example.com"),
                    new KeyValuePair<string?, string?>("Password", "12345678"),
                    new KeyValuePair<string?, string?>("ConfirmPassword", "12345678"),
                    new KeyValuePair<string?, string?>("RulesApproved", "true"),
                    new KeyValuePair<string?, string?>("g-recaptcha-response", "ignored"),
                    new KeyValuePair<string?, string?>("__RequestVerificationToken", antiforgeryToken),
                }));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        HtmlDocument doc = await response.AsHtmlDocument();

        doc.GetTitle().ShouldBe("Регистрация успешна - JoinRpg.Portal");
    }


}
