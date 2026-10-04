using JoinRpg.DomainTypes.ProjectMetadata;

namespace JoinRpg.DomainTypes.Test.ProjectMetadata;

public class TimeSlotOptionsTest
{
    [Fact]
    public void CreateDefault_StartsNowInProjectTimeZone()
    {
        var zone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Novosibirsk");
        var now = new DateTimeOffset(2026, 7, 10, 8, 0, 0, TimeSpan.Zero);

        var options = TimeSlotOptions.CreateDefault(zone, now);

        options.StartTime.ShouldBe(now);
        options.StartTime.Offset.ShouldBe(TimeSpan.FromHours(7));
        options.StartTime.Hour.ShouldBe(15);
    }

    [Fact]
    public void CreateDefault_UsesSummerOffset()
    {
        var zone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");

        var options = TimeSlotOptions.CreateDefault(zone, new DateTimeOffset(2026, 7, 10, 8, 0, 0, TimeSpan.Zero));

        options.StartTime.Offset.ShouldBe(TimeSpan.FromHours(2));
    }
}
