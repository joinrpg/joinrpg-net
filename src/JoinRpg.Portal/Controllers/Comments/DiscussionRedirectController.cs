using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Interfaces;
using JoinRpg.Portal.Controllers.Comments;
using JoinRpg.Portal.Controllers.Common;
using JoinRpg.PrimitiveTypes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers;

[Route("{ProjectId}/goto/")]
public class DiscussionRedirectController(
    IProjectMetadataRepository projectMetadataRepository,
    IForumRepository forumRepository,
    ICurrentUserAccessor currentUserAccessor
    ) : JoinControllerGameBase
{
    [HttpGet("discussion/{CommentDiscussionId}")]
    [Authorize]
    public async Task<ActionResult> ToDiscussion(ProjectIdentification projectId, int commentDiscussionId)
    {
        CommentDiscussion discussion = await forumRepository.GetDiscussion(projectId, commentDiscussionId);

        if (!discussion.HasAnyAccess(currentUserAccessor.UserId))
        {
            var projectInfo = await projectMetadataRepository.GetProjectMetadata(projectId);
            return NoAccesToProjectView(projectInfo, currentUserAccessor);
        }

        return CommentRedirectHelper.RedirectToDiscussion(Url, discussion, commentId: null);
    }

    [HttpGet("comment/{CommentId:int}")]
    [Authorize]
    public async Task<ActionResult> ToComment(ProjectIdentification projectId, int commentid)
    {
        CommentDiscussion discussion = await forumRepository.GetDiscussionByComment(projectId, commentid);

        if (!discussion.HasAnyAccess(currentUserAccessor.UserId))
        {
            var projectInfo = await projectMetadataRepository.GetProjectMetadata(projectId);
            return NoAccesToProjectView(projectInfo, currentUserAccessor);
        }

        return CommentRedirectHelper.RedirectToDiscussion(Url, discussion, commentid);
    }
}
