using EntityFramework.Exceptions.Common;
using JoinRpg.Data.Write.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace JoinRpg.Dal.JobService;
public class DailyJobRepository(JobScheduleDataDbContext dbContext) : IDailyJobRepository
{
    async Task<bool> IDailyJobRepository.TryInsertJobRecord(JobId jobId)
    {
        if (await dbContext.DailyJobRuns.ByJobId(jobId).AnyAsync())
        {
            // Давайте проверим, нет ли такой записи уже, чтобы не засирать лог ошибками
            return false;
        }
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
        var totalRows = await dbContext.DailyJobRuns
            .ByJobId(jobId)
            .Where(d => d.MachineName == Environment.MachineName && d.JobStatus == DailyJobStatus.Started) // Всегда трогаем только джобы, запущенные нами и в состоянии started
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
