using JoinRpg.Services.Impl.Projects;

namespace JoinRpg.Services.Impl.Test;
public class ProjectStaleDateCalculatorTests
{
    [Fact]
    public void ShouldNotCloseNotStale()
        => ProjectStaleDateCalculator.CalculateStaleStatus(lastUpdated: new DateOnly(2025, 07, 06), today: new DateOnly(2025, 08, 06))
        .ShouldBe((ProjectStaleStatus.NotStale, DateOnly.MaxValue));

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
            .ShouldBe((ProjectStaleStatus.StaleWarnOtherTime, new DateOnly(2025, 10, 01)));
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
