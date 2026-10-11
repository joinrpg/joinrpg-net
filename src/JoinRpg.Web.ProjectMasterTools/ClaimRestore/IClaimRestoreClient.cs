namespace JoinRpg.Web.ProjectMasterTools.ClaimRestore;

/// <summary>
/// Восстановление заявок из базы уведомлений после потери основной БД (DRP, #5481).
/// Только в заблокированном проекте (ADR023).
/// </summary>
public interface IClaimRestoreClient
{
    Task<ClaimRestorePlanViewModel> GetPlan(ProjectIdentification projectId);

    /// <summary>
    /// Применяет одну строку плана. Каждая строка — отдельный запрос: после неудачной строки
    /// следующая начинается с чистого контекста.
    /// </summary>
    Task<ClaimRestoreApplyResult> Apply(ClaimRestoreApplyRequest request);
}
