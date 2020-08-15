using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
using JoinRpg.IntegrationTests.TestInfrastructure;
using Shouldly;
using Xunit;

namespace JoinRpg.IntegrationTests.Scenarios
{
    public class RegisterScenario : IClassFixture<JoinApplicationFactory>
    {
        private readonly JoinApplicationFactory factory;

        public RegisterScenario(JoinApplicationFactory factory) => this.factory = factory;

        [Fact]
        public async Task RegistrationPageShouldOpen()
        {
            var client = factory.CreateClient();
            var response = await client.GetAsync("account/register");
            response.StatusCode.ShouldBe(HttpStatusCode.OK);
        }

        [Fact()]
        public async Task RegistrationShouldBePossible()
        {
            var client = factory.CreateClient();

            var response = await client.PostAsync("account/register",
                new FormUrlEncodedContent(
                    new[]
                    {
                        new KeyValuePair<string, string>("Email", "player@example.com"),
                        new KeyValuePair<string, string>("Password", "12345678"),
                        new KeyValuePair<string, string>("ConfirmPassword", "12345678"),
                        new KeyValuePair<string, string>("RulesApproved", "true"),
                        new KeyValuePair<string, string>("g-recaptcha-response", "ignored"),
                    }));

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            HtmlDocument doc = await response.AsHtmlDocument();

            doc.GetTitle().ShouldBe("Регистрация успешна - JoinRpg.Portal");
        }


    }
}
