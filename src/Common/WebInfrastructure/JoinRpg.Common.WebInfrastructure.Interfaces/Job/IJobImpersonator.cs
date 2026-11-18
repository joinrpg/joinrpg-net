namespace JoinRpg.Common.WebInfrastructure.Job;

/// <summary>
/// Имперсонация, под которой выполняются джобы.
/// Вызывается раннером вокруг <see cref="IJob.RunOnce"/>.
/// В пакете JoinRpg.Common реализации нет — интерфейс реализует хост-приложение:
/// в Joinrpg Portal это робот-администратор (`RobotJobImpersonator` в `JoinRpg.Services.Impl`).
/// </summary>
public interface IJobImpersonator
{
    /// <summary>
    /// Включить имперсонацию от имени джоб-пользователя.
    /// </summary>
    void StartAsRobot();

    /// <summary>
    /// Выключить имперсонацию.
    /// </summary>
    void Stop();
}
