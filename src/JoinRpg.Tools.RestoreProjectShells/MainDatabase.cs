using System.Data;
using System.Globalization;
using Microsoft.Data.SqlClient;

namespace JoinRpg.Tools.RestoreProjectShells;

/// <summary>
/// Основная БД (MSSQL) мимо EF6: проверки перед созданием и подмена счётчика Projects.
/// Сами проекты создаёт домен (<see cref="DomainHost"/>).
/// </summary>
internal sealed class MainDatabase(string connectionString)
{
    public async Task<MainDatabaseState> GetState(int backupMaxProjectId, IReadOnlyCollection<int> creatorIds, CancellationToken ct)
    {
        await using var connection = await Open(ct);

        var projects = new Dictionary<int, string>();
        await using (var command = new SqlCommand("SELECT ProjectId, ProjectName FROM dbo.Projects WHERE ProjectId > @backupMax", connection))
        {
            command.Parameters.Add("@backupMax", SqlDbType.Int).Value = backupMaxProjectId;
            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                projects[reader.GetInt32(0)] = reader.GetString(1);
            }
        }

        var users = new HashSet<int>();
        await using (var command = new SqlCommand("SELECT UserId FROM dbo.Users", connection))
        {
            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                _ = users.Add(reader.GetInt32(0));
            }
        }
        users.IntersectWith(creatorIds);

        long? shiftedSeed;
        await using (var command = new SqlCommand(
            """
            IF OBJECT_ID(N'drp.ReseedLog', N'U') IS NOT NULL
                EXEC sp_executesql N'SELECT NewSeed FROM drp.ReseedLog WHERE TableName = N''dbo.Projects''';
            """, connection))
        {
            var value = await command.ExecuteScalarAsync(ct);
            shiftedSeed = value is null or DBNull ? null : Convert.ToInt64(value, CultureInfo.InvariantCulture);
        }

        return new MainDatabaseState(projects, users, shiftedSeed);
    }

    public async Task<long?> GetProjectsIdentityCurrent(CancellationToken ct)
    {
        await using var connection = await Open(ct);
        await using var command = new SqlCommand("SELECT IDENT_CURRENT('dbo.Projects')", connection);
        var value = await command.ExecuteScalarAsync(ct);
        return value is DBNull or null ? null : Convert.ToInt64(value, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Опускает счётчик Projects так, чтобы следующая вставка получила <paramref name="projectId"/>.
    /// В таблице уже есть строки, поэтому следующий id — новое значение + 1.
    /// </summary>
    public async Task ReseedProjectsBefore(int projectId, CancellationToken ct)
    {
        await using var connection = await Open(ct);
        await using var command = new SqlCommand("DBCC CHECKIDENT ('dbo.Projects', RESEED, @seed) WITH NO_INFOMSGS", connection);
        command.Parameters.Add("@seed", SqlDbType.Int).Value = projectId - 1;
        _ = await command.ExecuteNonQueryAsync(ct);
    }

    private async Task<SqlConnection> Open(CancellationToken ct)
    {
        var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(ct);
        return connection;
    }
}
