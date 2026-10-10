using Npgsql;
using NpgsqlTypes;

namespace JoinRpg.Tools.RestoreLostProjects;

/// <summary>
/// Чтение базы уведомлений (Postgres, схема JoinRpg.Dal.Notifications). Только SELECT.
/// Все запросы видят только уведомления, созданные до момента аварии <paramref name="lostAt"/>.
/// </summary>
internal sealed class NotificationsSource(string connectionString, DateTimeOffset lostAt)
{
    private const string AdminHeaderCondition = """
        "Header" LIKE @adminPrefix AND "Header" LIKE @adminSuffix
        """;

    /// <summary>
    /// Все различные EntityReference. Разбирать их в SQL не стоит — формат знает только ProjectEntityIdParser.
    /// </summary>
    public async Task<IReadOnlyList<string>> GetEntityReferences(CancellationToken ct)
    {
        const string sql = """
            SELECT DISTINCT "EntityReference" FROM "Notifications"
            WHERE "EntityReference" IS NOT NULL AND "CreatedAt" < @lostAt
            """;

        await using var connection = await Open(ct);
        await using var command = CreateCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(ct);
        var result = new List<string>();
        while (await reader.ReadAsync(ct))
        {
            result.Add(reader.GetString(0));
        }
        return result;
    }

    /// <summary>
    /// Получатели уведомлений админам о новом проекте — то есть админы.
    /// </summary>
    public async Task<IReadOnlySet<int>> GetAdminUserIds(CancellationToken ct)
    {
        const string sql = $"""
            SELECT DISTINCT "RecipientUserId" FROM "Notifications"
            WHERE {AdminHeaderCondition} AND "CreatedAt" < @lostAt
            """;

        await using var connection = await Open(ct);
        await using var command = CreateCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(ct);
        var result = new HashSet<int>();
        while (await reader.ReadAsync(ct))
        {
            _ = result.Add(reader.GetInt32(0));
        }
        return result;
    }

    /// <summary>
    /// Уведомления по заданным EntityReference. Тело целиком не тащим: признаки мастера считаются
    /// в SQL (формат — ClaimNotificationTextBuilder: вторая строка «Заявка … мастером …», тип действия
    /// жирным), тело нужно только у уведомления админам — ради привязки к КогдаИгре.
    /// </summary>
    public async Task<IReadOnlyList<ProjectNotification>> GetNotifications(IReadOnlyCollection<string> entityReferences, CancellationToken ct)
    {
        if (entityReferences.Count == 0)
        {
            return [];
        }

        const string sql = $"""
            SELECT "EntityReference", "Header", "InitiatorUserId", "RecipientUserId", "CreatedAt",
                   split_part("Body", E'\n', 2) LIKE 'Заявка % мастером %',
                   strpos("Body", '**Новая заявка**') > 0,
                   CASE WHEN {AdminHeaderCondition} THEN "Body" END
            FROM "Notifications"
            WHERE "EntityReference" = ANY(@refs) AND "CreatedAt" < @lostAt
            """;

        await using var connection = await Open(ct);
        await using var command = CreateCommand(sql, connection);
        _ = command.Parameters.AddWithValue("refs", entityReferences.ToArray());
        await using var reader = await command.ExecuteReaderAsync(ct);
        var result = new List<ProjectNotification>();
        while (await reader.ReadAsync(ct))
        {
            result.Add(new ProjectNotification(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetInt32(2),
                reader.GetInt32(3),
                reader.GetFieldValue<DateTimeOffset>(4),
                reader.GetBoolean(5),
                reader.GetBoolean(6),
                reader.IsDBNull(7) ? null : reader.GetString(7)));
        }
        return result;
    }

    private NpgsqlCommand CreateCommand(string sql, NpgsqlConnection connection)
    {
        var command = new NpgsqlCommand(sql, connection);
        _ = command.Parameters.Add(new NpgsqlParameter("lostAt", NpgsqlDbType.TimestampTz) { Value = lostAt.ToUniversalTime() });
        _ = command.Parameters.AddWithValue("adminPrefix", EscapeLike(ProjectHeaderParser.AdminHeaderPrefix) + "%");
        _ = command.Parameters.AddWithValue("adminSuffix", "%" + EscapeLike(ProjectHeaderParser.AdminHeaderSuffix));
        return command;
    }

    private static string EscapeLike(string value) => value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");

    private async Task<NpgsqlConnection> Open(CancellationToken ct)
    {
        // Запросы идут по всей таблице уведомлений — 30 секунд по умолчанию на проде может не хватить.
        // 0 означает «без ограничения» — его не трогаем.
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        if (builder.CommandTimeout != 0)
        {
            builder.CommandTimeout = Math.Max(builder.CommandTimeout, 600);
        }
        var connection = new NpgsqlConnection(builder.ConnectionString);
        await connection.OpenAsync(ct);
        return connection;
    }
}
