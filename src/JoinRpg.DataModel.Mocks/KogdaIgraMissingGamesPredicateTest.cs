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

    // --- Нет привязок ---

    [Fact]
    public void NoGames_RecentlyUpdated_ShouldNeedBinding()
        => KogdaIgraMissingGamesPredicate.NeedsBinding([], RecentlyUpdated, Now)
            .ShouldBeTrue();

    [Fact]
    public void NoGames_StaleProject_ShouldNeedBinding()
        // Даже устаревший проект без привязки — кандидат (нужна привязка)
        => KogdaIgraMissingGamesPredicate.NeedsBinding([], StaleUpdated, Now)
            .ShouldBeTrue();

    // --- Будущие игры (ещё не прошли) ---

    [Fact]
    public void FutureGame_RecentlyUpdated_ShouldNotNeedBinding()
        => KogdaIgraMissingGamesPredicate.NeedsBinding([GameEndingIn(30)], RecentlyUpdated, Now)
            .ShouldBeFalse();

    [Fact]
    public void FutureGame_StaleProject_ShouldNotNeedBinding()
        // Привязан к будущей игре — не нужна новая привязка
        => KogdaIgraMissingGamesPredicate.NeedsBinding([GameEndingIn(30)], StaleUpdated, Now)
            .ShouldBeFalse();

    [Fact]
    public void GameEndingToday_ShouldNotNeedBinding()
        // End == Now: граница, игра «ещё не закончилась»
        => KogdaIgraMissingGamesPredicate.NeedsBinding([GameEndingIn(0)], RecentlyUpdated, Now)
            .ShouldBeFalse();

    // --- Прошедшие игры ---

    [Fact]
    public void PastGame_RecentlyUpdated_ShouldNeedBinding()
        // Сериал: игра прошла, проект активен → надо привязать следующую
        => KogdaIgraMissingGamesPredicate.NeedsBinding([GameEndingIn(-1)], RecentlyUpdated, Now)
            .ShouldBeTrue();

    [Fact]
    public void PastGame_StaleProject_ShouldNotNeedBinding()
        // Игра прошла, проект неактивен — не показывать в выборке
        => KogdaIgraMissingGamesPredicate.NeedsBinding([GameEndingIn(-1)], StaleUpdated, Now)
            .ShouldBeFalse();

    [Fact]
    public void PastGame_UpdatedExactlyAtBoundary_ShouldNotNeedBinding()
    {
        // Граница: обновлено ровно 60 дней назад — не попадает в выборку
        var updatedAt60DaysAgo = Now.AddDays(-60);
        KogdaIgraMissingGamesPredicate.NeedsBinding([GameEndingIn(-1)], updatedAt60DaysAgo, Now)
            .ShouldBeFalse();
    }

    [Fact]
    public void PastGame_UpdatedJustUnderBoundary_ShouldNeedBinding()
    {
        // Граница: обновлено 59 дней назад — попадает в выборку
        var updatedAt59DaysAgo = Now.AddDays(-59);
        KogdaIgraMissingGamesPredicate.NeedsBinding([GameEndingIn(-1)], updatedAt59DaysAgo, Now)
            .ShouldBeTrue();
    }

    // --- Неактивные привязки ---

    [Fact]
    public void OnlyInactiveGame_RecentlyUpdated_ShouldNeedBinding()
        // Привязка помечена как неактивная — игнорируется, как нет привязки
        => KogdaIgraMissingGamesPredicate.NeedsBinding([InactiveGame(30)], RecentlyUpdated, Now)
            .ShouldBeTrue();

    [Fact]
    public void OnlyInactiveGame_StaleProject_ShouldNeedBinding()
        => KogdaIgraMissingGamesPredicate.NeedsBinding([InactiveGame(30)], StaleUpdated, Now)
            .ShouldBeTrue();

    // --- Несколько игр (сериал) ---

    [Fact]
    public void MixedGames_OnePastOneFuture_ShouldNotNeedBinding()
        // Есть и прошедшая и будущая игра — привязан, не нуждается
        => KogdaIgraMissingGamesPredicate.NeedsBinding(
            [GameEndingIn(-30), GameEndingIn(30)],
            RecentlyUpdated, Now)
            .ShouldBeFalse();

    [Fact]
    public void MultiplePastGames_RecentlyUpdated_ShouldNeedBinding()
        // Все игры прошли, проект активен → нужна новая привязка
        => KogdaIgraMissingGamesPredicate.NeedsBinding(
            [GameEndingIn(-60), GameEndingIn(-30), GameEndingIn(-1)],
            RecentlyUpdated, Now)
            .ShouldBeTrue();

    [Fact]
    public void MultiplePastGames_StaleProject_ShouldNotNeedBinding()
        => KogdaIgraMissingGamesPredicate.NeedsBinding(
            [GameEndingIn(-60), GameEndingIn(-30), GameEndingIn(-1)],
            StaleUpdated, Now)
            .ShouldBeFalse();

    // --- Граничный случай: End = null ---

    [Fact]
    public void GameWithNullEnd_RecentlyUpdated_ShouldNeedBinding()
        // Игра без даты окончания: End=null → null >= now = false → hasActiveOrFutureGame=false
        // Но hasAnyGame=true, поэтому проверяется активность проекта
        => KogdaIgraMissingGamesPredicate.NeedsBinding([GameWithNullEnd()], RecentlyUpdated, Now)
            .ShouldBeTrue();

    [Fact]
    public void GameWithNullEnd_StaleProject_ShouldNotNeedBinding()
        => KogdaIgraMissingGamesPredicate.NeedsBinding([GameWithNullEnd()], StaleUpdated, Now)
            .ShouldBeFalse();
}
