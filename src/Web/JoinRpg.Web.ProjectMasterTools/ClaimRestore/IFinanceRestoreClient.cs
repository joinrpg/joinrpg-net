namespace JoinRpg.Web.ProjectMasterTools.ClaimRestore;

/// <summary>
/// Восстановление ручных оплат и возвратов (типы «наличные» и свои) из базы уведомлений (DRP, #5631).
/// Запускается после восстановления заявок (<see cref="IClaimRestoreClient"/>): операции вешаются на
/// существующие заявки. Только в заблокированном проекте (ADR023), право — управление финансами.
/// </summary>
public interface IFinanceRestoreClient
{
    Task<FinanceRestorePlanViewModel> GetPlan(ProjectIdentification projectId);

    /// <summary>
    /// Восстанавливает ручные оплаты одной заявки с выбранными мастером типами оплаты. План сервер строит
    /// заново. Каждая заявка — отдельный запрос.
    /// </summary>
    Task<FinanceRestoreApplyResult> Apply(FinanceRestoreApplyRequest request);

    /// <summary>Создаёт тип оплаты «наличные» на текущего мастера.</summary>
    Task CreateOwnCash(ProjectIdentification projectId);
}
