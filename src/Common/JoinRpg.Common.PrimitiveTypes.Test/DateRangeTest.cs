namespace JoinRpg.Common.PrimitiveTypes.Test;

public class DateRangeTest
{
    [Fact]
    public void Constructor_WhenEndBeforeBegin_Throws()
    {
        Should.Throw<ArgumentOutOfRangeException>(
            () => new DateRange(new DateOnly(2026, 3, 5), new DateOnly(2026, 3, 1)));
    }

    [Fact]
    public void Constructor_WhenEndEqualsBegin_DoesNotThrow()
    {
        var date = new DateOnly(2026, 3, 1);
        var range = new DateRange(date, date);
        range.Begin.ShouldBe(date);
        range.End.ShouldBe(date);
    }

    [Fact]
    public void Constructor_WhenEndAfterBegin_DoesNotThrow()
    {
        var range = new DateRange(new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 5));
        range.Begin.ShouldBe(new DateOnly(2026, 3, 1));
        range.End.ShouldBe(new DateOnly(2026, 3, 5));
    }
}
