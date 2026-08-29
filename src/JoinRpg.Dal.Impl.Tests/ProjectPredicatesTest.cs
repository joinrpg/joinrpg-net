using JoinRpg.Dal.Impl.Repositories;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Projects;
using Shouldly;
using Xunit;

namespace JoinRpg.Dal.Impl.Tests;

public class ProjectPredicatesTest
{
    private static Project CreateProject(IEnumerable<KogdaIgraGame>? games = null) => new()
    {
        Active = true,
        Details = new ProjectDetails(),
        KogdaIgraGames = [.. (games ?? [])],
    };

    private static bool HasFutureKogdaIgraGame(Project project)
        => ProjectPredicates.HasFutureKogdaIgraGame().Compile()(project);

    // Баг: админский отчёт о горячих ролях (AdminHotRolesList) показывал роли из проектов,
    // у которых нет ни одной непрошедшей игры КогдаИгра — то есть больше, чем реально
    // рекламируется через SingleHotRoleAdvertisementJob (там есть проверка NearestFutureKogdaIgraCard).

    [Fact]
    public void NoGames_ShouldNotHaveFutureGame()
        => HasFutureKogdaIgraGame(CreateProject()).ShouldBeFalse();

    [Fact]
    public void OnlyPastGame_ShouldNotHaveFutureGame()
        => HasFutureKogdaIgraGame(CreateProject([
            new KogdaIgraGame { KogdaIgraGameId = 1, Active = true, Begin = DateTime.UtcNow.AddDays(-30), Name = "Прошедшая игра" }
        ])).ShouldBeFalse();

    [Fact]
    public void FutureGame_ShouldHaveFutureGame()
        => HasFutureKogdaIgraGame(CreateProject([
            new KogdaIgraGame { KogdaIgraGameId = 1, Active = true, Begin = DateTime.UtcNow.AddDays(30), Name = "Будущая игра" }
        ])).ShouldBeTrue();

    [Fact]
    public void OnlyInactiveFutureGame_ShouldNotHaveFutureGame()
        // Неактивная привязка (например, ошибочно созданная) — игнорируется, как и у KogdaIgraMissingGamesPredicate
        => HasFutureKogdaIgraGame(CreateProject([
            new KogdaIgraGame { KogdaIgraGameId = 1, Active = false, Begin = DateTime.UtcNow.AddDays(30), Name = "Неактивная будущая игра" }
        ])).ShouldBeFalse();

    [Fact]
    public void MixedPastAndFutureGames_ShouldHaveFutureGame()
        // Сериал: прошедшая игра уже была, но заявлена следующая часть — проект остаётся кандидатом
        => HasFutureKogdaIgraGame(CreateProject([
            new KogdaIgraGame { KogdaIgraGameId = 1, Active = true, Begin = DateTime.UtcNow.AddDays(-30), Name = "Прошедшая игра" },
            new KogdaIgraGame { KogdaIgraGameId = 2, Active = true, Begin = DateTime.UtcNow.AddDays(30), Name = "Будущая игра" },
        ])).ShouldBeTrue();

    [Fact]
    public void GameWithNullBegin_ShouldNotHaveFutureGame()
        // Дата начала ещё не синхронизирована — не считаем это будущей игрой
        => HasFutureKogdaIgraGame(CreateProject([
            new KogdaIgraGame { KogdaIgraGameId = 1, Active = true, Begin = null, Name = "Без даты начала" }
        ])).ShouldBeFalse();
}
