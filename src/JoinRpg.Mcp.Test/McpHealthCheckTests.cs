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

    /// <summary>
    /// Выключенный MCP — не деградация, а штатное состояние окружения, где resource-клиент не
    /// заводили. Пока он отдавал Degraded, инфраструктура health check'ов писала WARN на каждую
    /// пробу k8s (~26 предупреждений за 8 минут на проде) — шум, маскирующий настоящие сигналы.
    /// Наблюдаемость обеспечивается описанием и полем enabled, а не уровнем логирования.
    /// </summary>
    [Fact]
    public async Task NotConfigured_HealthCheckReportsHealthyAndDisabled()
    {
        var report = await RunHealthCheckAsync(mcpOptions: null);

        var entry = report.Entries[McpRegistration.McpHealthCheckName];
        entry.Status.ShouldBe(HealthStatus.Healthy);
        entry.Description.ShouldContain("выключен");
        entry.Data["enabled"].ShouldBe(false);
    }

    [Fact]
    public async Task SecretMissing_HealthCheckReportsHealthyAndDisabled()
    {
        // Половинчатая конфигурация — самый коварный случай: ClientId задан, секрет забыли.
        var report = await RunHealthCheckAsync(new McpResourceOptions { ClientId = "portal-mcp", ClientSecret = "" });

        var entry = report.Entries[McpRegistration.McpHealthCheckName];
        entry.Status.ShouldBe(HealthStatus.Healthy);
        entry.Description.ShouldContain("выключен");
        entry.Data["enabled"].ShouldBe(false);
    }

    /// <summary>
    /// Сайт без MCP продолжает работать, так что проверка не должна ронять readiness — тегом
    /// ready она не помечена.
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
        entry.Data["enabled"].ShouldBe(true);
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
