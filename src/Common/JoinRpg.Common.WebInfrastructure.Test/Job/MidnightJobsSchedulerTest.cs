using JoinRpg.Common.WebInfrastructure.Job;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace JoinRpg.Common.WebInfrastructure.Test.Job;

public class MidnightJobsSchedulerTest
{
    public class SampleJob : IJob
    {
        public Task RunOnce(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private class FakeJobRepository : IJobRepository
    {
        public List<JobId> Inserted { get; } = [];

        public List<JobId> Completed { get; } = [];

        public List<JobId> Failed { get; } = [];

        /// <summary>Разрешить вставку: «мы первые», дальше планировщик запускает раннер.</summary>
        public bool WinInsertRace { get; set; }

        // false — «джоба уже запущена на другом экземпляре»: так проверяем запись в журнал, не поднимая JobRunner.
        public Task<bool> TryInsertJobRecord(JobId jobId)
        {
            Inserted.Add(jobId);
            return Task.FromResult(WinInsertRace);
        }

        public Task<bool> TrySetJobCompleted(JobId jobId)
        {
            Completed.Add(jobId);
            return Task.FromResult(true);
        }

        public Task<bool> TrySetJobFailed(JobId jobId)
        {
            Failed.Add(jobId);
            return Task.FromResult(true);
        }
    }

    private class FakeJobRunner(Type jobType, Exception? throwOnRun = null) : IJobRunner
    {
        public int RunCount { get; private set; }

        public Task RunJob(IServiceScope serviceScope, CancellationToken cancellationToken)
        {
            RunCount++;
            return throwOnRun is null ? Task.CompletedTask : Task.FromException(throwOnRun);
        }

        public string Name => jobType.Name;
        public string FullName => jobType.FullName!;
        public Type JobType => jobType;
    }

    private class FakeOptionsMonitor(JobsOptions value) : IOptionsMonitor<JobsOptions>
    {
        public JobsOptions CurrentValue { get; set; } = value;
        public JobsOptions Get(string? name) => CurrentValue;
        public IDisposable? OnChange(Action<JobsOptions, string?> listener) => null;
    }

    private class FakeLifetime : IHostApplicationLifetime
    {
        public CancellationToken ApplicationStarted => CancellationToken.None;
        public CancellationToken ApplicationStopping => CancellationToken.None;
        public CancellationToken ApplicationStopped => CancellationToken.None;
        public void StopApplication() { }
    }

    private static readonly DateOnly Today = new(2026, 5, 15);

    private readonly FakeJobRepository repository = new();

    private MidnightJobsScheduler CreateService(FakeOptionsMonitor options, FakeJobRunner? jobRunner = null)
    {
        var provider = new ServiceCollection()
            .AddSingleton<IJobRepository>(repository)
            .AddSingleton<IJobRunner>(jobRunner ?? new FakeJobRunner(typeof(SampleJob)))
            .BuildServiceProvider();
        return new MidnightJobsScheduler(
            provider,
            NullLogger<MidnightJobsScheduler>.Instance,
            new FakeLifetime(),
            options,
            TimeProvider.System,
            [new RegisteredJob(typeof(SampleJob))]);
    }

    [Fact]
    public void NextRunIsNextUtcMidnightPlusDelayAndJitter()
    {
        var now = new DateTimeOffset(2026, 5, 15, 13, 0, 0, TimeSpan.Zero);

        var (dayOfRun, startAt) = MidnightJobsScheduler.GetNextRun(now, TimeSpan.FromHours(9), TimeSpan.FromSeconds(2));

        dayOfRun.ShouldBe(new DateOnly(2026, 5, 16));
        startAt.ShouldBe(new DateTimeOffset(2026, 5, 16, 9, 0, 2, TimeSpan.Zero));
    }

    [Theory]
    [InlineData(3)] // Москва: местная дата уже сменилась, UTC — ещё нет
    [InlineData(-5)] // западнее UTC: местная дата отстаёт от UTC
    public void DayOfRunDoesNotDependOnLocalOffset(int offsetHours)
    {
        var utcNow = new DateTimeOffset(2026, 5, 15, 22, 30, 0, TimeSpan.Zero);
        var localNow = utcNow.ToOffset(TimeSpan.FromHours(offsetHours));

        var (dayOfRun, startAt) = MidnightJobsScheduler.GetNextRun(localNow, delay: null, TimeSpan.Zero);

        dayOfRun.ShouldBe(new DateOnly(2026, 5, 16));
        startAt.ShouldBe(new DateTimeOffset(2026, 5, 16, 0, 0, 5, TimeSpan.Zero));
    }

    [Fact]
    public async Task EnabledJobWritesJournalRecord()
    {
        var service = CreateService(new FakeOptionsMonitor(new JobsOptions()));

        await service.RunScheduledOnce(typeof(SampleJob), Today, CancellationToken.None);

        repository.Inserted.ShouldBe([new JobId(typeof(SampleJob).FullName!, Today)]);
    }

    [Fact]
    public async Task DisabledJobDoesNotWriteJournalRecord()
    {
        var service = CreateService(new FakeOptionsMonitor(new JobsOptions { Disabled = ["SampleJob"] }));

        await service.RunScheduledOnce(typeof(SampleJob), Today, CancellationToken.None);

        repository.Inserted.ShouldBeEmpty();
    }

    [Fact]
    public async Task ReEnabledJobRunsOnNextScheduledRunWithoutRestart()
    {
        var options = new FakeOptionsMonitor(new JobsOptions { Disabled = ["SampleJob"] });
        var service = CreateService(options);

        await service.RunScheduledOnce(typeof(SampleJob), Today, CancellationToken.None);
        options.CurrentValue = new JobsOptions();
        await service.RunScheduledOnce(typeof(SampleJob), Today, CancellationToken.None);

        repository.Inserted.ShouldBe([new JobId(typeof(SampleJob).FullName!, Today)]);
    }

    [Fact]
    public async Task SuccessfulJobRunIsMarkedCompleted()
    {
        repository.WinInsertRace = true;
        var runner = new FakeJobRunner(typeof(SampleJob));
        var service = CreateService(new FakeOptionsMonitor(new JobsOptions()), runner);

        await service.RunScheduledOnce(typeof(SampleJob), Today, CancellationToken.None);

        runner.RunCount.ShouldBe(1);
        repository.Completed.ShouldBe([new JobId(typeof(SampleJob).FullName!, Today)]);
        repository.Failed.ShouldBeEmpty();
    }

    [Fact]
    public async Task FailingJobRunIsMarkedFailed()
    {
        repository.WinInsertRace = true;
        var runner = new FakeJobRunner(typeof(SampleJob), new InvalidOperationException("boom"));
        var service = CreateService(new FakeOptionsMonitor(new JobsOptions()), runner);

        await service.RunScheduledOnce(typeof(SampleJob), Today, CancellationToken.None);

        repository.Failed.ShouldBe([new JobId(typeof(SampleJob).FullName!, Today)]);
        repository.Completed.ShouldBeEmpty();
    }
}
