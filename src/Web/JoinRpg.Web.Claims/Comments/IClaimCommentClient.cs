using JoinRpg.DomainTypes.Claims;

namespace JoinRpg.Web.Claims.Comments;

/// <summary>
/// Комментарии к заявке. Пока ими пользуется диалог модерации финансовой операции (#3366); задуман как
/// общий клиент для переписываемых комментариев.
/// </summary>
public interface IClaimCommentClient
{
    Task AddComment(AddClaimCommentRequest request);
}

/// <param name="ClaimId">Заявка, к которой оставляется комментарий.</param>
/// <param name="ParentCommentId">Комментарий, на который отвечают; <c>null</c> — комментарий верхнего уровня.</param>
/// <param name="CommentText">Текст комментария, обязателен.</param>
/// <param name="IsVisibleToPlayer"><c>false</c> — только мастерам; скрыть комментарий может только мастер.</param>
public record AddClaimCommentRequest(
    ClaimIdentification ClaimId,
    ClaimCommentIdentification? ParentCommentId,
    string CommentText,
    bool IsVisibleToPlayer);
