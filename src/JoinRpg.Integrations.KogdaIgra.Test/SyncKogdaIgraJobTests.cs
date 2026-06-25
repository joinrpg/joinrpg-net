using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Services.Interfaces.Integrations.KogdaIgra;
using Microsoft.Extensions.Logging.Abstractions;

namespace JoinRpg.Integrations.KogdaIgra.Test;

public class SyncKogdaIgraJobTests
{
    [Fact]
    public async Task RunOnce_WhenNoPendingGames_ShouldPerformSyncAtLeastOnce()
    {
        var service = new FakeKogdaIgraSyncService()
        {
            InitialStatus = new SyncStatus(10, DateTimeOffset.UnixEpoch, PendingGamesCount: 0),
            PerformSyncResults = [new SyncStatus(10, DateTimeOffset.UnixEpoch, PendingGamesCount: 0)],
        };
        var job = new SyncKogdaIgraJob(service, NullLogger<SyncKogdaIgraJob>.Instance);

        await job.RunOnce(CancellationToken.None);

        service.PerformSyncCallCount.ShouldBe(1);
    }

    [Fact]
    public async Task RunOnce_WhenPendingGamesDecrease_ShouldRepeatSyncUntilNoProgress()
    {
        var service = new FakeKogdaIgraSyncService()
        {
            InitialStatus = new SyncStatus(10, DateTimeOffset.UnixEpoch, PendingGamesCount: 3),
            PerformSyncResults =
            [
                new SyncStatus(10, DateTimeOffset.UnixEpoch, PendingGamesCount: 2),
                new SyncStatus(10, DateTimeOffset.UnixEpoch, PendingGamesCount: 1),
                new SyncStatus(10, DateTimeOffset.UnixEpoch, PendingGamesCount: 0),
            ],
        };
        var job = new SyncKogdaIgraJob(service, NullLogger<SyncKogdaIgraJob>.Instance);

        await job.RunOnce(CancellationToken.None);

        service.PerformSyncCallCount.ShouldBe(3);
    }

    [Fact]
    public async Task RunOnce_WhenPendingGamesCountDoesNotDecrease_ShouldStopAfterOneIteration()
    {
        var service = new FakeKogdaIgraSyncService()
        {
            InitialStatus = new SyncStatus(10, DateTimeOffset.UnixEpoch, PendingGamesCount: 2),
            PerformSyncResults =
            [
                new SyncStatus(10, DateTimeOffset.UnixEpoch, PendingGamesCount: 2),
            ],
        };
        var job = new SyncKogdaIgraJob(service, NullLogger<SyncKogdaIgraJob>.Instance);

        await job.RunOnce(CancellationToken.None);

        service.PerformSyncCallCount.ShouldBe(1);
    }

    private sealed class FakeKogdaIgraSyncService : IKogdaIgraSyncService
    {
        public required SyncStatus InitialStatus { get; init; }
        public required SyncStatus[] PerformSyncResults { get; init; }

        public int PerformSyncCallCount { get; private set; }

        public Task<SyncStatus> ForceResyncGames(KogdaIgraIdentification[] gameIds) => throw new NotImplementedException();
        public Task<SyncStatus> GetSyncStatus() => Task.FromResult(InitialStatus);

        public Task<SyncStatus> PerformSync()
        {
            var result = PerformSyncResults[PerformSyncCallCount];
            PerformSyncCallCount++;
            return Task.FromResult(result);
        }
    }
}
