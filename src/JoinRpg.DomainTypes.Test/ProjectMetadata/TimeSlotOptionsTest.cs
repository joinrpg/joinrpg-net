using JoinRpg.DomainTypes.ProjectMetadata;

namespace JoinRpg.DomainTypes.Test.ProjectMetadata;

public class TimeSlotOptionsTest
{
    private static readonly TimeZoneInfo Novosibirsk = TimeZoneInfo.FindSystemTimeZoneById("Asia/Novosibirsk");
    private static readonly TimeZoneInfo Berlin = TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");

    [Fact]
    public void CreateDefault_StartsNowOnProjectClock()
    {
        var options = TimeSlotOptions.CreateDefault(Novosibirsk, new DateTimeOffset(2026, 7, 10, 8, 0, 0, TimeSpan.Zero));

        options.LocalStartTime.ShouldBe(new DateTime(2026, 7, 10, 15, 0, 0));
    }

    [Fact]
    public void ToJson_HasNoOffset()
        => new TimeSlotOptions(new DateTime(2026, 7, 10, 10, 0, 0), 50).ToJson()
            .ShouldBe("""{"StartTime":"2026-07-10T10:00:00","TimeSlotInMinutes":50}""");

    [Fact]
    public void FromJson_RoundTrips()
    {
        var options = new TimeSlotOptions(new DateTime(2026, 7, 10, 10, 0, 0), 50);

        TimeSlotOptions.FromJson(options.ToJson()).ShouldBe(options);
    }

    /// <summary>
    /// Раньше начало сохранялось со смещением (и с вычисляемыми полями) — смещение игнорируем
    /// </summary>
    [Theory]
    [InlineData("""{"StartTime":"2026-07-10T10:00:00+03:00","TimeSlotInMinutes":50,"TimeSlotLength":"00:50:00","EndTime":"2026-07-10T10:50:00+03:00"}""")]
    [InlineData("""{"StartTime":"2026-07-10T10:00:00+07:00","TimeSlotInMinutes":50}""")]
    [InlineData("""{"StartTime":"2026-07-10T10:00:00Z","TimeSlotInMinutes":50}""")]
    public void FromJson_LegacyOffset_IsIgnored(string json)
    {
        var options = TimeSlotOptions.FromJson(json).ShouldNotBeNull();

        options.LocalStartTime.ShouldBe(new DateTime(2026, 7, 10, 10, 0, 0));
        options.TimeSlotInMinutes.ShouldBe(50);
    }

    [Fact]
    public void StartTime_UsesProjectTimeZone()
        => new TimeSlotOptions(new DateTime(2026, 7, 10, 10, 0, 0), 50).GetStartTime(Novosibirsk)
            .ShouldBe(new DateTimeOffset(2026, 7, 10, 10, 0, 0, TimeSpan.FromHours(7)));

    [Fact]
    public void StartTime_FollowsDaylightSavingTime()
    {
        new TimeSlotOptions(new DateTime(2026, 7, 10, 10, 0, 0), 50).GetStartTime(Berlin).Offset.ShouldBe(TimeSpan.FromHours(2));
        new TimeSlotOptions(new DateTime(2026, 1, 10, 10, 0, 0), 50).GetStartTime(Berlin).Offset.ShouldBe(TimeSpan.FromHours(1));
    }

    [Fact]
    public void EndTime_AcrossDaylightSavingTransition_LastsRealMinutes()
    {
        // 29.03.2026 в 02:00 в Берлине часы переводят на 03:00
        var options = new TimeSlotOptions(new DateTime(2026, 3, 29, 1, 30, 0), 60);

        var start = options.GetStartTime(Berlin);
        var end = options.GetEndTime(Berlin);

        (end - start).ShouldBe(TimeSpan.FromMinutes(60));
        end.ShouldBe(new DateTimeOffset(2026, 3, 29, 3, 30, 0, TimeSpan.FromHours(2)));
    }
}
