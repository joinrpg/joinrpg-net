using JoinRpg.Common.WebInfrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace JoinRpg.Mcp.Test;

/// <summary>
/// Пока MCP выключался молча, «выключен» и «сломан» выглядели снаружи одинаково — 404 на /mcp
/// и ни строчки в логах. Так он и уехал на dev незамеченным. Health check делает состояние
/// наблюдаемым.
/// </summary>
public class McpHealthCheckTests
{
    private static readonly JoinRpgHostNamesOptions HostNames = new()
    {
        MainHost = "dev.joinrpg.ru",
        IdHost = "devid.joinrpg.ru",
        KogdaIgraHost = "dev.kogda-igra.ru",
        RatingHost = "rating.bastilia.ru",
    };

    [Fact]
    public async Task NotConfigured_HealthCheckReportsDegraded()
    {
        var report = await RunHealthCheckAsync(mcpOptions: null);

        var entry = report.Entries[McpRegistration.McpHealthCheckName];
        entry.Status.ShouldBe(HealthStatus.Degraded);
        entry.Description.ShouldContain("выключен");
    }

    [Fact]
    public async Task SecretMissing_HealthCheckReportsDegraded()
    {
        // Половинчатая конфигурация — самый коварный случай: ClientId задан, секрет забыли.
        var report = await RunHealthCheckAsync(new McpResourceOptions { ClientId = "portal-mcp", ClientSecret = "" });

        var entry = report.Entries[McpRegistration.McpHealthCheckName];
        entry.Status.ShouldBe(HealthStatus.Degraded);
        entry.Description.ShouldContain("выключен");
    }

    /// <summary>
    /// Degraded не должен превращаться в 503 и ронять readiness: сайт без MCP продолжает
    /// работать. Проверка не помечена тегом ready именно поэтому.
    /// </summary>
    [Fact]
    public async Task Disabled_DoesNotAffectReadiness()
    {
        var report = await RunHealthCheckAsync(mcpOptions: null);

        report.Entries[McpRegistration.McpHealthCheckName].Tags.ShouldNotContain("ready");
    }

    [Fact]
    public async Task Configured_HealthCheckReportsEnabled()
    {
        var report = await RunHealthCheckAsync(
            new McpResourceOptions { ClientId = "portal-mcp", ClientSecret = "s3cret" });

        var entry = report.Entries[McpRegistration.McpHealthCheckName];
        entry.Status.ShouldBe(HealthStatus.Healthy);
        entry.Description.ShouldContain("включён");
    }

    private static async Task<HealthReport> RunHealthCheckAsync(McpResourceOptions? mcpOptions)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddJoinMcp(mcpOptions, HostNames);

        using var provider = services.BuildServiceProvider();
        return await provider.GetRequiredService<HealthCheckService>().CheckHealthAsync();
    }
}
