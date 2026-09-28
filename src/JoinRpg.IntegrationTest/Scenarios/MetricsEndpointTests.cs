using System.Net;
using JoinRpg.IntegrationTest.TestInfrastructure;

namespace JoinRpg.IntegrationTest.Scenarios;

/// <summary>
/// Эндпоинт /metrics отдаёт метрики в формате Prometheus (см. #5047: он молча отдавал
/// пустое тело, потому что версия экспортёра разъехалась с ядром OpenTelemetry).
/// Проверяем не только 200, но и то, что в теле есть реальные серии: любой обслуженный
/// запрос обязан попасть в http.server.request.duration из метра Microsoft.AspNetCore.Hosting.
/// </summary>
[Collection("XApi")]
public class MetricsEndpointTests(XApiMasterFixture fixture)
{
    [Fact]
    public async Task MetricsEndpoint_ExposesAspNetCoreRequestMetrics()
    {
        var client = fixture.Factory.CreateClient();

        // Гарантируем, что хотя бы один запрос уже обслужен и записан в метрики
        _ = await client.GetAsync("/health/live");

        var response = await client.GetAsync("/metrics");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();

        body.ShouldNotBeEmpty();
        body.ShouldContain("http_server_request_duration_seconds");
    }
}
