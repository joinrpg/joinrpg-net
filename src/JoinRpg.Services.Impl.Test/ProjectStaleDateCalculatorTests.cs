using JoinRpg.Services.Impl.Projects;

namespace JoinRpg.Services.Impl.Test;

public class ProjectStaleDateCalculatorTests
{
    [Fact]
    public void ShouldNotCloseNotStale()
        => ProjectStaleDateCalculator.CalculateStaleStatus(lastUpdated: new DateOnly(2025, 07, 06), today: new DateOnly(2025, 08, 06))
        .ShouldBe((ProjectStaleStatus.NotStale, DateOnly.MaxValue));

    [Theory]
    [InlineData(01, 09)] // Январь + 9 = Октябрь, 31
    [InlineData(03, 09)] // Март + 9 = Декабрь, 31
    [InlineData(03, 10)] // Март + 10 = Январь, 31
    [InlineData(05, 10)] // Май + 10 = Март, 31
    [InlineData(08, 09)] // Август + 9 = Май, 31
    [InlineData(10, 9)]  // Октябрь + 9 = Июль, 31
    [InlineData(10, 10)] // Октябрь + 10 = Август, 31
    [InlineData(12, 10)] // Декабрь + 10 = Октябрь, 31
    public void ShouldNotThrowOn31CloseToday(int lastUpdateMonth, int monthDistance)
    {
        var lastUpdated = new DateOnly(2025, lastUpdateMonth, 31);
        DateOnly today = lastUpdated.AddMonths(monthDistance);
        ProjectStaleDateCalculator.CalculateStaleStatus(lastUpdated, today)
            .ShouldBe((ProjectStaleStatus.StaleCloseToday, today));
    }

    [Theory]
    [InlineData(01, 10)]
    [InlineData(03, 08)] // Март + 8 = Ноябрь, 30
    [InlineData(05, 09)]
    [InlineData(07, 09)]
    [InlineData(07, 10)]
    [InlineData(08, 10)]
    [InlineData(10, 8)]  // Октябрь + 8 = Июнь, 30 дней переезжает на 1ое
    [InlineData(12, 09)]
    public void ShouldNotThrowOn31WarnOtherTime(int lastUpdateMonth, int monthDistance)
    {
        var lastUpdated = new DateOnly(2025, lastUpdateMonth, 31);
        ProjectStaleDateCalculator.CalculateStaleStatus(lastUpdated, today: lastUpdated.AddMonths(monthDistance))
            .status
            .ShouldBe(ProjectStaleStatus.StaleWarnOtherTime);
    }

    [Theory]
    [InlineData(01, 31, ProjectStaleStatus.NotStale)]
    [InlineData(02, 28, ProjectStaleStatus.NotStale)]
    [InlineData(03, 31, ProjectStaleStatus.NotStale)]
    [InlineData(04, 01, ProjectStaleStatus.NotStale)]
    [InlineData(04, 30, ProjectStaleStatus.StaleWarnToday)]
    [InlineData(05, 01, ProjectStaleStatus.StaleWarnOtherTime)]
    [InlineData(05, 31, ProjectStaleStatus.StaleWarnToday)]
    [InlineData(06, 30, ProjectStaleStatus.StaleWarnToday)]
    [InlineData(07, 01, ProjectStaleStatus.StaleWarnOtherTime)]
    [InlineData(07, 31, ProjectStaleStatus.StaleWarnToday)]
    [InlineData(08, 31, ProjectStaleStatus.StaleCloseToday)]
    [InlineData(10, 1, ProjectStaleStatus.StaleWarnOtherTime)]
    [InlineData(10, 31, ProjectStaleStatus.StaleCloseToday)]
    internal void ShouldHasCorrectDatesFor31(int month, int day, ProjectStaleStatus should)
    {
        var lastUpdated = new DateOnly(2025, 01, 31);
        var today = new DateOnly(2025, month, day);
        ProjectStaleDateCalculator.CalculateStaleStatus(lastUpdated, today)
            .status
            .ShouldBe(should);
    }

    [Fact]
    public void ShouldCalculateCloseThresholdCorrectly()
    {
        ProjectStaleDateCalculator.GetCloseThreshold(new DateOnly(2025, 01, 31))
            .ShouldBe(new DateOnly(2025, 8, 31));
    }

    [Fact]
    public void ShouldCloseAfterSevenMonthsExact()
    {
        var today = new DateOnly(2025, 08, 01);
        ProjectStaleDateCalculator
            .CalculateStaleStatus(lastUpdated: new DateOnly(2025, 1, 1), today: today)
            .ShouldBe((ProjectStaleStatus.StaleCloseToday, today));
    }

    [Fact]
    public void ShouldNotCloseAfterSixMonthsWhenNotExact()
    {
        var today = new DateOnly(2025, 09, 02);
        ProjectStaleDateCalculator
            .CalculateStaleStatus(lastUpdated: new DateOnly(2025, 1, 1), today: today)
            .ShouldBe((ProjectStaleStatus.StaleWarnOtherTime, new DateOnly(2025, 08, 01)));
    }

    [Fact]
    public void ShouldWarnWhenSameDayAndStale()
    {
        var today = new DateOnly(2025, 06, 01);
        ProjectStaleDateCalculator
            .CalculateStaleStatus(lastUpdated: new DateOnly(2025, 01, 1), today: today)
            .ShouldBe((ProjectStaleStatus.StaleWarnToday, new DateOnly(2025, 08, 01)));
    }

    [Fact]
    public void ShouldWarnAfter4Months()
    {
        var today = new DateOnly(2025, 07, 06);
        ProjectStaleDateCalculator
            .CalculateStaleStatus(lastUpdated: new DateOnly(2025, 02, 16), today: today)
            .ShouldBe((ProjectStaleStatus.StaleWarnOtherTime, new DateOnly(2025, 09, 16)));
    }
}
