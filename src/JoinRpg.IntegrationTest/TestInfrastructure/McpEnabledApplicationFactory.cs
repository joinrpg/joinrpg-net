namespace JoinRpg.IntegrationTest.TestInfrastructure;

/// <summary>
/// Portal с включённым MCP. Обычная <see cref="JoinApplicationFactory"/> поднимает его
/// выключенным (секции Mcp нет), поэтому проверить метаданные и заголовки challenge на ней
/// нельзя — /mcp просто не замаплен.
/// </summary>
/// <remarks>
/// Креденшелы фиктивные, и этого достаточно: и Protected Resource Metadata, и 401 без токена
/// отдаются без единого обращения к IdPortal. Интроспекция здесь не участвует.
/// </remarks>
public class McpEnabledApplicationFactory : JoinApplicationFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.UseSetting("Mcp:ClientId", "integration-test-portal-mcp");
        builder.UseSetting("Mcp:ClientSecret", "integration-test-secret");
    }
}
