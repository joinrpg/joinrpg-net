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
        var settings = Make(new ProjectFeeSettingInfo(Day20, Fee: 1000, PreferentialFee: 500));

        settings.GetFeeSettingForDate(Day10).ShouldBeNull();
        settings.GetFeeForDate(Day10, preferential: false).ShouldBe(0);
    }

    [Fact]
    public void LatestStartedRowWins()
    {
        var settings = Make(
            new ProjectFeeSettingInfo(Day10, Fee: 1000, PreferentialFee: 500),
            new ProjectFeeSettingInfo(Day20, Fee: 2000, PreferentialFee: 900));

        settings.GetFeeForDate(Day20, preferential: false).ShouldBe(2000);
        settings.GetFeeForDate(Day10, preferential: false).ShouldBe(1000);
    }

    [Fact]
    public void ScheduleOrderDoesNotMatter()
    {
        var ascending = Make(
            new ProjectFeeSettingInfo(Day10, Fee: 1000, PreferentialFee: null),
            new ProjectFeeSettingInfo(Day20, Fee: 2000, PreferentialFee: null));
        var descending = Make(
            new ProjectFeeSettingInfo(Day20, Fee: 2000, PreferentialFee: null),
            new ProjectFeeSettingInfo(Day10, Fee: 1000, PreferentialFee: null));

        descending.GetFeeForDate(Day20, preferential: false)
            .ShouldBe(ascending.GetFeeForDate(Day20, preferential: false));
    }

    [Fact]
    public void RowStartsWorkingOnItsOwnStartDate()
    {
        var settings = Make(new ProjectFeeSettingInfo(Day20, Fee: 2000, PreferentialFee: null));

        settings.GetFeeForDate(Day20, preferential: false).ShouldBe(2000);
    }

    [Fact]
    public void TimeOfDayIsIgnored()
    {
        // Сравнение идёт по .Date: строка, начинающаяся 20-го в 00:00, действует и в 23:59 того же дня.
        var settings = Make(new ProjectFeeSettingInfo(Day20, Fee: 2000, PreferentialFee: null));

        settings.GetFeeForDate(Day20.AddHours(23).AddMinutes(59), preferential: false).ShouldBe(2000);
    }

    [Fact]
    public void PreferentialFeeIsTakenWhenAsked()
    {
        var settings = Make(new ProjectFeeSettingInfo(Day10, Fee: 1000, PreferentialFee: 400));

        settings.GetFeeForDate(Day10, preferential: true).ShouldBe(400);
    }

    [Fact]
    public void MissingPreferentialFeeMeansZeroNotRegularFee()
    {
        // Так же ведёт себя старый ProjectFeeForDate: (preferential ? f.PreferentialFee : f.Fee) ?? 0.
        var settings = Make(new ProjectFeeSettingInfo(Day10, Fee: 1000, PreferentialFee: null));

        settings.GetFeeForDate(Day10, preferential: true).ShouldBe(0);
    }
}
