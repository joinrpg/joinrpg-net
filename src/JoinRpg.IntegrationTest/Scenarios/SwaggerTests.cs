using System.Text.Json;
using JoinRpg.IntegrationTest.TestInfrastructure;

namespace JoinRpg.IntegrationTest.Scenarios;

public class SwaggerTests(JoinApplicationFactory factory) : IClassFixture<JoinApplicationFactory>
{
    [Fact]
    public async Task SwaggerShouldWork()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/openapi/v1.json");

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            Assert.Fail(
                $"Expected success from /openapi/v1.json but got {(int)response.StatusCode}.\n" +
                $"Response body:\n{body}");
        }
    }

    /// <summary>
    /// Swagger UI не умеет <c>type: ["integer", "string"]</c> и считает такой параметр незаполненным.
    /// </summary>
    [Fact]
    public async Task IntegerRouteParameterShouldBePlainInteger()
    {
        var client = factory.CreateClient();

        using var document = JsonDocument.Parse(await client.GetStringAsync("/openapi/v1.json"));

        var parameter = document.RootElement
            .GetProperty("paths")
            .GetProperty("/x-game-api/{projectId}/schedule/all")
            .GetProperty("get")
            .GetProperty("parameters")
            .EnumerateArray()
            .Single(p => p.GetProperty("name").GetString() == "projectId");
        var schema = parameter.GetProperty("schema");

        schema.GetProperty("type").ValueKind.ShouldBe(JsonValueKind.String);
        schema.GetProperty("type").GetString().ShouldBe("integer");
        schema.TryGetProperty("pattern", out _).ShouldBeFalse();
    }
}
