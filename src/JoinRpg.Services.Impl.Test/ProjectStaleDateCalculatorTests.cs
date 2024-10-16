using JoinRpg.Services.Impl.Projects;

namespace JoinRpg.Services.Impl.Test;
public class ProjectStaleDateCalculatorTests
{
    [Fact]
    public void InFutureShouldBeSixMonthLater()
    {
        var lastUpdated = new DateOnly(2025, 1, 1);
        ProjectStaleDateCalculator.CalculateCloseDate(lastUpdated).ShouldBe(new DateOnly(2025, 8, 1));
    }

    [Fact]
    public void ShouldBeAfterNovebmer2024()
    {
        var lastUpdated = new DateOnly(2021, 1, 1);
        ProjectStaleDateCalculator.CalculateCloseDate(lastUpdated).ShouldBe(new DateOnly(2024, 11, 1));
    }

    [Fact]
    public void Scrackanobaza()
    {
        var lastUpdated = new DateOnly(2024, 7, 16);
        ProjectStaleDateCalculator.CalculateCloseDate(lastUpdated).ShouldBe(new DateOnly(2025, 2, 16));
    }
}
