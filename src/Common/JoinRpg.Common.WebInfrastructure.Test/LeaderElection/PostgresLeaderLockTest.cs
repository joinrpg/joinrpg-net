using JoinRpg.Common.WebInfrastructure.LeaderElection;
using JoinRpg.TestHelpers;
using Npgsql;
using Shouldly;
using Testcontainers.PostgreSql;
using Xunit;

namespace JoinRpg.Common.WebInfrastructure.Test.LeaderElection;

/// <summary>
/// Advisory lock на настоящем Postgres: взаимное исключение экземпляров, освобождение
/// при dispose и при обрыве сессии лидера (#6220).
/// </summary>
public class PostgresLeaderLockTest : IAsyncLifetime
{
    private const long Key = 6220;

    private readonly PostgreSqlContainer postgres = new PostgreSqlBuilder(ProductionPostgres.Image).Build();

    async Task IAsyncLifetime.InitializeAsync() => await postgres.StartAsync();

    async Task IAsyncLifetime.DisposeAsync() => await postgres.DisposeAsync();

    [Fact]
    public async Task SecondInstanceGetsNullWhileFirstHolds()
    {
        var lockA = new PostgresLeaderLock(postgres.GetConnectionString());
        var lockB = new PostgresLeaderLock(postgres.GetConnectionString());

        var first = await lockA.TryAcquireAsync(Key);
        first.ShouldNotBeNull();

        (await lockB.TryAcquireAsync(Key)).ShouldBeNull();
    }

    [Fact]
    public async Task SecondAcquiresAfterFirstDisposes()
    {
        var lockA = new PostgresLeaderLock(postgres.GetConnectionString());
        var lockB = new PostgresLeaderLock(postgres.GetConnectionString());
        var first = await lockA.TryAcquireAsync(Key);
        first.ShouldNotBeNull();

        await first.DisposeAsync();

        (await lockB.TryAcquireAsync(Key)).ShouldNotBeNull();
    }

    [Fact]
    public async Task TerminatedLeaderReleasesLock()
    {
        var lockA = new PostgresLeaderLock(postgres.GetConnectionString());
        var lockB = new PostgresLeaderLock(postgres.GetConnectionString());
        var first = await lockA.TryAcquireAsync(Key);
        first.ShouldNotBeNull();

        await TerminateAdvisoryLockHoldersAsync();

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        (await lockB.TryAcquireAsync(Key, timeout.Token)).ShouldNotBeNull();
    }

    [Fact]
    public async Task DifferentKeysAreIndependent()
    {
        var lockA = new PostgresLeaderLock(postgres.GetConnectionString());
        var lockB = new PostgresLeaderLock(postgres.GetConnectionString());
        var first = await lockA.TryAcquireAsync(Key);
        first.ShouldNotBeNull();

        (await lockB.TryAcquireAsync(Key + 1)).ShouldNotBeNull();
    }

    private async Task TerminateAdvisoryLockHoldersAsync()
    {
        await using var admin = new NpgsqlConnection(postgres.GetConnectionString());
        await admin.OpenAsync();
        await using var command = new NpgsqlCommand(
            "SELECT pg_terminate_backend(pid) FROM pg_locks WHERE locktype = 'advisory' AND granted",
            admin);
        await command.ExecuteNonQueryAsync();
    }
}
