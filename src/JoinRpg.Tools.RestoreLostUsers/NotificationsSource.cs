using Npgsql;
using NpgsqlTypes;

namespace JoinRpg.Tools.RestoreLostUsers;

/// <summary>
/// Чтение базы уведомлений (Postgres, схема JoinRpg.Dal.Notifications). Только SELECT.
/// Все запросы видят только уведомления, созданные до момента аварии <paramref name="lostAt"/>:
/// после него id потерянного пользователя мог достаться новому регистранту.
/// </summary>
internal sealed class NotificationsSource(string connectionString, DateTimeOffset lostAt)
{
    public async Task<IReadOnlyList<NotificationUserActivity>> GetActivity(CancellationToken ct)
    {
        const string sql = """
            SELECT u.user_id,
                   MIN(u.created_at),
                   MAX(u.created_at),
                   COUNT(*) FILTER (WHERE u.is_recipient),
                   COUNT(*) FILTER (WHERE NOT u.is_recipient)
            FROM (
                SELECT "RecipientUserId" AS user_id, "CreatedAt" AS created_at, TRUE AS is_recipient FROM "Notifications" WHERE "CreatedAt" < @lostAt
                UNION ALL
                SELECT "InitiatorUserId", "CreatedAt", FALSE FROM "Notifications" WHERE "CreatedAt" < @lostAt
            ) u
            GROUP BY u.user_id
            """;

        await using var connection = await Open(ct);
        await using var command = CreateCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(ct);
        var result = new List<NotificationUserActivity>();
        while (await reader.ReadAsync(ct))
        {
            result.Add(new NotificationUserActivity(
                reader.GetInt32(0),
                reader.GetFieldValue<DateTimeOffset>(1),
                reader.GetFieldValue<DateTimeOffset>(2),
                checked((int)reader.GetInt64(3)),
                checked((int)reader.GetInt64(4))));
        }
        return result;
    }

    public async Task<IReadOnlyDictionary<int, NotificationUserDetails>> GetDetails(IReadOnlyCollection<int> userIds, CancellationToken ct)
    {
        if (userIds.Count == 0)
        {
            return new Dictionary<int, NotificationUserDetails>();
        }

        var ids = userIds.ToArray();
        await using var connection = await Open(ct);

        var emails = await GetEmailValues(connection, ids, ct);
        var telegrams = await GetLatestTelegramValues(connection, ids, ct);
        var greetings = await GetGreetings(connection, ids, ct);

        return ids.ToDictionary(
            id => id,
            id => new NotificationUserDetails(
                id,
                emails.TryGetValue(id, out var emailList) ? emailList : [],
                telegrams.GetValueOrDefault(id),
                greetings.TryGetValue(id, out var greetingList) ? greetingList : []));
    }

    private async Task<Dictionary<int, List<ChannelValue>>> GetEmailValues(NpgsqlConnection connection, int[] ids, CancellationToken ct)
    {
        // Все адреса, а не только последний: если их несколько, это попадёт в отчёт.
        const string sql = """
            SELECT n."RecipientUserId", c."ChannelSpecificValue", MAX(n."CreatedAt")
            FROM "Notifications" n
            JOIN "NotificationMessageChannels" c ON c."NotificationMessageId" = n."NotificationMessageId"
            WHERE n."RecipientUserId" = ANY(@ids) AND c."Channel"::text = 'email' AND n."CreatedAt" < @lostAt
            GROUP BY 1, 2
            """;

        await using var command = CreateCommand(sql, connection);
        _ = command.Parameters.AddWithValue("ids", ids);
        await using var reader = await command.ExecuteReaderAsync(ct);
        var result = new Dictionary<int, List<ChannelValue>>();
        while (await reader.ReadAsync(ct))
        {
            GetOrAdd(result, reader.GetInt32(0)).Add(new ChannelValue(reader.GetString(1), reader.GetFieldValue<DateTimeOffset>(2)));
        }
        return result;
    }

    private async Task<Dictionary<int, string>> GetLatestTelegramValues(NpgsqlConnection connection, int[] ids, CancellationToken ct)
    {
        const string sql = """
            SELECT DISTINCT ON (n."RecipientUserId") n."RecipientUserId", c."ChannelSpecificValue"
            FROM "Notifications" n
            JOIN "NotificationMessageChannels" c ON c."NotificationMessageId" = n."NotificationMessageId"
            WHERE n."RecipientUserId" = ANY(@ids) AND c."Channel"::text = 'telegram' AND n."CreatedAt" < @lostAt
            ORDER BY n."RecipientUserId", n."CreatedAt" DESC, n."NotificationMessageId" DESC
            """;

        await using var command = CreateCommand(sql, connection);
        _ = command.Parameters.AddWithValue("ids", ids);
        await using var reader = await command.ExecuteReaderAsync(ct);
        var result = new Dictionary<int, string>();
        while (await reader.ReadAsync(ct))
        {
            result[reader.GetInt32(0)] = reader.GetString(1);
        }
        return result;
    }

    private async Task<Dictionary<int, List<GreetingLine>>> GetGreetings(NpgsqlConnection connection, int[] ids, CancellationToken ct)
    {
        const string sql = """
            SELECT "RecipientUserId", split_part("Body", E'\n', 1) AS first_line, COUNT(*), MAX("CreatedAt")
            FROM "Notifications"
            WHERE "RecipientUserId" = ANY(@ids) AND "Body" LIKE @prefix AND "CreatedAt" < @lostAt
            GROUP BY 1, 2
            """;

        await using var command = CreateCommand(sql, connection);
        _ = command.Parameters.AddWithValue("ids", ids);
        _ = command.Parameters.AddWithValue("prefix", GreetingNameParser.GreetingPrefix + "%");
        await using var reader = await command.ExecuteReaderAsync(ct);
        var result = new Dictionary<int, List<GreetingLine>>();
        while (await reader.ReadAsync(ct))
        {
            GetOrAdd(result, reader.GetInt32(0)).Add(new GreetingLine(
                reader.GetString(1).TrimEnd('\r'),
                checked((int)reader.GetInt64(2)),
                reader.GetFieldValue<DateTimeOffset>(3)));
        }
        return result;
    }

    private static List<T> GetOrAdd<T>(Dictionary<int, List<T>> dictionary, int key)
    {
        if (!dictionary.TryGetValue(key, out var list))
        {
            list = [];
            dictionary[key] = list;
        }
        return list;
    }

    private NpgsqlCommand CreateCommand(string sql, NpgsqlConnection connection)
    {
        var command = new NpgsqlCommand(sql, connection);
        _ = command.Parameters.Add(new NpgsqlParameter("lostAt", NpgsqlDbType.TimestampTz) { Value = lostAt.ToUniversalTime() });
        return command;
    }

    private async Task<NpgsqlConnection> Open(CancellationToken ct)
    {
        // Агрегаты идут по всей таблице уведомлений — 30 секунд по умолчанию на проде может не хватить.
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
