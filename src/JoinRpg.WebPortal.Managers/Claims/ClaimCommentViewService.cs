using JoinRpg.Data.Interfaces.Characters;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Claims.Comments;

namespace JoinRpg.WebPortal.Managers.Claims;

/// <summary>
/// Комментарий к заявке из острова (<c>FinanceModerationButton</c>). Запрос — в терминах заявки, а
/// запись идёт через общий вход обсуждения (ADR026 §5): права и правила, включая «скрыть может только
/// мастер», проверяет он.
/// </summary>
internal class ClaimCommentViewService(
    ICommentDiscussionService commentDiscussionService,
    IClaimInfoRepository claimInfoRepository) : IClaimCommentClient
{
    public async Task AddComment(AddClaimCommentRequest request)
    {
        var claim = await claimInfoRepository.GetClaimInCharacterOrDefault(request.ClaimId)
            ?? throw new JoinRpgEntityNotFoundException(request.ClaimId.ClaimId, nameof(ClaimIdentification));

        await commentDiscussionService.AddComment(
            claim.Claim.CommentDiscussionId,
            request.ParentCommentId is { } parent ? new CommentId(parent.CommentId) : null,
            request.IsVisibleToPlayer,
            request.CommentText);
    }
}
