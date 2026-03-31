using JoinRpg.WebPortal.Managers.Schedule;

namespace JoinRpg.WebPortal.Managers.Test.Schedule;

public class TimeZoneGuesserTests
{
    [Theory]
    [InlineData(0, "UTC")]
    [InlineData(1, "Central European Standard Time")]
    [InlineData(2, "Kaliningrad Standard Time")]
    [InlineData(3, "Russian Standard Time")]
    [InlineData(4, "Russia Time Zone 3")]
    [InlineData(5, "Ekaterinburg Standard Time")]
    [InlineData(6, "Omsk Standard Time")]
    [InlineData(7, "North Asia Standard Time")]
    [InlineData(8, "North Asia East Standard Time")]
    [InlineData(9, "Yakutsk Standard Time")]
    [InlineData(10, "Vladivostok Standard Time")]
    [InlineData(11, "Russia Time Zone 10")]
    [InlineData(12, "Russia Time Zone 11")]
    public void GuessTimeZoneByOffset_KnownOffset_ReturnsCorrectTimeZone(int hours, string expectedId)
    {
        var result = TimeZoneGuesser.GuessTimeZoneByOffset(TimeSpan.FromHours(hours));
        result.ShouldNotBeNull();
        result.Id.ShouldBe(expectedId);
    }

    [Theory]
    [InlineData(-5)]
    [InlineData(13)]
    [InlineData(-1)]
    public void GuessTimeZoneByOffset_UnknownOffset_ReturnsNull(int hours)
    {
        var result = TimeZoneGuesser.GuessTimeZoneByOffset(TimeSpan.FromHours(hours));
        result.ShouldBeNull();
    }
}
