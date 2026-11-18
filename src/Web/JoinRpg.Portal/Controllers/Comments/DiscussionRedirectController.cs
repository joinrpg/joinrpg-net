using JoinRpg.Interfaces;
using JoinRpg.Portal.Controllers.Comments;
using JoinRpg.Portal.Controllers.Common;
using JoinRpg.WebPortal.Managers.Comments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers;

[Route("{ProjectId}/goto/")]
public class DiscussionRedirectController(
    CommentDiscussionOwnerLoader ownerLoader,
    ICurrentUserAccessor currentUserAccessor
    ) : JoinControllerGameBase
{
    [HttpGet("discussion/{CommentDiscussionId}")]
    [Authorize]
    public async Task<ActionResult> ToDiscussion(ProjectIdentification projectId, CommentDiscussionId? commentDiscussionId)
    {
        // Typed id не биндится из нуля и мусора — такого обсуждения нет.
        if (commentDiscussionId is null)
        {
            return NotFound();
        }
        return RedirectToOwner(await ownerLoader.GetOwnerOrDefault(projectId, commentDiscussionId), commentId: null);
    }

    [HttpGet("comment/{CommentId:int}")]
    [Authorize]
    public async Task<ActionResult> ToComment(ProjectIdentification projectId, CommentId? commentId)
    {
        if (commentId is null)
        {
            return NotFound();
        }
        return RedirectToOwner(await ownerLoader.GetOwnerByCommentOrDefault(projectId, commentId), commentId);
    }

    [HttpGet("finance-operation/{FinanceOperationId:int}")]
    [Authorize]
    public async Task<ActionResult> ToFinanceOperation(ProjectIdentification projectId, int financeOperationId)
    {
        // Первичный ключ FinanceOperation — это CommentId связанного комментария,
        // поэтому financeOperationId совпадает с id комментария операции.
        var commentId = new CommentId(financeOperationId);
        return RedirectToOwner(await ownerLoader.GetOwnerByCommentOrDefault(projectId, commentId), commentId);
    }

    private ActionResult RedirectToOwner(CommentDiscussionOwnerAccess? access, CommentId? commentId)
    {
        if (access is null)
        {
            return NotFound();
        }

        if (!access.HasAccess)
        {
            return NoAccesToProjectView(access.ProjectInfo, currentUserAccessor);
        }

        return CommentRedirectHelper.RedirectToDiscussion(Url, access.Owner, commentId);
    }
}
