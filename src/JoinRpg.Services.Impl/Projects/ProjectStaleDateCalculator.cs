
namespace JoinRpg.Services.Impl.Projects;

internal class ProjectStaleDateCalculator
{
    private const int CloseDistanceInMonths = 7;
    private const int WarnDistanceBeforCloseInMonths = 3;

    private static DateOnly MoveUntilDayOfMonth(DateOnly dateToMove, int day)
    {
        if (dateToMove.Day == day)
        {
            return dateToMove;
        }
        do
        {
            dateToMove = dateToMove.AddDays(1);
        }
        while (dateToMove.Day != day
            // Перевалили за границу месяца
            && dateToMove.Day != 1);
        return dateToMove;
    }

    internal static (ProjectStaleStatus status, DateOnly closeDate) CalculateStaleStatus(DateOnly lastUpdated, DateOnly today)
    {
        if (today < lastUpdated)
        {
            throw new InvalidOperationException("Дата обновления проекта в будущем");
        }
        if (lastUpdated.AddMonths(WarnDistanceBeforCloseInMonths) > today)
        {
            return (ProjectStaleStatus.NotStale, DateOnly.MaxValue);
        }

        var closeCandidate = GetCloseThreshold(lastUpdated);

        if (closeCandidate > today)
        {
            // Зона предупреждения
            for (var monthDistance = WarnDistanceBeforCloseInMonths; monthDistance < CloseDistanceInMonths; monthDistance++)
            {
                var warnDate = lastUpdated.AddMonths(monthDistance);
                if (warnDate == today)
                {
                    return (ProjectStaleStatus.StaleWarnToday, closeCandidate);
                }
            }
            return (ProjectStaleStatus.StaleWarnOtherTime, closeCandidate);
        }
        else
        {
            // Зона закрытия
            if (closeCandidate.Day == today.Day)
            {
                return (ProjectStaleStatus.StaleCloseToday, today);
            }
            else
            {
                return (ProjectStaleStatus.StaleWarnOtherTime, closeCandidate);
            }

        }

    }

    internal static DateOnly GetCloseThreshold(DateOnly lastUpdated) => MoveUntilDayOfMonth(lastUpdated.AddMonths(CloseDistanceInMonths), lastUpdated.Day);
}

internal enum ProjectStaleStatus
{
    NotStale,
    StaleWarnToday,
    StaleWarnOtherTime,
    StaleCloseToday,
}
