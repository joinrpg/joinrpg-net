namespace JoinRpg.Common.WebInfrastructure.LeaderElection;

/// <summary>
/// Логика, которую нужно крутить ровно на одной реплике из N. Сервис запускается, пока
/// реплика — лидер, и получает отмену токена при потере лидерства или остановке хоста;
/// реализация обязана завершаться по отмене (см. <c>Task.Delay(..., cancellationToken)</c>).
/// </summary>
public interface ILeaderElectedService
{
    Task ExecuteAsync(CancellationToken cancellationToken);
}
