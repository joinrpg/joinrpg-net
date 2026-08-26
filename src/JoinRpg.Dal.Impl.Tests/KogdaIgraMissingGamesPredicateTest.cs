using JoinRpg.Dal.Impl.Repositories;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Projects;
using Shouldly;
using Xunit;

namespace JoinRpg.Dal.Impl.Tests;

public class KogdaIgraMissingGamesPredicateTest
{
    private static readonly DateTime Now = new DateTime(2025, 6, 1, 12, 0, 0);

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
        => TestPredicate(CreateProject(), Now.AddDays(-10)).ShouldBeTrue();

    [Fact]
    public void NoGames_StaleProject_ShouldNeedBinding()
        // Даже устаревший проект без привязки — кандидат (нужна привязка)
        => TestPredicate(CreateProject(), Now.AddDays(-90)).ShouldBeTrue();

    // --- Будущие игры (ещё не прошли) ---

    [Fact]
    public void FutureGame_RecentlyUpdated_ShouldNotNeedBinding()
        => TestPredicate(CreateProject([GameEndingIn(30)]), Now.AddDays(-10)).ShouldBeFalse();

    [Fact]
    public void FutureGame_StaleProject_ShouldNotNeedBinding()
        // Привязан к будущей игре — не нужна новая привязка
        => TestPredicate(CreateProject([GameEndingIn(30)]), Now.AddDays(-90)).ShouldBeFalse();

    [Fact]
    public void GameEndingToday_ShouldNotNeedBinding()
        // End == Now: граница, игра «ещё не закончилась»
        => TestPredicate(CreateProject([GameEndingIn(0)]), Now.AddDays(-10)).ShouldBeFalse();

    // --- Игра только что закончилась: пост-обработка — не сигнал сериала ---

    [Fact]
    public void PastGame_JustEndedAndJustClosedOut_ShouldNotNeedBinding()
        // Игра закончилась вчера, проект обновили сразу после (итоги, фото) —
        // это закрытие игры, а не признак того, что готовится следующая часть
        => TestPredicate(CreateProject([GameEndingIn(-1)]), Now.AddDays(-1)).ShouldBeFalse();

    [Fact]
    public void PastGame_JustEndedAndUpdatedToday_ShouldNotNeedBinding()
        // Игра закончилась вчера, обновление сегодня — зазор всего 1 день, недостаточно
        => TestPredicate(CreateProject([GameEndingIn(-1)]), Now).ShouldBeFalse();

    // --- Игра закончилась давно: важен зазор End → lastUpdated, а не «now» ---

    [Fact]
    public void PastGame_OldEnough_ActivityLongAfterEnd_ShouldNeedBinding()
        // Игра закончилась 90 дней назад, но проект продолжают обновлять спустя 85 дней
        // после её конца — похоже на живой сериал, готовящий следующую часть
        => TestPredicate(CreateProject([GameEndingIn(-90)]), Now.AddDays(-5)).ShouldBeTrue();

    [Fact]
    public void PastGame_OldEnough_ActivityRightAfterEnd_ShouldNotNeedBinding()
        // Игра закончилась 90 дней назад, обновление было сразу после (закрытие игры),
        // а дальше — тишина. Проект скорее мёртв, чем готовит сиквел
        => TestPredicate(CreateProject([GameEndingIn(-90)]), Now.AddDays(-85)).ShouldBeFalse();

    [Fact]
    public void PastGame_StaleProject_ShouldNotNeedBinding()
        // Обновляли задолго до конца игры, дальше активности не было
        => TestPredicate(CreateProject([GameEndingIn(-1)]), Now.AddDays(-90)).ShouldBeFalse();

    // --- Граница: ровно 60 дней между End и lastUpdated ---

    [Fact]
    public void PastGame_GapExactly60Days_ShouldNotNeedBinding()
    {
        var end = Now.AddDays(-100);
        var updatedExactly60DaysAfterEnd = end.AddDays(60);
        TestPredicate(CreateProject([new KogdaIgraGame { KogdaIgraGameId = 1, Active = true, End = end, Name = "Test Game" }]), updatedExactly60DaysAfterEnd)
            .ShouldBeFalse();
    }

    [Fact]
    public void PastGame_GapJustOver60Days_ShouldNeedBinding()
    {
        var end = Now.AddDays(-100);
        var updatedJustOver60DaysAfterEnd = end.AddDays(61);
        TestPredicate(CreateProject([new KogdaIgraGame { KogdaIgraGameId = 1, Active = true, End = end, Name = "Test Game" }]), updatedJustOver60DaysAfterEnd)
            .ShouldBeTrue();
    }

    // --- Неактивные привязки ---

    [Fact]
    public void OnlyInactiveGame_RecentlyUpdated_ShouldNeedBinding()
        // Привязка помечена как неактивная — игнорируется, как нет привязки
        => TestPredicate(CreateProject([InactiveGame(30)]), Now.AddDays(-10)).ShouldBeTrue();

    [Fact]
    public void OnlyInactiveGame_StaleProject_ShouldNeedBinding()
        => TestPredicate(CreateProject([InactiveGame(30)]), Now.AddDays(-90)).ShouldBeTrue();

    // --- Несколько игр (сериал) ---

    [Fact]
    public void MixedGames_OnePastOneFuture_ShouldNotNeedBinding()
        // Есть и прошедшая и будущая игра — привязан, не нуждается
        => TestPredicate(CreateProject([GameEndingIn(-30), GameEndingIn(30)]), Now.AddDays(-10)).ShouldBeFalse();

    [Fact]
    public void MultiplePastGames_ActivityLongAfterLastEnd_ShouldNeedBinding()
        // Все игры прошли, но зазор считается от самой последней (-90) —
        // обновление спустя 85 дней после неё указывает на живой сериал
        => TestPredicate(
            CreateProject([GameEndingIn(-120), GameEndingIn(-100), GameEndingIn(-90)]),
            Now.AddDays(-5)).ShouldBeTrue();

    [Fact]
    public void MultiplePastGames_ActivityRightAfterLastEnd_ShouldNotNeedBinding()
        // Обновление было сразу после последней игры (-90), дальше тишина
        => TestPredicate(
            CreateProject([GameEndingIn(-120), GameEndingIn(-100), GameEndingIn(-90)]),
            Now.AddDays(-95)).ShouldBeFalse();

    // --- Граничный случай: End = null (дата окончания ещё не известна) ---

    [Fact]
    public void GameWithNullEnd_RecentlyUpdated_ShouldNotNeedBinding()
        // End неизвестен — считаем игру ещё не завершённой (как будущую),
        // поэтому проект не считается кандидатом на новую привязку
        => TestPredicate(CreateProject([GameWithNullEnd()]), Now.AddDays(-10)).ShouldBeFalse();

    [Fact]
    public void GameWithNullEnd_StaleProject_ShouldNotNeedBinding()
        => TestPredicate(CreateProject([GameWithNullEnd()]), Now.AddDays(-90)).ShouldBeFalse();

    // --- Проект с DisableKogdaIgraMapping ---

    [Fact]
    public void DisableKogdaIgraMapping_ShouldNotNeedBinding()
        // Проект с отключённой привязкой к КогдаИгра — не попадает в выборку
        => TestPredicate(CreateProject(disableKogdaIgraMapping: true), Now.AddDays(-10)).ShouldBeFalse();

    // --- Неактивный проект ---

    [Fact]
    public void InactiveProject_ShouldNotNeedBinding()
        // Неактивный проект — не попадает в выборку
        => TestPredicate(CreateProject(active: false), Now.AddDays(-10)).ShouldBeFalse();
}
