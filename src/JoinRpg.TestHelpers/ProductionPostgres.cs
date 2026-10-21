namespace JoinRpg.TestHelpers;

/// <summary>
/// PostgreSQL, на котором работает прод: тесты на Testcontainers поднимают ту же мажорную версию.
/// <para>
/// На проде — Selectel Managed Database, PostgreSQL 18. <b>Менять только вместе с продом.</b>
/// Если тег разъедется с продом, тесты будут проверять среду, которой нигде нет.
/// </para>
/// </summary>
public static class ProductionPostgres
{
    /// <summary>
    /// Образ для <c>PostgreSqlBuilder</c>.
    /// </summary>
    public const string Image = "postgres:18-alpine";
}
