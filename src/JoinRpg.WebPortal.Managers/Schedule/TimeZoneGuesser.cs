namespace JoinRpg.WebPortal.Managers.Schedule;

internal static class TimeZoneGuesser
{
    private static readonly Dictionary<TimeSpan, string> OffsetToWindowsId = new()
    {
        [TimeSpan.FromHours(0)] = "UTC",
        [TimeSpan.FromHours(1)] = "Central European Standard Time",
        [TimeSpan.FromHours(2)] = "Kaliningrad Standard Time",
        [TimeSpan.FromHours(3)] = "Russian Standard Time",
        [TimeSpan.FromHours(4)] = "Russia Time Zone 3",
        [TimeSpan.FromHours(5)] = "Ekaterinburg Standard Time",
        [TimeSpan.FromHours(6)] = "Omsk Standard Time",
        [TimeSpan.FromHours(7)] = "North Asia Standard Time",
        [TimeSpan.FromHours(8)] = "North Asia East Standard Time",
        [TimeSpan.FromHours(9)] = "Yakutsk Standard Time",
        [TimeSpan.FromHours(10)] = "Vladivostok Standard Time",
        [TimeSpan.FromHours(11)] = "Russia Time Zone 10",
        [TimeSpan.FromHours(12)] = "Russia Time Zone 11",
    };

    public static TimeZoneInfo? GuessTimeZoneByOffset(TimeSpan offset)
    {
        if (!OffsetToWindowsId.TryGetValue(offset, out var id))
        {
            return null;
        }

        return TimeZoneInfo.FindSystemTimeZoneById(id);
    }
}
