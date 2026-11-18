namespace JoinRpg.Common.WebInfrastructure.LeaderElection;

/// <summary>
/// Распределённый лок для выбора лидера из N реплик на базе PostgreSQL advisory lock.
/// </summary>
internal interface ILeaderLock
{
    /// <summary>
    /// Попытаться захватить лок. Возвращает <c>null</c>, если лок уже занят другим
    /// (реплика не лидер). Удерживающий лок экземпляр — получивший handle.
    /// </summary>
    Task<ILeaderLockHandle?> TryAcquireAsync(long lockKey, CancellationToken cancellationToken = default);
}
