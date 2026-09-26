using System.Data.SqlClient;
using Testcontainers.MsSql;

namespace JoinRpg.IntegrationTest.TestInfrastructure;

/// <summary>
/// Один SQL Server на всю тестовую сборку.
/// <para>
/// Раньше каждый <see cref="JoinApplicationFactory"/> поднимал собственный контейнер, а таких
/// фабрик в сборке под два десятка. xUnit гоняет тестовые классы параллельно, поэтому под общей
/// нагрузкой (например, <c>dotnet test Joinrpg.slnx</c>) несколько инстансов SQL Server стартовали
/// одновременно, не получали памяти и падали с exit code 1 — тесты сыпались пачками.
/// Теперь контейнер общий, а изоляция достигается отдельной базой на каждую фабрику.
/// </para>
/// <para>
/// Контейнер живёт до конца процесса и удаляется resource reaper'ом Testcontainers.
/// </para>
/// </summary>
internal static class SharedSqlServerContainer
{
    private static readonly SemaphoreSlim StartGate = new(1, 1);
    private static MsSqlContainer? container;
    private static int databaseCounter;

    /// <summary>
    /// Поднимает (при первом обращении) общий SQL Server и заводит на нём пустую базу,
    /// выровненную по проду — см. <see cref="ProductionDatabaseParity"/>.
    /// </summary>
    /// <returns>Строка подключения к свежесозданной базе.</returns>
    public static async Task<string> CreateDatabaseAsync()
    {
        var sqlServer = await GetContainerAsync();

        var databaseName = $"joinrpg_test_{Interlocked.Increment(ref databaseCounter)}";
        await ExecuteAsync(
            sqlServer,
            $"CREATE DATABASE [{databaseName}]",
            $"Не удалось создать тестовую базу {databaseName}");

        // Выравниваем по проду до наката миграций, чтобы их DDL выполнялся на том же
        // уровне совместимости, что на боевой базе.
        await ExecuteAsync(
            sqlServer,
            ProductionDatabaseParity.BuildAlignScript(databaseName),
            $"Не удалось выровнять тестовую базу {databaseName} по проду");

        return new SqlConnectionStringBuilder(sqlServer.GetConnectionString())
        {
            InitialCatalog = databaseName,
        }.ConnectionString;
    }

    /// <summary>
    /// Выполняет скрипт в контексте <c>master</c> (<c>ExecScriptAsync</c> запускает sqlcmd без
    /// <c>-d</c>), поэтому годится и для <c>ALTER DATABASE</c>, который нельзя выполнять внутри
    /// изменяемой базы.
    /// </summary>
    private static async Task ExecuteAsync(MsSqlContainer sqlServer, string script, string errorMessage)
    {
        var result = await sqlServer.ExecScriptAsync(script);
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException($"{errorMessage}: {result.Stderr}{result.Stdout}");
        }
    }

    private static async Task<MsSqlContainer> GetContainerAsync()
    {
        if (container is not null)
        {
            return container;
        }

        await StartGate.WaitAsync();
        try
        {
            if (container is null)
            {
                Log("Starting shared SQL Server container...");
                var newContainer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-CU14-ubuntu-22.04").Build();
                await newContainer.StartAsync();
                container = newContainer;
                Log("Shared SQL Server container started.");
            }

            return container;
        }
        finally
        {
            _ = StartGate.Release();
        }
    }

    private static void Log(string message) =>
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [SharedSql] {message}");
}
