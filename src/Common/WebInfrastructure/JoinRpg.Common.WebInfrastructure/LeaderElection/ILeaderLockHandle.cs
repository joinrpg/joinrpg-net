namespace JoinRpg.Common.WebInfrastructure.LeaderElection;

/// <summary>
/// Владение advisory lock'ом PostgreSQL. <see cref="IAsyncDisposable.DisposeAsync"/> освобождает
/// лок и закрывает выделенное под него соединение. Если владелец умер раньше — сервер освобождает
/// лок сам при обрыве сессии, и handle становится пустышкой.
/// </summary>
internal interface ILeaderLockHandle : IAsyncDisposable
{
    /// <summary>
    /// Проверка, что лок ещё жив (соединение, держащее его, не умерло). Бросает при мёртвом соединении.
    /// </summary>
    Task ProbeAsync();
}
