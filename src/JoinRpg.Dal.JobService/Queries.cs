using JoinRpg.Data.Write.Interfaces;

namespace JoinRpg.Dal.JobService;

internal static class Queries
{
    public static IQueryable<DailyJobRun> ByJobId(this IQueryable<DailyJobRun> dailyJobRuns, JobId jobId) => dailyJobRuns.Where(d => d.JobName == jobId.JobName && d.DayOfRun == jobId.DayOfRun);
}
