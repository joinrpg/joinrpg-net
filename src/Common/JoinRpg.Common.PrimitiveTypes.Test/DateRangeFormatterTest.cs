using System.Globalization;

namespace JoinRpg.Common.PrimitiveTypes.Test;

public class DateRangeFormatterTest
{
    private static readonly CultureInfo RuCulture = CultureInfo.GetCultureInfo("ru-RU");

    [Theory]
    [InlineData("2026-03-01", "2026-03-05", "1–5 марта 2026")]
    [InlineData("2026-03-01", "2026-03-01", "1 марта 2026")]
    [InlineData("2026-03-01", "2026-04-05", "1 марта–5 апреля 2026")]
    [InlineData("2026-03-01", "2027-04-05", "01.03.2026–05.04.2027")]
    public void FormatDisplay_WithRussianCulture_ReturnsExpectedString(string begin, string end, string expected)
    {
        var range = new DateRange(DateOnly.Parse(begin), DateOnly.Parse(end));
        DateRangeFormatter.FormatDisplay(range, RuCulture).ShouldBe(expected);
    }

    [Fact]
    public void FormatDisplay_WhenHideCurrentYearAndCurrentYear_HidesYear()
    {
        var currentYear = DateTime.Now.Year;
        var range = new DateRange(new DateOnly(currentYear, 3, 1), new DateOnly(currentYear, 3, 5));
        DateRangeFormatter.FormatDisplay(range, RuCulture, hideCurrentYear: true).ShouldBe("1–5 марта");
    }

    [Fact]
    public void FormatDisplay_WhenHideCurrentYearAndDifferentYear_ShowsYear()
    {
        var range = new DateRange(new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 5));
        DateRangeFormatter.FormatDisplay(range, RuCulture, hideCurrentYear: true).ShouldBe("1–5 марта 2026");
    }

    [Theory]
    [InlineData("2026-03-01", "2026-03-01", "воскресенье")]
    [InlineData("2026-03-01", "2026-03-05", "воскресенье–четверг")]
    public void FormatTooltip_WithRussianCulture_ReturnsExpectedString(string begin, string end, string expected)
    {
        var range = new DateRange(DateOnly.Parse(begin), DateOnly.Parse(end));
        DateRangeFormatter.FormatTooltip(range, RuCulture).ShouldBe(expected);
    }
}
