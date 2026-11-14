using JoinRpg.Common.WebInfrastructure.LeaderElection;
using JoinRpg.TestHelpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Testcontainers.PostgreSql;
using Xunit;

namespace JoinRpg.Common.WebInfrastructure.Test.LeaderElection;

/// <summary>
/// Leader election на настоящем Postgres: из двух реплик активна ровно одна,
/// вторая подхватывает после остановки лидера (#6220). Каждый тест самодостаточен.
/// Лидером может стать любой из хостов — статистика считается на закрытый generic-тег,
/// чтобы тест мог найти и остановить именно лидера.
/// </summary>
public class LeaderElectedHostedServiceTest : IAsyncLifetime
{
    private readonly PostgreSqlContainer postgres = new PostgreSqlBuilder(ProductionPostgres.Image).Build();

    private sealed class Host1;

    private sealed class Host2;

    private sealed class CountingService<TTag> : ILeaderElectedService
    {
        public static int Concurrent;
        public static int Runs;

        public static void Reset()
        {
            Concurrent = 0;
            Runs = 0;
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref Concurrent);
            Interlocked.Increment(ref Runs);
            try
            {
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Лидерство потеряно или хост останавливается — завершаемся по отмене.
            }
            finally
            {
                Interlocked.Decrement(ref Concurrent);
            }
        }
    }

    async Task IAsyncLifetime.InitializeAsync()
    {
        await postgres.StartAsync();
        CountingService<Host1>.Reset();
        CountingService<Host2>.Reset();
    }

    async Task IAsyncLifetime.DisposeAsync() => await postgres.DisposeAsync();

    private (ServiceProvider Provider, IHostedService Hosted) BuildHost<TTag>(long lockKey, TimeSpan? retryDelay = null)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DataProtection"] = postgres.GetConnectionString(),
            })
            .Build();
        var provider = new ServiceCollection()
            .AddLogging()
            .AddLeaderElectedHostedService<CountingService<TTag>>(config, lockKey, retryDelay: retryDelay ?? TimeSpan.FromMilliseconds(200))
            .BuildServiceProvider();
        return (provider, provider.GetRequiredService<IHostedService>());
    }

    private static int TotalRuns => CountingService<Host1>.Runs + CountingService<Host2>.Runs;

    private static async Task PollUntilAsync(Func<bool> condition, string because)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(30);
        while (!condition())
        {
            if (DateTime.UtcNow > deadline)
            {
                condition().ShouldBeTrue(because);
            }

            await Task.Delay(50);
        }
    }

    [Fact]
    public async Task OnlyOneInstanceRunsAtATime()
    {
        var (provider1, hosted1) = BuildHost<Host1>(lockKey: 6220);
        var (provider2, hosted2) = BuildHost<Host2>(lockKey: 6220);
        try
        {
            await hosted1.StartAsync(CancellationToken.None);
            await hosted2.StartAsync(CancellationToken.None);

            await PollUntilAsync(() => TotalRuns == 1, "ни один инстанс не стал лидером");
            await Task.Delay(TimeSpan.FromSeconds(2));

            (CountingService<Host1>.Concurrent + CountingService<Host2>.Concurrent).ShouldBe(1, "лидеров больше одного");
            TotalRuns.ShouldBe(1, "второй инстанс тоже запустился");
        }
        finally
        {
            await hosted1.StopAsync(CancellationToken.None);
            await hosted2.StopAsync(CancellationToken.None);
            provider1.Dispose();
            provider2.Dispose();
        }
    }

    [Fact]
    public async Task SecondTakesOverAfterFirstStops()
    {
        var (provider1, hosted1) = BuildHost<Host1>(lockKey: 6221);
        var (provider2, hosted2) = BuildHost<Host2>(lockKey: 6221);
        try
        {
            await hosted1.StartAsync(CancellationToken.None);
            await hosted2.StartAsync(CancellationToken.None);
            await PollUntilAsync(() => TotalRuns == 1, "ни один инстанс не стал лидером");

            // Лидером может стать любой хост: останавливаем того, кто держит лок.
            var leader = CountingService<Host1>.Runs == 1 ? hosted1 : hosted2;

            await leader.StopAsync(CancellationToken.None);

            await PollUntilAsync(() => TotalRuns == 2, "второй инстанс не подхватил после остановки лидера");
            (CountingService<Host1>.Concurrent + CountingService<Host2>.Concurrent).ShouldBe(1);
        }
        finally
        {
            await hosted1.StopAsync(CancellationToken.None);
            await hosted2.StopAsync(CancellationToken.None);
            provider1.Dispose();
            provider2.Dispose();
        }
    }

    [Fact]
    public async Task AcquireFailureDoesNotCrashAndRetries()
    {
        var flakyLock = new FlakyLock(failuresLeft: 2);
        var provider = new ServiceCollection().BuildServiceProvider();
        using var hosted = new LeaderElectedHostedService<CountingService<Host1>>(
            flakyLock, provider, NullLogger<LeaderElectedHostedService<CountingService<Host1>>>.Instance,
            TimeProvider.System, lockKey: 6222, retryDelay: TimeSpan.FromMilliseconds(50));

        await hosted.StartAsync(CancellationToken.None);
        try
        {
            await PollUntilAsync(() => flakyLock.Calls >= 3, "ретраев после сбоя не было");
            await Task.Delay(TimeSpan.FromMilliseconds(200));

            hosted.ExecuteTask.ShouldNotBeNull();
            hosted.ExecuteTask.IsCompleted.ShouldBeFalse("цикл захвата упал после ошибок БД");
        }
        finally
        {
            await hosted.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public void RegistrationWithoutConnectionStringThrows()
    {
        var emptyConfig = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();

        Should.Throw<InvalidOperationException>(
            () => services.AddLeaderElectedHostedService<CountingService<Host1>>(emptyConfig, lockKey: 1, connectionStringName: "Missing"));
    }

    [Fact]
    public async Task StopReleasesLock()
    {
        var (provider1, hosted1) = BuildHost<Host1>(lockKey: 6223);
        var (provider2, hosted2) = BuildHost<Host2>(lockKey: 6223);
        try
        {
            await hosted1.StartAsync(CancellationToken.None);
            await hosted2.StartAsync(CancellationToken.None);
            await PollUntilAsync(() => TotalRuns == 1, "ни один инстанс не стал лидером");
        }
        finally
        {
            await hosted1.StopAsync(CancellationToken.None);
            await hosted2.StopAsync(CancellationToken.None);
            provider1.Dispose();
            provider2.Dispose();
        }

        var lockC = new PostgresLeaderLock(postgres.GetConnectionString());
        (await lockC.TryAcquireAsync(6223)).ShouldNotBeNull("лок не освободился после остановки хостов");
    }

    [Fact]
    public async Task CompletedServiceDoesNotCauseTightRetryLoop()
    {
        var alwaysLeaderLock = new AlwaysLeaderLock();
        var provider = new ServiceCollection().AddScoped<ImmediateService>().BuildServiceProvider();
        using var hosted = new LeaderElectedHostedService<ImmediateService>(
            alwaysLeaderLock, provider, NullLogger<LeaderElectedHostedService<ImmediateService>>.Instance,
            TimeProvider.System, lockKey: 6224, retryDelay: TimeSpan.FromMilliseconds(50));

        await hosted.StartAsync(CancellationToken.None);
        try
        {
            await Task.Delay(TimeSpan.FromMilliseconds(400));

            // С бэкоффом после штатного завершения сервиса за 400 мс успеет ~8 итераций;
            // без него цикл открывал бы новые соединения тысячи раз в секунду.
            alwaysLeaderLock.Calls.ShouldBeInRange(3, 30);
        }
        finally
        {
            await hosted.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task StopWaitsForServiceCleanup()
    {
        SlowCleanupService.Reset();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DataProtection"] = postgres.GetConnectionString(),
            })
            .Build();
        var provider = new ServiceCollection()
            .AddLogging()
            .AddScoped<ScopedResource>()
            .AddLeaderElectedHostedService<SlowCleanupService>(config, lockKey: 6225, retryDelay: TimeSpan.FromMilliseconds(100))
            .BuildServiceProvider();
        var hosted = provider.GetRequiredService<IHostedService>();

        await hosted.StartAsync(CancellationToken.None);
        try
        {
            await PollUntilAsync(() => SlowCleanupService.Runs == 1, "сервис не стал лидером");

            await hosted.StopAsync(CancellationToken.None);

            SlowCleanupService.CleanupCompleted.ShouldBe(1,
                "StopAsync вернулся до завершения дочистки сервиса (scope dispose'ился раньше времени)");
        }
        finally
        {
            await hosted.StopAsync(CancellationToken.None);
            provider.Dispose();
        }
    }

    private sealed class ScopedResource : IDisposable
    {
        private bool disposed;

        public void EnsureAlive() => ObjectDisposedException.ThrowIf(disposed, this);

        public void Dispose() => disposed = true;
    }

    private sealed class SlowCleanupService(ScopedResource resource) : ILeaderElectedService
    {
        public static int Runs;
        public static int CleanupCompleted;

        public static void Reset()
        {
            Runs = 0;
            CleanupCompleted = 0;
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref Runs);
            try
            {
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Хост останавливается — дочищаемся.
            }

            resource.EnsureAlive();
            await Task.Delay(100, cancellationToken);
            Interlocked.Increment(ref CleanupCompleted);
        }
    }

    private sealed class ImmediateService : ILeaderElectedService
    {
        public Task ExecuteAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class AlwaysLeaderLock : ILeaderLock
    {
        public int Calls;

        public Task<ILeaderLockHandle?> TryAcquireAsync(long lockKey, CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref Calls);
            return Task.FromResult<ILeaderLockHandle?>(FakeHandle.Instance);
        }
    }

    private sealed class FakeHandle : ILeaderLockHandle
    {
        public static readonly FakeHandle Instance = new();

        public Task ProbeAsync() => Task.Delay(Timeout.Infinite);

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class FlakyLock(int failuresLeft) : ILeaderLock
    {
        public int Calls;

        public Task<ILeaderLockHandle?> TryAcquireAsync(long lockKey, CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref Calls);
            if (failuresLeft-- > 0)
            {
                throw new InvalidOperationException("database is down");
            }

            return Task.FromResult<ILeaderLockHandle?>(null);
        }
    }
}
