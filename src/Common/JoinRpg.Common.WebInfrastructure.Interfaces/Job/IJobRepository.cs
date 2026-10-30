namespace JoinRpg.Common.WebInfrastructure.Job;

/// <summary>
/// Журнал запусков джоб: «один запуск в сутки» через уникальный индекс (JobName, DayOfRun).
/// </summary>
public interface IJobRepository
{
    /// <summary>
    /// Записать запуск: true — мы первые, джобу запускаем; false — на этом экземпляре уже запущена (или была) сегодня.
    /// </summary>
    Task<bool> TryInsertJobRecord(JobId jobId);

    /// <summary>
    /// Пометить запуск успешным. Трогает только записи, начатые этим экземпляром.
    /// </summary>
    Task<bool> TrySetJobCompleted(JobId jobId);

    /// <summary>
    /// Пометить запуск упавшим. Трогает только записи, начатые этим экземпляром.
    /// </summary>
    Task<bool> TrySetJobFailed(JobId jobId);
}

/// <summary>
/// Идентификатор запуска джобы: имя (полное имя класса) и день запуска.
/// </summary>
/// <param name="JobName">Полное имя класса джобы (<c>Type.FullName</c>).</param>
/// <param name="DayOfRun">День запуска по UTC — ключ «раз в сутки».</param>
public record class JobId(string JobName, DateOnly DayOfRun);
