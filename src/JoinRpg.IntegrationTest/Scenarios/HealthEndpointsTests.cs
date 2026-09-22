using System.Net;
using JoinRpg.IntegrationTest.TestInfrastructure;

namespace JoinRpg.IntegrationTest.Scenarios;

/// <summary>
/// В тестовом окружении секция Mcp не задана, значит MCP выключен — его health check отдаёт
/// Healthy с описанием «выключен». Пробы k8s это в любом случае не роняет: liveness ходит в
/// /health/live (не выполняет проверок вовсе), readiness — в /health/ready (фильтр по тегу
/// ready, которого у MCP нет), а сам /health и при Degraded обязан отдавать 200, а не 503.
/// </summary>
[Collection("XApi")]
public class HealthEndpointsTests(XApiMasterFixture fixture)
{
    [Theory]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    [InlineData("/health")]
    public async Task HealthEndpoint_ReturnsOk_WhenMcpDisabled(string path)
    {
        var client = fixture.Factory.CreateClient();

        var response = await client.GetAsync(path);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
