using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;
using JoinRpg.WebPortal.Managers.Schedule;

namespace JoinRpg.WebPortal.Managers.Test.Schedule;

public class IcalScheduleTest
{
    private static CalendarEvent MakeEvent(DateTime start, string tzId)
        => new() { Start = new CalDateTime(start, tzId), End = new CalDateTime(start.AddHours(1), tzId), Summary = "Событие" };

    [Fact]
    public void Calendar_DescribesTimeZoneOfEvents()
    {
        var calendar = SchedulePageManager.CreateCalendar([
            MakeEvent(new DateTime(2026, 7, 10, 10, 0, 0), "Europe/Berlin"),
            MakeEvent(new DateTime(2026, 7, 11, 10, 0, 0), "Europe/Berlin"),
        ]);

        calendar.TimeZones.ShouldHaveSingleItem().TzId.ShouldBe("Europe/Berlin");

        var ical = new CalendarSerializer().SerializeToString(calendar).ShouldNotBeNull();
        ical.ShouldContain("BEGIN:VTIMEZONE");
        ical.ShouldContain("TZID:Europe/Berlin");
        // Летнее и зимнее время описаны
        ical.ShouldContain("BEGIN:DAYLIGHT");
        ical.ShouldContain("BEGIN:STANDARD");
        ical.ShouldContain("DTSTART;TZID=Europe/Berlin:20260710T100000");
    }

    /// <summary>
    /// Ical.Net описывает пояс от (earliest − 1 год) до «сейчас» и падает, если начало позже «сейчас»
    /// </summary>
    [Fact]
    public void EventsMoreThanYearAhead_DoNotBreakCalendar()
    {
        var calendar = SchedulePageManager.CreateCalendar([MakeEvent(DateTime.Now.AddYears(2), "Europe/Berlin")]);

        calendar.TimeZones.ShouldHaveSingleItem().TzId.ShouldBe("Europe/Berlin");
    }

    [Fact]
    public void Moscow_HasOnlyStandardTime()
    {
        var calendar = SchedulePageManager.CreateCalendar([MakeEvent(new DateTime(2026, 7, 10, 10, 0, 0), "Europe/Moscow")]);

        var ical = new CalendarSerializer().SerializeToString(calendar).ShouldNotBeNull();
        ical.ShouldContain("TZID:Europe/Moscow");
        ical.ShouldContain("TZOFFSETTO:+0300");
        ical.ShouldNotContain("BEGIN:DAYLIGHT");
    }

    [Fact]
    public void EachTimeZone_IsDescribedOnce()
    {
        var calendar = SchedulePageManager.CreateCalendar([
            MakeEvent(new DateTime(2026, 7, 10, 10, 0, 0), "Europe/Berlin"),
            MakeEvent(new DateTime(2026, 7, 10, 10, 0, 0), "Europe/Moscow"),
            MakeEvent(new DateTime(2026, 7, 11, 10, 0, 0), "Europe/Moscow"),
        ]);

        calendar.TimeZones.Select(z => z.TzId).ShouldBe(["Europe/Berlin", "Europe/Moscow"], ignoreOrder: true);
    }

    [Fact]
    public void EmptyCalendar_HasNoTimeZones()
        => SchedulePageManager.CreateCalendar([]).TimeZones.ShouldBeEmpty();
}
