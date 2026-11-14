using Npgsql;

namespace JoinRpg.Common.WebInfrastructure.LeaderElection;

/// <summary>
/// Advisory lock PostgreSQL (<c>pg_try_advisory_lock</c>) на выделенном непулинговом соединении.
/// Advisory lock привязан к сессии: его нельзя держать на пулинговом соединении EF Core —
/// оно вернулось бы в пул раньше unlock'а. Поэтому на каждый захват открывается своё
/// <c>Pooling=false</c> соединение, живущее ровно столько, сколько живёт лок.
/// </summary>
internal sealed class PostgresLeaderLock : ILeaderLock
{
    private readonly string connectionString;

    public PostgresLeaderLock(string connectionString)
    {
        // Мы не используем Kerberos-аутентификацию к Postgres, а krb5-библиотек в контейнере нет
        // (см. https://github.com/npgsql/npgsql/issues/6360) — то же отключение, что в DbContextRegisterHelper.
        // Pooling=false: лок обязан умереть вместе с соединением, а не возвращаться в пул.
        this.connectionString = new NpgsqlConnectionStringBuilder(connectionString)
        {
            Pooling = false,
            GssEncryptionMode = GssEncryptionMode.Disable,
        }.ConnectionString;
    }

    public async Task<ILeaderLockHandle?> TryAcquireAsync(long lockKey, CancellationToken cancellationToken = default)
    {
        var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        try
        {
            await using var command = new NpgsqlCommand("SELECT pg_try_advisory_lock($1)", connection);
            _ = command.Parameters.AddWithValue(lockKey);
            var acquired = await command.ExecuteScalarAsync(cancellationToken);
            if (acquired is true)
            {
                return new PostgresLeaderLockHandle(connection, lockKey);
            }
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }

        await connection.DisposeAsync();
        return null;
    }
}

/// <summary>
/// Держит advisory lock живым, пока живо выделенное под него соединение.
/// </summary>
internal sealed class PostgresLeaderLockHandle : ILeaderLockHandle
{
    private NpgsqlConnection? connection;

    public PostgresLeaderLockHandle(NpgsqlConnection connection, long lockKey)
    {
        this.connection = connection;
        LockKey = lockKey;
    }

    private long LockKey { get; }

    /// <summary>
    /// Проверка, что соединение (и значит, лок) ещё живы. Бросает при мёртвом соединении.
    /// </summary>
    public async Task ProbeAsync()
    {
        var current = connection ?? throw new ObjectDisposedException(nameof(PostgresLeaderLockHandle));
        await using var command = new NpgsqlCommand("SELECT 1", current);
        _ = await command.ExecuteScalarAsync();
    }

    public async ValueTask DisposeAsync()
    {
        var current = Interlocked.Exchange(ref connection, null);
        if (current is null)
        {
            return;
        }

        try
        {
            await using var command = new NpgsqlCommand("SELECT pg_advisory_unlock($1)", current);
            _ = command.Parameters.AddWithValue(LockKey);
            _ = await command.ExecuteScalarAsync();
        }
        catch
        {
            // Соединение может быть уже мёртвым — сервер всё равно освободил лок при обрыве сессии.
        }

        await current.DisposeAsync();
    }
}
