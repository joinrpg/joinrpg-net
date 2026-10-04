using JoinRpg.WebPortal.Models.FieldSetup;

namespace JoinRpg.WebPortal.Models.Test;

public class TimeSlotStartTimeParseTest
{
    [Fact]
    public void WithoutOffset_IsProjectClock()
        => GameFieldDropdownValueViewModelBase.ParseStartTime("2026-07-10T10:00")
            .ShouldBe(new DateTime(2026, 7, 10, 10, 0, 0));

    [Fact]
    public void WithOffset_OffsetIsIgnored()
        => GameFieldDropdownValueViewModelBase.ParseStartTime("2026-07-10T10:00+07:00")
            .ShouldBe(new DateTime(2026, 7, 10, 10, 0, 0));
}
