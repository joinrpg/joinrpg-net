using System.Data;
using Microsoft.Data.SqlClient;

namespace JoinRpg.Tools.RestoreLostUsers;

/// <summary>
/// Основная БД (MSSQL). Пишем мимо EF6: MyDbContext не даёт задать значение IDENTITY-колонки,
/// а нам нужно вернуть пользователям прежние id.
/// </summary>
internal sealed class MainDatabase(string connectionString)
{
    // Совпадают с User.RobotVirtualUser и User.OnlinePaymentVirtualUser в JoinRpg.DataModel.
    // DataModel не подключаем, чтобы не тянуть EF6 в консольный инструмент.
    public static readonly string[] VirtualUserEmails = ["robot@joinrpg.ru", "payments@joinrpg.ru"];

    public async Task<IReadOnlyList<ExistingUser>> GetUsers(CancellationToken ct)
    {
        await using var connection = await Open(ct);
        await using var command = new SqlCommand("SELECT UserId, Email, UserName FROM dbo.Users", connection);
        await using var reader = await command.ExecuteReaderAsync(ct);
        var result = new List<ExistingUser>();
        while (await reader.ReadAsync(ct))
        {
            result.Add(new ExistingUser(
                reader.GetInt32(0),
                reader.IsDBNull(1) ? null : reader.GetString(1),
                reader.IsDBNull(2) ? null : reader.GetString(2)));
        }
        return result;
    }

    public async Task<decimal?> GetIdentityCurrent(CancellationToken ct)
    {
        await using var connection = await Open(ct);
        await using var command = new SqlCommand("SELECT IDENT_CURRENT('dbo.Users')", connection);
        var value = await command.ExecuteScalarAsync(ct);
        return value is DBNull or null ? null : Convert.ToDecimal(value, System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Создаёт заготовки пользователей с заданными id в одной транзакции. Строки — как у только что
    /// зарегистрированного пользователя (MyUserStore.CreateImpl), но почта подтверждена, пароля нет,
    /// а штамп безопасности случайный (см. <see cref="NewUserRow"/>).
    /// Пользователь, чей id или адрес почты уже занят, пропускается — повторный запуск ничего не меняет.
    /// </summary>
    /// <returns>Id созданных пользователей.</returns>
    public async Task<IReadOnlyList<int>> CreateUsers(IReadOnlyCollection<NewUserRow> users, CancellationToken ct)
    {
        const string insertSql = """
            INSERT INTO dbo.Users (UserId, BornName, FatherName, SurName, UserName, Email, PasswordHash, PrefferedName, VerifiedProfileFlag, SelectedAvatarId)
            SELECT @id, NULL, NULL, NULL, @email, @email, NULL, @name, 0, NULL
            WHERE NOT EXISTS (
                SELECT 1 FROM dbo.Users WITH (UPDLOCK, HOLDLOCK)
                WHERE UserId = @id OR Email = @email OR UserName = @email);

            IF @@ROWCOUNT = 1
            BEGIN
                INSERT INTO dbo.UserAuthDetails (UserId, EmailConfirmed, RegisterDate, IsAdmin, AspNetSecurityStamp, LastLoginDate)
                VALUES (@id, 1, @registerDate, 0, @securityStamp, NULL);

                INSERT INTO dbo.UserExtras (UserId, GenderByte, Gender, PhoneNumber, Nicknames, GroupNames, BirthDate, Vk, Livejournal, Telegram,
                    VkVerified, SocialNetworksAccess, EnableTelegramPlayerDigestNotification, PassportData, RegistrationAddress)
                VALUES (@id, 0, 0, NULL, NULL, NULL, NULL, NULL, NULL, NULL,
                    0, 1, 1, NULL, NULL);

                SELECT CAST(1 AS bit);
            END
            ELSE
                SELECT CAST(0 AS bit);
            """;

        await using var connection = await Open(ct);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(ct);
        var created = new List<int>();
        try
        {
            await Execute(connection, transaction, "SET IDENTITY_INSERT dbo.Users ON", ct);

            foreach (var user in users)
            {
                await using var command = new SqlCommand(insertSql, connection, transaction);
                command.Parameters.Add("@id", SqlDbType.Int).Value = user.UserId;
                command.Parameters.Add("@email", SqlDbType.NVarChar, -1).Value = user.Email;
                command.Parameters.Add("@name", SqlDbType.NVarChar, -1).Value = (object?)user.PrefferedName ?? DBNull.Value;
                command.Parameters.Add("@registerDate", SqlDbType.DateTime).Value = user.RegisterDateUtc;
                command.Parameters.Add("@securityStamp", SqlDbType.NVarChar, -1).Value = user.SecurityStamp;

                if (await command.ExecuteScalarAsync(ct) is true)
                {
                    created.Add(user.UserId);
                }
            }

            await Execute(connection, transaction, "SET IDENTITY_INSERT dbo.Users OFF", ct);
            // Всё уже записано — отмена на этом шаге оставила бы исход неясным.
            await transaction.CommitAsync(CancellationToken.None);
        }
        catch
        {
            try
            {
                await transaction.RollbackAsync(CancellationToken.None);
            }
            catch
            {
                // SQL Server мог уже сам откатить транзакцию (например, при ошибке уровня батча) —
                // тогда Rollback бросает InvalidOperationException и прячет исходную причину.
            }
            throw;
        }
        return created;
    }

    private static async Task Execute(SqlConnection connection, SqlTransaction transaction, string sql, CancellationToken ct)
    {
        await using var command = new SqlCommand(sql, connection, transaction);
        _ = await command.ExecuteNonQueryAsync(ct);
    }

    private async Task<SqlConnection> Open(CancellationToken ct)
    {
        var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(ct);
        return connection;
    }
}
