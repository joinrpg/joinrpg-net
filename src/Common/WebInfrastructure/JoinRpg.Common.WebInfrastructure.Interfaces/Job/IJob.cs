namespace JoinRpg.Common.WebInfrastructure.Job;

/// <summary>
/// Фоновая джоба. Ответственность джобы — идемпотентность: если она вчера не отработала,
/// должна сегодня доделать за вчера.
/// </summary>
public interface IJob
{
    /// <summary>
    /// Один запуск джобы. Планировщик вызывает по расписанию; админская страница — вручную.
    /// </summary>
    /// <param name="cancellationToken">Отмена при остановке хоста; джоба должна успеть завершиться.</param>
    Task RunOnce(CancellationToken cancellationToken);
}

/// <summary>
/// Сдвиг планового запуска джобы от полуночи UTC (в часах).
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class JobDelayAttribute(int hoursDelay) : Attribute
{
    /// <summary>
    /// Сдвиг запуска от полуночи UTC.
    /// </summary>
    public TimeSpan Delay { get; } = TimeSpan.FromHours(hoursDelay);
}
