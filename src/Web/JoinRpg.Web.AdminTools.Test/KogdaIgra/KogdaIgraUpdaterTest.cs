using Bunit;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Web.AdminTools.KogdaIgra;
using JoinRpg.Web.ProjectCommon.KogdaIgra;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace JoinRpg.Web.AdminTools.Test.KogdaIgra;

/// <summary>
/// Список необновлённых игр КогдаИгры: у игр с нестандартным статусом (отменена, отложена и т.п.)
/// рядом с названием показывается подпись статуса, у нормальных — нет (issue #6298).
/// </summary>
public class KogdaIgraUpdaterTest : BunitContext
{
    private readonly FakeSyncClient syncClient = new();

    public KogdaIgraUpdaterTest()
    {
        Services.AddSingleton<IKogdaIgraSyncClient>(syncClient);
    }

    private static KogdaIgraShortViewModel Game(int id, string name, int? year, KogdaIgraGameStatus? status)
        => new(new KogdaIgraIdentification(id), name, new Uri($"https://kogda-igra.ru/game/{id}"), year, status);

    [Fact]
    public void NotUpdatedGames_WarningStatusesShown_NormalStatusesHidden()
    {
        syncClient.NotUpdated =
        [
            Game(1, "Живая игра", 2026, KogdaIgraGameStatus.Ok),
            Game(2, "Прошедшая игра", 2025, KogdaIgraGameStatus.Passed),
            Game(3, "Отменённая игра", 2026, KogdaIgraGameStatus.Cancelled),
            Game(4, "Отложенная игра", null, KogdaIgraGameStatus.Postponed),
            Game(5, "Игра без статуса", 2026, null),
        ];

        var cut = Render<KogdaIgraUpdater>();

        cut.WaitForAssertion(() => cut.FindAll("ul li").Count.ShouldBe(5));
        var items = cut.FindAll("ul li");
        items[0].TextContent.ShouldBe("Живая игра");
        items[1].TextContent.ShouldBe("Прошедшая игра");
        items[2].TextContent.ShouldBe("Отменённая игра — Отменена");
        items[3].TextContent.ShouldBe("Отложенная игра — Отложена");
        items[4].TextContent.ShouldBe("Игра без статуса");
    }

    private sealed class FakeSyncClient : IKogdaIgraSyncClient
    {
        public KogdaIgraShortViewModel[] NotUpdated { get; set; } = [];

        public Task<KogdaIgraShortViewModel[]> GetKogdaIgraNotUpdated() => Task.FromResult(NotUpdated);

        public Task<SyncStatusViewModel> GetSyncStatus()
            => Task.FromResult(new SyncStatusViewModel(NotUpdated.Length, DateTimeOffset.UtcNow, NotUpdated.Length));

        public Task<ResyncOperationResultsViewModel> ResyncKograIgra() => throw new NotSupportedException();

        public Task<KogdaIgraShortViewModel[]> GetKogdaIgraCandidates() => throw new NotSupportedException();

        public Task<KogdaIgraShortViewModel[]> GetFutureKogdaIgraCandidates() => throw new NotSupportedException();

        public Task<KogdaIgraCardViewModel[]> GetKogdaIgraCards(IReadOnlyCollection<KogdaIgraIdentification> kogdaIgraIds) => throw new NotSupportedException();

        public Task<ResyncOperationResultsViewModel> ForceResyncGames(KogdaIgraIdentification[] gameIds) => throw new NotSupportedException();

        public Task<ResyncOperationResultsViewModel> ScheduleUpdateAllGames() => throw new NotSupportedException();

        public Task<ResyncOperationResultsViewModel> RunSyncJob() => throw new NotSupportedException();
    }
}
