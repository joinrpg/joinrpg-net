namespace JoinRpg.IntegrationTest.TestInfrastructure;

/// <summary>
/// Параметры боевой базы, которые тестовая база обязана повторять.
/// <para>
/// Значения сняты с прода при сверке схем (ADR015, этап P5, см.
/// <c>docs/spike-schema-drift-prod-vs-migrations.md</c>). Свежесозданная база в контейнере
/// получает дефолты своего образа SQL Server (уровень 150, блокировочная изоляция) — то есть
/// среду, которой нигде не существует. Для характеризационных (golden master) тестов перехода
/// с EF6 на EF Core это неприемлемо: уровень совместимости меняет SQL, который генерирует
/// провайдер (<c>OPENJSON</c>, <c>STRING_AGG</c>, <c>TRIM</c> требуют ≥ 130), а режим изоляции —
/// видимость данных, гонки и дедлоки. Поэтому база выравнивается по проду сразу после
/// <c>CREATE DATABASE</c> и до наката миграций.
/// </para>
/// <para>
/// <b>Менять только вместе с продом.</b> Если на боевой базе поднимут уровень совместимости или
/// переключат изоляцию — правится здесь же, иначе тесты снова начнут мерить не ту среду.
/// Поднимать уровень на проде «заодно» нельзя: это отдельное решение со своими рисками.
/// </para>
/// </summary>
internal static class ProductionDatabaseParity
{
    /// <summary>
    /// Уровень совместимости прода: 110 — это SQL Server 2012.
    /// Да, в 2026 году и на образе SQL Server 2022. Так на проде.
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
    /// Скрипт выравнивания свежесозданной базы по проду.
    /// Выполняется в контексте <c>master</c>: <c>ALTER DATABASE</c> нельзя выполнять
    /// внутри изменяемой базы.
    /// </summary>
    /// <remarks>
    /// <c>SET READ_COMMITTED_SNAPSHOT</c> требует эксклюзивного доступа к базе; сразу после
    /// <c>CREATE DATABASE</c> посторонних подключений нет, поэтому обходимся без
    /// <c>SINGLE_USER WITH ROLLBACK IMMEDIATE</c>.
    /// </remarks>
    public static string BuildAlignScript(string databaseName) =>
        $"""
        ALTER DATABASE [{databaseName}] SET COMPATIBILITY_LEVEL = {CompatibilityLevel};
        ALTER DATABASE [{databaseName}] SET ALLOW_SNAPSHOT_ISOLATION {OnOff(AllowSnapshotIsolation)};
        ALTER DATABASE [{databaseName}] SET READ_COMMITTED_SNAPSHOT {OnOff(ReadCommittedSnapshot)};
        """;

    private static string OnOff(bool value) => value ? "ON" : "OFF";
}
