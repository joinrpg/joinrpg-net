using JoinRpg.IntegrationTest.TestInfrastructure.MockControllers;
using JoinRpg.IntegrationTests.TestInfrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Shouldly;
using Xunit;

namespace JoinRpg.IntegrationTest.Scenarios
{
    public class CheckDateTimeMappingTest : IClassFixture<JoinApplicationFactoryWithMockControllers>
    {
        private readonly JoinApplicationFactory factory;

        public CheckDateTimeMappingTest(JoinApplicationFactoryWithMockControllers factory) => this.factory = factory;

        [Fact]
        public async Task CheckDateTime()
        {
            var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
            var content = new FormUrlEncodedContent(
                    new[]
                    {
                        new KeyValuePair<string?, string?>("Date", "29.8.2020"),
                    });

            var request = new HttpRequestMessage(HttpMethod.Post, "test/mockdatetime");
            request.Content = content;

            var response = await client.SendAsync(request);
            _ = response.EnsureSuccessStatusCode();
            MockDateTimeController.LastCalledDateTime.ShouldBe(new DateTime(2020, 8, 29));
        }

        [Fact]
        public async Task CheckDateTimeWithEngLanguageRequested()
        {
            var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
            var content = new FormUrlEncodedContent(
                    new[]
                    {
                        new KeyValuePair<string?, string?>("Date", "29.8.2020"),
                    });

            var request = new HttpRequestMessage(HttpMethod.Post, "test/mockdatetime");
            request.Headers.AcceptLanguage.ParseAdd("en-US");
            request.Content = content;

            var response = await client.SendAsync(request);
            _ = response.EnsureSuccessStatusCode();
            MockDateTimeController.LastCalledDateTime.ShouldBe(new DateTime(2020, 8, 29));
        }
    }
}
