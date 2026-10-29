using JoinRpg.DomainTypes.Comments;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers.Comments;

internal static class CommentRedirectHelper
{
    /// <summary>Переход на страницу владельца обсуждения — заявку или тему форума.</summary>
    /// <param name="commentId">Комментарий, к которому прокрутить страницу; <c>null</c> — к началу.</param>
    public static ActionResult RedirectToDiscussion(IUrlHelper Url, CommentDiscussionOwner owner, CommentId? commentId = null)
    {
        var extra = commentId is not null ? $"#comment{commentId.Value}" : null;
        var actionLink = owner switch
        {
            ClaimDiscussionOwner claimOwner
                => Url.Action("Edit", "Claim", new { claimOwner.ClaimId.ClaimId, ProjectId = claimOwner.ProjectId.Value }),
            ForumDiscussionOwner forumOwner
                => Url.Action("ViewThread", "Forum", new { ProjectId = forumOwner.ProjectId.Value, ForumThreadId = forumOwner.ThreadId.ThreadId }),
            _ => throw new InvalidOperationException($"Unknown comment discussion owner {owner}"),
        };
        return new RedirectResult(actionLink + extra);
    }
}
