namespace JoinRpg.Common.PrimitiveTypes.Test;

public class DateRangeFormatterTest
{
    [Theory]
    [InlineData(2026, 3, 1, 2026, 3, 1, "1 марта 2026")]
    [InlineData(2026, 3, 1, 2026, 3, 5, "1–5 марта 2026")]
    [InlineData(2026, 3, 1, 2026, 4, 5, "1 марта–5 апреля 2026")]
    [InlineData(2025, 12, 31, 2026, 1, 1, "31.12.2025–01.01.2026")]
    public void Format_ReturnsExpectedString(
        int startYear, int startMonth, int startDay,
        int endYear, int endMonth, int endDay,
        string expected)
    {
        var start = new DateOnly(startYear, startMonth, startDay);
        var end = new DateOnly(endYear, endMonth, endDay);

        DateRangeFormatter.Format(start, end).ShouldBe(expected);
    }

    [Theory]
    [InlineData(2026, 3, 1, 2026, 3, 1, "1 марта 2026 г")]
    [InlineData(2026, 3, 1, 2026, 3, 5, "1–5 марта 2026 г")]
    [InlineData(2026, 3, 1, 2026, 4, 5, "1 марта–5 апреля 2026 г")]
    public void Format_WithAppendYearWord_ReturnsExpectedString(
        int startYear, int startMonth, int startDay,
        int endYear, int endMonth, int endDay,
        string expected)
    {
        var start = new DateOnly(startYear, startMonth, startDay);
        var end = new DateOnly(endYear, endMonth, endDay);

        DateRangeFormatter.Format(start, end, appendYearWord: true).ShouldBe(expected);
    }

    [Fact]
    public void Format_WhenCurrentYearAndHideCurrentYear_OmitsYear()
    {
        var today = DateTime.Now;
        var start = new DateOnly(today.Year, 6, 18);
        var end = new DateOnly(today.Year, 6, 20);

        DateRangeFormatter.Format(start, end, hideCurrentYear: true).ShouldBe("18–20 июня");
    }
}
