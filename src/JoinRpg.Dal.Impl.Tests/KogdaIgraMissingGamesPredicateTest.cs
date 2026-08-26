using JoinRpg.Dal.Impl.Repositories;
using JoinRpg.DataModel.Projects;
using Shouldly;
using Xunit;

namespace JoinRpg.DataModel.Mocks;

public class KogdaIgraMissingGamesPredicateTest
{
    private static readonly DateTime Now = new DateTime(2025, 6, 1, 12, 0, 0);
    private static readonly DateTime RecentlyUpdated = Now.AddDays(-10);
    private static readonly DateTime StaleUpdated = Now.AddDays(-90);

    private static KogdaIgraGame GameEndingIn(int days) => new()
    {
        KogdaIgraGameId = 1,
        Active = true,
        End = Now.AddDays(days),
        Name = "Test Game",
    };

    private static KogdaIgraGame GameWithNullEnd() => new()
    {
        KogdaIgraGameId = 2,
        Active = true,
        End = null,
        Name = "Test Game No End",
    };

    private static KogdaIgraGame InactiveGame(int endDaysOffset) => new()
    {
        KogdaIgraGameId = 3,
        Active = false,
        End = Now.AddDays(endDaysOffset),
        Name = "Inactive Game",
    };

    private static Project CreateProject(
        IEnumerable<KogdaIgraGame>? games = null,
        bool active = true,
        bool disableKogdaIgraMapping = false) => new()
        {
            Active = active,
            Details = new ProjectDetails { DisableKogdaIgraMapping = disableKogdaIgraMapping },
            KogdaIgraGames = [.. (games ?? [])],
        };

    private static bool TestPredicate(Project project, DateTime lastUpdated)
        => KogdaIgraMissingGamesPredicate.GetPredicate(Now).Compile()(project, lastUpdated);

    // --- Нет привязок ---

    [Fact]
    public void NoGames_RecentlyUpdated_ShouldNeedBinding()
        => TestPredicate(CreateProject(), RecentlyUpdated).ShouldBeTrue();

    [Fact]
    public void NoGames_StaleProject_ShouldNeedBinding()
        // Даже устаревший проект без привязки — кандидат (нужна привязка)
        => TestPredicate(CreateProject(), StaleUpdated).ShouldBeTrue();

    // --- Будущие игры (ещё не прошли) ---

    [Fact]
    public void FutureGame_RecentlyUpdated_ShouldNotNeedBinding()
        => TestPredicate(CreateProject([GameEndingIn(30)]), RecentlyUpdated).ShouldBeFalse();

    [Fact]
    public void FutureGame_StaleProject_ShouldNotNeedBinding()
        // Привязан к будущей игре — не нужна новая привязка
        => TestPredicate(CreateProject([GameEndingIn(30)]), StaleUpdated).ShouldBeFalse();

    [Fact]
    public void GameEndingToday_ShouldNotNeedBinding()
        // End == Now: граница, игра «ещё не закончилась»
        => TestPredicate(CreateProject([GameEndingIn(0)]), RecentlyUpdated).ShouldBeFalse();

    // --- Прошедшие игры ---

    [Fact]
    public void PastGame_RecentlyUpdated_ShouldNeedBinding()
        // Сериал: игра прошла, проект активен → надо привязать следующую
        => TestPredicate(CreateProject([GameEndingIn(-1)]), RecentlyUpdated).ShouldBeTrue();

    [Fact]
    public void PastGame_StaleProject_ShouldNotNeedBinding()
        // Игра прошла, проект неактивен — не показывать в выборке
        => TestPredicate(CreateProject([GameEndingIn(-1)]), StaleUpdated).ShouldBeFalse();

    [Fact]
    public void PastGame_UpdatedExactlyAtBoundary_ShouldNotNeedBinding()
    {
        // Граница: обновлено ровно 60 дней назад — не попадает в выборку
        var updatedAt60DaysAgo = Now.AddDays(-60);
        TestPredicate(CreateProject([GameEndingIn(-1)]), updatedAt60DaysAgo).ShouldBeFalse();
    }

    [Fact]
    public void PastGame_UpdatedJustUnderBoundary_ShouldNeedBinding()
    {
        // Граница: обновлено 59 дней назад — попадает в выборку
        var updatedAt59DaysAgo = Now.AddDays(-59);
        TestPredicate(CreateProject([GameEndingIn(-1)]), updatedAt59DaysAgo).ShouldBeTrue();
    }

    // --- Неактивные привязки ---

    [Fact]
    public void OnlyInactiveGame_RecentlyUpdated_ShouldNeedBinding()
        // Привязка помечена как неактивная — игнорируется, как нет привязки
        => TestPredicate(CreateProject([InactiveGame(30)]), RecentlyUpdated).ShouldBeTrue();

    [Fact]
    public void OnlyInactiveGame_StaleProject_ShouldNeedBinding()
        => TestPredicate(CreateProject([InactiveGame(30)]), StaleUpdated).ShouldBeTrue();

    // --- Несколько игр (сериал) ---

    [Fact]
    public void MixedGames_OnePastOneFuture_ShouldNotNeedBinding()
        // Есть и прошедшая и будущая игра — привязан, не нуждается
        => TestPredicate(CreateProject([GameEndingIn(-30), GameEndingIn(30)]), RecentlyUpdated).ShouldBeFalse();

    [Fact]
    public void MultiplePastGames_RecentlyUpdated_ShouldNeedBinding()
        // Все игры прошли, проект активен → нужна новая привязка
        => TestPredicate(
            CreateProject([GameEndingIn(-60), GameEndingIn(-30), GameEndingIn(-1)]),
            RecentlyUpdated).ShouldBeTrue();

    [Fact]
    public void MultiplePastGames_StaleProject_ShouldNotNeedBinding()
        => TestPredicate(
            CreateProject([GameEndingIn(-60), GameEndingIn(-30), GameEndingIn(-1)]),
            StaleUpdated).ShouldBeFalse();

    // --- Граничный случай: End = null ---

    [Fact]
    public void GameWithNullEnd_RecentlyUpdated_ShouldNeedBinding()
        // Игра без даты окончания: End=null → null >= now = false → hasActiveOrFutureGame=false
        // Но hasAnyGame=true, поэтому проверяется активность проекта
        => TestPredicate(CreateProject([GameWithNullEnd()]), RecentlyUpdated).ShouldBeTrue();

    [Fact]
    public void GameWithNullEnd_StaleProject_ShouldNotNeedBinding()
        => TestPredicate(CreateProject([GameWithNullEnd()]), StaleUpdated).ShouldBeFalse();

    // --- Проект с DisableKogdaIgraMapping ---

    [Fact]
    public void DisableKogdaIgraMapping_ShouldNotNeedBinding()
        // Проект с отключённой привязкой к КогдаИгра — не попадает в выборку
        => TestPredicate(CreateProject(disableKogdaIgraMapping: true), RecentlyUpdated).ShouldBeFalse();

    // --- Неактивный проект ---

    [Fact]
    public void InactiveProject_ShouldNotNeedBinding()
        // Неактивный проект — не попадает в выборку
        => TestPredicate(CreateProject(active: false), RecentlyUpdated).ShouldBeFalse();
}
