namespace JoinRpg.Web.Claims.Finance;

/// <summary>
/// Подтверждение и отклонение финансовой операции. Простой комментарий к операции — через
/// <see cref="Comments.IClaimCommentClient"/>: это обычный ответ на комментарий, в котором она заведена.
/// </summary>
public interface IFinanceModerationClient
{
    Task Approve(FinanceOperationIdentification operationId, FinanceModerationDecisionRequest request);

    Task Decline(FinanceOperationIdentification operationId, FinanceModerationDecisionRequest request);
}

/// <param name="CommentText">
/// Комментарий к решению: при подтверждении можно не писать, при отклонении обязателен.
/// Решение по операции игрок видит всегда.
/// </param>
public record FinanceModerationDecisionRequest(string? CommentText);
