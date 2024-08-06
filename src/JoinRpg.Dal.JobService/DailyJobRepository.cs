using EntityFramework.Exceptions.Common;
using JoinRpg.Data.Write.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace JoinRpg.Dal.JobService;
public class DailyJobRepository(JobScheduleDataDbContext dbContext) : IDailyJobRepository
{
    async Task<bool> IDailyJobRepository.TryInsertJobRecord(JobId jobId)
    {
        _ = dbContext.DailyJobRuns.Add(new DailyJobRun()
        {
            DayOfRun = jobId.DayOfRun,
            JobName = jobId.JobName,
            MachineName = Environment.MachineName
        });

        try
        {
            _ = await dbContext.SaveChangesAsync();
        }
        catch (UniqueConstraintException)
        {
            return false; // Проиграли гонку
        }
        return true; // Выиграли, запускайте джоб
    }
    async Task<bool> IDailyJobRepository.TrySetJobCompleted(JobId jobId) => await TrySetJobStatus(dbContext, jobId, DailyJobStatus.Completed);
    async Task<bool> IDailyJobRepository.TrySetJobFailed(JobId jobId) => await TrySetJobStatus(dbContext, jobId, DailyJobStatus.Error);

    private static async Task<bool> TrySetJobStatus(JobScheduleDataDbContext dbContext, JobId jobId, DailyJobStatus targetStatus)
    {
        // Всегда трогаем только джобы, запущенные нами и в состоянии started
        var totalRows = await dbContext.DailyJobRuns
            .Where(d => d.JobName == jobId.JobName && d.DayOfRun == jobId.DayOfRun && d.MachineName == Environment.MachineName && d.JobStatus == DailyJobStatus.Started)
            .ExecuteUpdateAsync(setters => setters.SetProperty(d => d.JobStatus, targetStatus));

        return
            totalRows switch
            {
                0 => false,
                1 => true,
                _ => throw new InvalidOperationException("Unexpected — too many rows updated")
            };
    }


}
