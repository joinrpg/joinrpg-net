using JoinRpg.DomainTypes.ProjectMetadata.Payments;

namespace JoinRpg.DomainTypes.Test.ProjectMetadata;

/// <summary>
/// Расписание взносов повторяет поведение <c>FinanceExtensions.ProjectFeeForDate</c>, которое
/// раньше работало прямо по EF-сущностям. Тесты фиксируют именно его, включая неочевидные углы.
/// </summary>
public class ProjectFinanceSettingsTest
{
    private static readonly DateTime Day10 = new(2026, 3, 10);
    private static readonly DateTime Day20 = new(2026, 3, 20);

    private static ProjectFinanceSettings Make(params ProjectFeeSettingInfo[] schedule)
        => new(PreferentialFeeEnabled: true, PaymentTypes: [], FeeSchedule: schedule);

    [Fact]
    public void EmptyScheduleMeansNoFee()
    {
        var settings = Make();

        settings.GetFeeSettingForDate(Day20).ShouldBeNull();
        settings.GetFeeForDate(Day20, preferential: false).ShouldBe(0);
    }

    [Fact]
    public void BeforeFirstStartDateThereIsNoFee()
    {
        var settings = Make(new ProjectFeeSettingInfo(Day20, Fee: 1000, PreferentialFee: 500, ProjectFeeSettingId: 1));

        settings.GetFeeSettingForDate(Day10).ShouldBeNull();
        settings.GetFeeForDate(Day10, preferential: false).ShouldBe(0);
    }

    [Fact]
    public void LatestStartedRowWins()
    {
        var settings = Make(
            new ProjectFeeSettingInfo(Day10, Fee: 1000, PreferentialFee: 500, ProjectFeeSettingId: 2),
            new ProjectFeeSettingInfo(Day20, Fee: 2000, PreferentialFee: 900, ProjectFeeSettingId: 3));

        settings.GetFeeForDate(Day20, preferential: false).ShouldBe(2000);
        settings.GetFeeForDate(Day10, preferential: false).ShouldBe(1000);
    }

    [Fact]
    public void ScheduleOrderDoesNotMatter()
    {
        var ascending = Make(
            new ProjectFeeSettingInfo(Day10, Fee: 1000, PreferentialFee: null, ProjectFeeSettingId: 4),
            new ProjectFeeSettingInfo(Day20, Fee: 2000, PreferentialFee: null, ProjectFeeSettingId: 5));
        var descending = Make(
            new ProjectFeeSettingInfo(Day20, Fee: 2000, PreferentialFee: null, ProjectFeeSettingId: 5),
            new ProjectFeeSettingInfo(Day10, Fee: 1000, PreferentialFee: null, ProjectFeeSettingId: 4));

        descending.GetFeeForDate(Day20, preferential: false)
            .ShouldBe(ascending.GetFeeForDate(Day20, preferential: false));
    }

    [Fact]
    public void AmongRowsWithSameStartDateLastCreatedWins()
    {
        // Баг #5053: если на одну дату заведено несколько строк, действовать должна последняя
        // созданная (с большим Id), а не произвольная.
        var settings = Make(
            new ProjectFeeSettingInfo(Day20, Fee: 2000, PreferentialFee: 900, ProjectFeeSettingId: 12),
            new ProjectFeeSettingInfo(Day20, Fee: 3000, PreferentialFee: 1500, ProjectFeeSettingId: 13),
            new ProjectFeeSettingInfo(Day10, Fee: 1000, PreferentialFee: 500, ProjectFeeSettingId: 14));

        settings.GetFeeSettingForDate(Day20).ShouldNotBeNull().ProjectFeeSettingId.ShouldBe(13);
        settings.GetFeeForDate(Day20, preferential: false).ShouldBe(3000);
        settings.GetFeeForDate(Day20, preferential: true).ShouldBe(1500);
    }

    [Fact]
    public void AmongRowsWithSameStartDateOrderInScheduleDoesNotMatter()
    {
        // Порядок в коллекции не должен влиять: Id больше — строка побеждает.
        var settings = Make(
            new ProjectFeeSettingInfo(Day20, Fee: 3000, PreferentialFee: null, ProjectFeeSettingId: 13),
            new ProjectFeeSettingInfo(Day20, Fee: 2000, PreferentialFee: null, ProjectFeeSettingId: 12));

        settings.GetFeeForDate(Day20, preferential: false).ShouldBe(3000);
    }

    [Fact]
    public void RowStartsWorkingOnItsOwnStartDate()
    {
        var settings = Make(new ProjectFeeSettingInfo(Day20, Fee: 2000, PreferentialFee: null, ProjectFeeSettingId: 8));

        settings.GetFeeForDate(Day20, preferential: false).ShouldBe(2000);
    }

    [Fact]
    public void TimeOfDayIsIgnored()
    {
        // Сравнение идёт по .Date: строка, начинающаяся 20-го в 00:00, действует и в 23:59 того же дня.
        var settings = Make(new ProjectFeeSettingInfo(Day20, Fee: 2000, PreferentialFee: null, ProjectFeeSettingId: 9));

        settings.GetFeeForDate(Day20.AddHours(23).AddMinutes(59), preferential: false).ShouldBe(2000);
    }

    [Fact]
    public void PreferentialFeeIsTakenWhenAsked()
    {
        var settings = Make(new ProjectFeeSettingInfo(Day10, Fee: 1000, PreferentialFee: 400, ProjectFeeSettingId: 10));

        settings.GetFeeForDate(Day10, preferential: true).ShouldBe(400);
    }

    [Fact]
    public void MissingPreferentialFeeMeansZeroNotRegularFee()
    {
        // Так же ведёт себя старый ProjectFeeForDate: (preferential ? f.PreferentialFee : f.Fee) ?? 0.
        var settings = Make(new ProjectFeeSettingInfo(Day10, Fee: 1000, PreferentialFee: null, ProjectFeeSettingId: 11));

        settings.GetFeeForDate(Day10, preferential: true).ShouldBe(0);
    }
}
