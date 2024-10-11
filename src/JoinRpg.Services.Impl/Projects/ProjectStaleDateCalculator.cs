
namespace JoinRpg.Services.Impl.Projects;

internal class ProjectStaleDateCalculator
{
    internal static DateOnly CalculateCloseDate(DateOnly lastUpdated)
    {
        DateOnly[] dates = [
            lastUpdated.AddMonths(7),
            new DateOnly(2024, 10, 20)];

        return MoveUntilDayOfMonth(dates.Max(), lastUpdated.Day);
    }

    private static DateOnly MoveUntilDayOfMonth(DateOnly maxDate, int day)
    {
        if (maxDate.Day > day)
        {
            maxDate = maxDate.AddMonths(1);
        }

        return new DateOnly(maxDate.Year, maxDate.Month, day);
    }
}
