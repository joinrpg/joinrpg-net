namespace JoinRpg.WebPortal.Models.Test;

public class OperationDateConversionTest
{
    [Fact]
    public void DateOnlyToDateTime_MatchesPlainDateTimeParse()
    {
        var fromDateTimeParse = DateTime.Parse("19.08.2026", System.Globalization.CultureInfo.GetCultureInfo("ru-RU"));
        var fromDateOnlyConversion = DateOnly.Parse("19.08.2026", System.Globalization.CultureInfo.GetCultureInfo("ru-RU"))
            .ToDateTime(TimeOnly.MinValue);

        fromDateOnlyConversion.ShouldBe(fromDateTimeParse);
        fromDateOnlyConversion.TimeOfDay.ShouldBe(TimeSpan.Zero);
        fromDateOnlyConversion.Kind.ShouldBe(DateTimeKind.Unspecified);
    }
}
