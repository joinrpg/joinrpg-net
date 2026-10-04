using JoinRpg.WebPortal.Managers.Schedule;

namespace JoinRpg.WebPortal.Managers.Test.Schedule;

public class IcalScheduleTest
{
    [Fact]
    public void SlotTime_IsConvertedToProjectTimeZone()
    {
        // Слот ввели по Москве, а проект — в Новосибирске
        var zone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Novosibirsk");
        var slotStart = new DateTimeOffset(2026, 7, 10, 10, 0, 0, TimeSpan.FromHours(3));

        var result = SchedulePageManager.ToCalDateTime(slotStart, zone);

        result.TzId.ShouldBe("Asia/Novosibirsk");
        result.Value.ShouldBe(new DateTime(2026, 7, 10, 14, 0, 0));
    }
}
