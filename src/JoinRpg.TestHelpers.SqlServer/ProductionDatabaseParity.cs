using System.Data.Common;
using Testcontainers.MsSql;

namespace JoinRpg.TestHelpers.SqlServer;

/// <summary>
/// Параметры боевой базы, которые тестовая база обязана повторять.
/// <para>
/// Значения сняты с прода при сверке схем (ADR016, этап P5, см.
/// <c>docs/spike-schema-drift-prod-vs-migrations.md</c>). Свежесозданная база в контейнере
/// получает дефолты своего образа SQL Server (уровень 150, блокировочная изоляция) — то есть
/// среду, которой нигде не существует. Для характеризационных (golden master) тестов перехода
/// с EF6 на EF Core это неприемлемо: уровень совместимости меняет SQL, который генерирует
/// провайдер (<c>OPENJSON</c>, <c>STRING_AGG</c>, <c>TRIM</c> требуют ≥ 130), а режим изоляции —
/// видимость данных, гонки и дедлоки. Поэтому база выравнивается по проду сразу после
/// <c>CREATE DATABASE</c> и до наката миграций. То же нужно и остальным тестам на SQL Server:
/// иначе они проверяют не ту среду, что работает на проде.
/// </para>
/// <para>
/// <b>Менять только вместе с продом.</b> Если на боевой базе поднимут уровень совместимости или
/// переключат изоляцию — правится здесь же, иначе тесты снова начнут мерить не ту среду.
/// Поднимать уровень на проде «заодно» нельзя: это отдельное решение со своими рисками.
/// </para>
/// </summary>
public static class ProductionDatabaseParity
{
    /// <summary>
    /// Уровень совместимости прода: 110 — это SQL Server 2012.
    /// Да, в 2026 году и на SQL Server 2019. Так на проде.
    /// </summary>
    public const int CompatibilityLevel = 110;

    /// <summary>
    /// На проде <c>READ_COMMITTED_SNAPSHOT</c> включён: READ COMMITTED работает на версиях строк,
    /// а не на блокировках.
    /// </summary>
    public const bool ReadCommittedSnapshot = true;

    /// <summary>
    /// На проде <c>ALLOW_SNAPSHOT_ISOLATION</c> включён.
    /// </summary>
    public const bool AllowSnapshotIsolation = true;

    /// <summary>
    /// Заводит на сервере в контейнере пустую базу с коллейшном прода
    /// (<see cref="ProductionSqlServer.DatabaseCollation"/>) и выравнивает её параметры по проду
    /// до наката миграций, чтобы их DDL выполнялся на том же уровне совместимости, что на боевой базе.
    /// <para>
    /// Базу по умолчанию из строки подключения Testcontainers (<c>master</c>) для тестов брать нельзя:
    /// это системная база с дефолтами сервера, а не прода.
    /// </para>
    /// </summary>
    /// <param name="sqlServer">Запущенный контейнер SQL Server.</param>
    /// <param name="databaseName">Имя новой базы — простой идентификатор, подставляется в скрипт без экранирования.</param>
    /// <returns>Строка подключения к свежесозданной базе.</returns>
    public static async Task<string> CreateDatabaseAsync(MsSqlContainer sqlServer, string databaseName)
    {
        await ExecuteAsync(
            sqlServer,
            $"CREATE DATABASE [{databaseName}] COLLATE {ProductionSqlServer.DatabaseCollation}",
            $"Не удалось создать тестовую базу {databaseName}");

        await ExecuteAsync(
            sqlServer,
            BuildAlignScript(databaseName),
            $"Не удалось выровнять тестовую базу {databaseName} по проду");

        var builder = new DbConnectionStringBuilder { ConnectionString = sqlServer.GetConnectionString() };
        builder["Database"] = databaseName;
        return builder.ConnectionString;
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

    /// <summary>
    /// Скрипт выравнивания свежесозданной базы по проду.
    /// Выполняется в контексте <c>master</c>: <c>ALTER DATABASE</c> нельзя выполнять
    /// внутри изменяемой базы.
    /// </summary>
    /// <remarks>
    /// <c>SET READ_COMMITTED_SNAPSHOT</c> требует эксклюзивного доступа к базе; сразу после
    /// <c>CREATE DATABASE</c> посторонних подключений нет, поэтому обходимся без
    /// <c>SINGLE_USER WITH ROLLBACK IMMEDIATE</c>.
    /// </remarks>
    private static string BuildAlignScript(string databaseName) =>
        $"""
        ALTER DATABASE [{databaseName}] SET COMPATIBILITY_LEVEL = {CompatibilityLevel};
        ALTER DATABASE [{databaseName}] SET ALLOW_SNAPSHOT_ISOLATION {OnOff(AllowSnapshotIsolation)};
        ALTER DATABASE [{databaseName}] SET READ_COMMITTED_SNAPSHOT {OnOff(ReadCommittedSnapshot)};
        """;

    private static string OnOff(bool value) => value ? "ON" : "OFF";
}
