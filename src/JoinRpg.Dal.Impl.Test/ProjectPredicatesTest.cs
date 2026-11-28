using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Dal.Impl.Repositories;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Projects;
using Shouldly;
using Xunit;

namespace JoinRpg.Dal.Impl.Test;

public class ProjectPredicatesTest
{
    private static Project CreateProject(IEnumerable<KogdaIgraGame>? games = null) => new()
    {
        Active = true,
        Details = new ProjectDetails(),
        KogdaIgraGames = [.. (games ?? [])],
    };

    private static KogdaIgraGame Game(int id, Region? regionId, int? status = 0, bool active = true, double beginInDays = 30) => new()
    {
        KogdaIgraGameId = id,
        Active = active,
        Begin = DateTime.UtcNow.AddDays(beginInDays),
        Name = "Игра",
        Status = status,
        RegionId = regionId,
    };

    private static bool HasFutureKogdaIgraGame(Project project)
        => ProjectPredicates.HasFutureKogdaIgraGame().Compile()(project);

    private static bool HasFutureOkKogdaIgraGameInRegions(Project project, params Region[] regions)
        => ProjectPredicates.HasFutureOkKogdaIgraGameInRegions(regions).Compile()(project);

    // Баг: админский отчёт о горячих ролях (AdminHotRolesList) показывал роли из проектов,
    // у которых нет ни одной непрошедшей игры КогдаИгра — то есть больше, чем реально
    // рекламируется через AdvertisementJob (там есть проверка NearestFutureOkKogdaIgraCard).

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

    [Fact]
    public void FutureOkGameInRegion_ShouldMatch()
        => HasFutureOkKogdaIgraGameInRegions(
            CreateProject([Game(1, Region.Moskva)]),
            Region.Moskva, Region.SanktPeterburg).ShouldBeTrue();

    [Fact]
    public void FutureOkGameInOtherRegion_ShouldNotMatch()
        => HasFutureOkKogdaIgraGameInRegions(
            CreateProject([Game(1, Region.Moskva)]),
            Region.SanktPeterburg).ShouldBeFalse();

    [Fact]
    public void FutureOkGameWithNullRegion_ShouldNotMatch()
        // Регион неизвестен (не ресинкнута или id вне справочника) — при включённом фильтре исключается
        => HasFutureOkKogdaIgraGameInRegions(
            CreateProject([Game(1, null)]),
            Region.Moskva).ShouldBeFalse();

    [Fact]
    public void PastOkGameInRegion_ShouldNotMatch()
        => HasFutureOkKogdaIgraGameInRegions(
            CreateProject([Game(1, Region.Moskva, beginInDays: -30)]),
            Region.Moskva).ShouldBeFalse();

    [Fact]
    public void InactiveFutureOkGameInRegion_ShouldNotMatch()
        => HasFutureOkKogdaIgraGameInRegions(
            CreateProject([Game(1, Region.Moskva, active: false)]),
            Region.Moskva).ShouldBeFalse();

    [Theory]
    [InlineData(1)] // Прошла
    [InlineData(3)] // Отложена
    [InlineData(null)] // Статус неизвестен
    public void FutureGameWithNotOkStatus_ShouldNotMatch(int? status)
        => HasFutureOkKogdaIgraGameInRegions(
            CreateProject([Game(1, Region.Moskva, status: status)]),
            Region.Moskva).ShouldBeFalse();

    [Fact]
    public void OneOfTwoGamesInRegion_ShouldMatch()
        // Прошлая игра другого региона не мешает будущей игре из фильтра
        => HasFutureOkKogdaIgraGameInRegions(
            CreateProject(
            [
                Game(1, Region.SanktPeterburg, beginInDays: -30),
                Game(2, Region.Moskva),
            ]),
            Region.Moskva).ShouldBeTrue();
}
