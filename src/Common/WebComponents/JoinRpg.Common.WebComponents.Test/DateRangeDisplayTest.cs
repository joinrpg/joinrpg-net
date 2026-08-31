using System.Globalization;

namespace JoinRpg.Common.WebComponents.Test;

public class DateRangeDisplayTest
{
    private static readonly CultureInfo RuCulture = CultureInfo.GetCultureInfo("ru-RU");

    private sealed class CultureSwitcher : IDisposable
    {
        private readonly CultureInfo _originalCulture;
        private readonly CultureInfo _originalUiCulture;

        public CultureSwitcher(CultureInfo culture)
        {
            _originalCulture = CultureInfo.CurrentCulture;
            _originalUiCulture = CultureInfo.CurrentUICulture;
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
        }

        public void Dispose()
        {
            CultureInfo.CurrentCulture = _originalCulture;
            CultureInfo.CurrentUICulture = _originalUiCulture;
        }
    }

    [Fact]
    public void SingleDay_RendersDateAndWeekdayTooltip()
    {
        using var _ = new CultureSwitcher(RuCulture);
        using var ctx = new BunitContext();
        var cut = ctx.Render<DateRangeDisplay>(p => p
            .Add(x => x.Start, new DateOnly(2026, 3, 1))
            .Add(x => x.End, new DateOnly(2026, 3, 1)));

        cut.Markup.ShouldContain("1 марта 2026");
        cut.Markup.ShouldContain("воскресенье");
    }

    [Fact]
    public void SameMonth_RendersDateRangeAndWeekdayTooltip()
    {
        using var _ = new CultureSwitcher(RuCulture);
        using var ctx = new BunitContext();
        var cut = ctx.Render<DateRangeDisplay>(p => p
            .Add(x => x.Start, new DateOnly(2026, 3, 1))
            .Add(x => x.End, new DateOnly(2026, 3, 5)));

        cut.Markup.ShouldContain("1–5 марта 2026");
        cut.Markup.ShouldContain("воскресенье–четверг");
    }

    [Fact]
    public void DifferentMonths_RendersDateRangeAndWeekdayTooltip()
    {
        using var _ = new CultureSwitcher(RuCulture);
        using var ctx = new BunitContext();
        var cut = ctx.Render<DateRangeDisplay>(p => p
            .Add(x => x.Start, new DateOnly(2026, 3, 1))
            .Add(x => x.End, new DateOnly(2026, 4, 5)));

        cut.Markup.ShouldContain("1 марта–5 апреля 2026");
        cut.Markup.ShouldContain("воскресенье–воскресенье");
    }

    [Fact]
    public void DifferentYears_RendersShortDateRangeAndWeekdayTooltip()
    {
        using var _ = new CultureSwitcher(RuCulture);
        using var ctx = new BunitContext();
        var cut = ctx.Render<DateRangeDisplay>(p => p
            .Add(x => x.Start, new DateOnly(2026, 3, 1))
            .Add(x => x.End, new DateOnly(2027, 4, 5)));

        cut.Markup.ShouldContain("01.03.2026–05.04.2027");
        cut.Markup.ShouldContain("воскресенье–понедельник");
    }

    [Fact]
    public void HideCurrentYear_RendersWithoutYear()
    {
        var currentYear = DateTime.Now.Year;
        using var _ = new CultureSwitcher(RuCulture);
        using var ctx = new BunitContext();
        var cut = ctx.Render<DateRangeDisplay>(p => p
            .Add(x => x.Start, new DateOnly(currentYear, 3, 1))
            .Add(x => x.End, new DateOnly(currentYear, 3, 5))
            .Add(x => x.HideCurrentYear, true));

        cut.Markup.ShouldContain("1–5 марта");
        cut.Markup.ShouldNotContain(currentYear.ToString());
    }
}
