using JoinRpg.WebPortal.Models.FieldSetup;

namespace JoinRpg.WebPortal.Models.Test;

public class TimeSlotStartTimeParseTest
{
    private static readonly TimeZoneInfo Novosibirsk = TimeZoneInfo.FindSystemTimeZoneById("Asia/Novosibirsk");

    [Fact]
    public void WithoutOffset_UsesProjectTimeZone()
        => GameFieldDropdownValueViewModelBase.ParseStartTime("2026-07-10T10:00", Novosibirsk)
            .ShouldBe(new DateTimeOffset(2026, 7, 10, 10, 0, 0, TimeSpan.FromHours(7)));

    [Fact]
    public void WithOffset_KeepsIt()
        => GameFieldDropdownValueViewModelBase.ParseStartTime("2026-07-10T10:00+03:00", Novosibirsk)
            .ShouldBe(new DateTimeOffset(2026, 7, 10, 10, 0, 0, TimeSpan.FromHours(3)));
}
