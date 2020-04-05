using JoinRpg.DataModel;
using JoinRpg.Domain;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers.Comments
{
    internal static class CommentRedirectHelper
    {
        public static ActionResult RedirectToDiscussion(IUrlHelper Url, CommentDiscussion discussion, int? commentId = null)
        {
            string extra = commentId != null ? $"#comment{commentId}" : null;
            var claim = discussion.GetClaim();
            if (claim != null)
            {
                var actionLink = Url.Action("Edit", "Claim", new { claim.ClaimId, discussion.ProjectId });
                return new RedirectResult(actionLink + extra);
            }
            var forumThread = discussion.GetForumThread();
            if (forumThread != null)
            {
                var actionLink = Url.Action("ViewThread", new { discussion.ProjectId, forumThread.ForumThreadId });
                return new RedirectResult(actionLink + extra);
            }
            return new NotFoundResult();
        }
    }
}
