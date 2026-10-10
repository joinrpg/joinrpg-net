using Microsoft.Data.SqlClient;

namespace JoinRpg.Tools.RestoreLostProjects;

/// <summary>
/// Основная БД (MSSQL), только чтение. Мимо EF6 — чтобы не тянуть DataModel в консольный инструмент.
/// </summary>
internal sealed class MainDatabase(string connectionString)
{
    // Совпадают с User.RobotVirtualUser и User.OnlinePaymentVirtualUser в JoinRpg.DataModel.
    public static readonly string[] VirtualUserEmails = ["robot@joinrpg.ru", "payments@joinrpg.ru"];

    public async Task<IReadOnlyDictionary<int, string>> GetProjects(CancellationToken ct)
    {
        await using var connection = await Open(ct);
        await using var command = new SqlCommand("SELECT ProjectId, ProjectName FROM dbo.Projects", connection);
        await using var reader = await command.ExecuteReaderAsync(ct);
        var result = new Dictionary<int, string>();
        while (await reader.ReadAsync(ct))
        {
            result[reader.GetInt32(0)] = reader.IsDBNull(1) ? "" : reader.GetString(1);
        }
        return result;
    }

    public async Task<IReadOnlyList<ExistingUser>> GetUsers(CancellationToken ct)
    {
        await using var connection = await Open(ct);
        await using var command = new SqlCommand("SELECT UserId, Email FROM dbo.Users", connection);
        await using var reader = await command.ExecuteReaderAsync(ct);
        var result = new List<ExistingUser>();
        while (await reader.ReadAsync(ct))
        {
            result.Add(new ExistingUser(reader.GetInt32(0), reader.IsDBNull(1) ? null : reader.GetString(1)));
        }
        return result;
    }

    private async Task<SqlConnection> Open(CancellationToken ct)
    {
        var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(ct);
        return connection;
    }
}
