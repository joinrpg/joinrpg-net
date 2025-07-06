
namespace JoinRpg.Services.Impl.Projects;

internal class ProjectStaleDateCalculator
{
    private const int CloseDistanceInMonths = 7;
    private const int WarnDistanceBeforCloseInMonths = 3;

    private static DateOnly MoveUntilDayOfMonth(DateOnly maxDate, int day)
    {
        if (maxDate.Day > day)
        {
            maxDate = maxDate.AddMonths(1);
        }

        return new DateOnly(maxDate.Year, maxDate.Month, day);
    }

    internal static (ProjectStaleStatus, DateOnly closeDate) CalculateStaleStatus(DateOnly lastUpdated, DateOnly today)
    {
        var closeCandidate = MoveUntilDayOfMonth(lastUpdated.AddMonths(CloseDistanceInMonths), lastUpdated.Day);
        if (closeCandidate < today)
        {
            closeCandidate = MoveUntilDayOfMonth(today, closeCandidate.Day);
        }
        if (closeCandidate == today)
        {
            return (ProjectStaleStatus.StaleCloseToday, closeCandidate);
        }
        if (today.AddMonths(WarnDistanceBeforCloseInMonths) <= closeCandidate)
        {
            return (ProjectStaleStatus.NotStale, DateOnly.MaxValue);
        }
        if (closeCandidate.Day == today.Day)
        {
            return (ProjectStaleStatus.StaleWarnToday, closeCandidate);
        }
        return (ProjectStaleStatus.StaleWarnOtherTime, closeCandidate);
    }
}

internal enum ProjectStaleStatus
{
    NotStale,
    StaleWarnToday,
    StaleWarnOtherTime,
    StaleCloseToday,
}
