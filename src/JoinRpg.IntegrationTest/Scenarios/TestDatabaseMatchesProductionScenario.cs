using System.Data.SqlClient;
using JoinRpg.IntegrationTest.TestInfrastructure;

namespace JoinRpg.IntegrationTest.Scenarios;

/// <summary>
/// Тестовая база должна быть той же средой, что прод: иначе характеризационные (golden master)
/// эталоны для перехода с EF6 на EF Core снимаются со среды, которой нигде не существует.
/// Значения — в <see cref="ProductionDatabaseParity"/>.
/// </summary>
public class TestDatabaseMatchesProductionScenario(JoinApplicationFactory factory)
    : IClassFixture<JoinApplicationFactory>
{
    [Fact]
    public async Task DatabaseOptions_MatchProduction()
    {
        using var connection = new SqlConnection(factory.ConnectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT compatibility_level, is_read_committed_snapshot_on, snapshot_isolation_state
            FROM sys.databases
            WHERE name = DB_NAME()
            """;

        using var reader = await command.ExecuteReaderAsync();
        (await reader.ReadAsync()).ShouldBeTrue("Не нашли текущую базу в sys.databases");

        reader.GetByte(0).ShouldBe((byte)ProductionDatabaseParity.CompatibilityLevel);
        reader.GetBoolean(1).ShouldBe(ProductionDatabaseParity.ReadCommittedSnapshot);
        // snapshot_isolation_state: 0 — выключена, 1 — включена.
        reader.GetByte(2).ShouldBe(ProductionDatabaseParity.AllowSnapshotIsolation ? (byte)1 : (byte)0);
    }
}
