namespace JoinRpg.Common.WebInfrastructure.DailyJob;

public interface IDailyJobRepository
{
    Task<bool> TryInsertJobRecord(JobId jobId);

    Task<bool> TrySetJobCompleted(JobId jobId);

    Task<bool> TrySetJobFailed(JobId jobId);
}

public record class JobId(string JobName, DateOnly DayOfRun) { }
