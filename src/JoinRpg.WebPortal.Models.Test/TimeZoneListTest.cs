using JoinRpg.Common.WebComponents;

namespace JoinRpg.WebPortal.Models.Test;

public class TimeZoneListTest
{
    [Fact]
    public void ContainsMoscowWithOffsetInDisplayName()
    {
        var moscow = TimeZoneList.All.Where(z => z.Id == "Europe/Moscow").ShouldHaveSingleItem();
        moscow.DisplayName.ShouldBe("(UTC+03:00) Europe/Moscow");
    }

    [Fact]
    public void ContainsOnlyIanaIds()
    {
        TimeZoneList.All.ShouldNotContain(z => z.Id == "Russian Standard Time");
        TimeZoneList.All.ShouldAllBe(z => TimeZoneInfo.FindSystemTimeZoneById(z.Id).HasIanaId);
    }

    [Fact]
    public void IdsAreUnique() => TimeZoneList.All.Select(z => z.Id).ShouldBeUnique();

    [Fact]
    public void SortedByOffset()
        => TimeZoneList.All.Select(z => z.BaseUtcOffset).ShouldBe(TimeZoneList.All.Select(z => z.BaseUtcOffset).Order());

    [Fact]
    public void NegativeOffsetFormattedWithMinus()
        => new TimeZoneListItem("America/New_York", TimeSpan.FromHours(-5)).DisplayName.ShouldBe("(UTC-05:00) America/New_York");

    [Fact]
    public void NonWholeHourOffsetFormatted()
        => new TimeZoneListItem("Asia/Kathmandu", new TimeSpan(5, 45, 0)).DisplayName.ShouldBe("(UTC+05:45) Asia/Kathmandu");

    [Fact]
    public void WithSelectedKeepsKnownZoneOnce()
        => TimeZoneList.WithSelected("Europe/Moscow").Count(z => z.Id == "Europe/Moscow").ShouldBe(1);

    [Fact]
    public void WithSelectedAddsUnknownZone()
        => TimeZoneList.WithSelected("Unknown/Zone").ShouldContain(z => z.Id == "Unknown/Zone");

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void WithEmptySelectedReturnsAll(string? selected)
        => TimeZoneList.WithSelected(selected).Length.ShouldBe(TimeZoneList.All.Count);
}
