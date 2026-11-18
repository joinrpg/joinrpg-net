namespace JoinRpg.Web.ProjectMasterTools.ClaimRestore;

/// <summary>
/// Восстановление комментариев к заявкам из базы уведомлений (DRP, #5492). Запускается после
/// восстановления заявок (<see cref="IClaimRestoreClient"/>). Только в заблокированном проекте (ADR023).
/// </summary>
public interface ICommentRestoreClient
{
    Task<CommentRestorePlanViewModel> GetPlan(ProjectIdentification projectId);

    /// <summary>
    /// Восстанавливает комментарии одной заявки. План сервер строит заново: клиент присылает только
    /// номер заявки. Каждая заявка — отдельный запрос.
    /// </summary>
    Task<CommentRestoreApplyResult> Apply(ClaimIdentification claimId);
}
