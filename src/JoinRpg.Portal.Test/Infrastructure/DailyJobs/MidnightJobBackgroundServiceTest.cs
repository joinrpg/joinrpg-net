using JoinRpg.Common.WebInfrastructure.DailyJob;
using JoinRpg.Interfaces;
using JoinRpg.Portal.Infrastructure.DailyJobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace JoinRpg.Portal.Test.Infrastructure.DailyJobs;

public class MidnightJobBackgroundServiceTest
{
    public class SampleJob : IDailyJob
    {
        public Task RunOnce(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private class FakeDailyJobRepository : IDailyJobRepository
    {
        public List<JobId> Inserted { get; } = [];

        // false — «джоба уже запущена на другом экземпляре»: так проверяем запись в журнал, не поднимая JobRunner.
        public Task<bool> TryInsertJobRecord(JobId jobId)
        {
            Inserted.Add(jobId);
            return Task.FromResult(false);
        }

        public Task<bool> TrySetJobCompleted(JobId jobId) => Task.FromResult(true);
        public Task<bool> TrySetJobFailed(JobId jobId) => Task.FromResult(true);
    }

    private class FakeOptionsMonitor(DailyJobsOptions value) : IOptionsMonitor<DailyJobsOptions>
    {
        public DailyJobsOptions CurrentValue { get; set; } = value;
        public DailyJobsOptions Get(string? name) => CurrentValue;
        public IDisposable? OnChange(Action<DailyJobsOptions, string?> listener) => null;
    }

    private class FakeLifetime : IHostApplicationLifetime
    {
        public CancellationToken ApplicationStarted => CancellationToken.None;
        public CancellationToken ApplicationStopping => CancellationToken.None;
        public CancellationToken ApplicationStopped => CancellationToken.None;
        public void StopApplication() { }
    }

    private static readonly DateOnly Today = new(2026, 5, 15);

    private readonly FakeDailyJobRepository repository = new();

    private MidnightJobBackgroundService<SampleJob> CreateService(FakeOptionsMonitor options)
    {
        var provider = new ServiceCollection()
            .AddSingleton<IDailyJobRepository>(repository)
            .BuildServiceProvider();
        return new MidnightJobBackgroundService<SampleJob>(
            provider,
            NullLogger<MidnightJobBackgroundService<SampleJob>>.Instance,
            new FakeLifetime(),
            options);
    }

    [Fact]
    public async Task EnabledJobWritesJournalRecord()
    {
        var service = CreateService(new FakeOptionsMonitor(new DailyJobsOptions()));

        await service.RunScheduledOnce(Today, CancellationToken.None);

        repository.Inserted.ShouldBe([new JobId(typeof(SampleJob).FullName!, Today)]);
    }

    [Fact]
    public async Task DisabledJobDoesNotWriteJournalRecord()
    {
        var service = CreateService(new FakeOptionsMonitor(new DailyJobsOptions { Disabled = ["SampleJob"] }));

        await service.RunScheduledOnce(Today, CancellationToken.None);

        repository.Inserted.ShouldBeEmpty();
    }

    [Fact]
    public async Task ReEnabledJobRunsOnNextScheduledRunWithoutRestart()
    {
        var options = new FakeOptionsMonitor(new DailyJobsOptions { Disabled = ["SampleJob"] });
        var service = CreateService(options);

        await service.RunScheduledOnce(Today, CancellationToken.None);
        options.CurrentValue = new DailyJobsOptions();
        await service.RunScheduledOnce(Today, CancellationToken.None);

        repository.Inserted.ShouldBe([new JobId(typeof(SampleJob).FullName!, Today)]);
    }
}
