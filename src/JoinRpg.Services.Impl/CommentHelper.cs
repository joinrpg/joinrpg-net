using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.PrimitiveTypes.Claims;

namespace JoinRpg.Services.Impl;

internal static class CommentHelper
{
    public static Comment CreateCommentForClaim(
      Claim claim,
      int currentUserId,
      DateTime createdAt,
      string commentText,
      bool isVisibleToPlayer,
      CommentExtraAction? extraAction = null)
    {
        var comment = CreateCommentForDiscussion(claim.CommentDiscussion,
          currentUserId,
          createdAt,
          commentText,
          isVisibleToPlayer,
          extraAction);

        SetClaimTimes(claim, currentUserId, createdAt, isVisibleToPlayer);

        return comment;
    }

    private static void SetClaimTimes(Claim claim, int currentUserId, DateTime createdAt, bool isVisibleToPlayer)
    {
        claim.LastUpdateDateTime = createdAt;
        if (claim.Player.UserId == currentUserId)
        {
            claim.LastPlayerCommentAt = createdAt;
        }
        else
        {
            claim.LastMasterCommentAt = createdAt;
            claim.LastMasterCommentBy_Id = currentUserId;

            if (isVisibleToPlayer)
            {
                claim.LastVisibleMasterCommentAt = createdAt;
                claim.LastVisibleMasterCommentBy_Id = currentUserId;
            }
        }
    }

    public static Comment CreateCommentForDiscussion(
        CommentDiscussion commentDiscussion,
        int currentUserId,
        DateTime createdAt,
        string commentText,
        bool isVisibleToPlayer,
        Comment? parentComment,
        CommentExtraAction? extraAction = null) => CreateCommentForDiscussion(commentDiscussion, currentUserId, createdAt, commentText, isVisibleToPlayer, extraAction).SetParent(parentComment);

    [Obsolete("Use SetParentCommentAndCheck")]
    public static Comment SetParent(this Comment comment, Comment? parentComment)
    {
        if (parentComment is not null)
        {
            comment.Parent = parentComment;
        }
        return comment;
    }

    public static Comment CreateCommentForDiscussion(CommentDiscussion commentDiscussion, int currentUserId, DateTime createdAt, string commentText, bool isVisibleToPlayer, CommentExtraAction? extraAction)
    {
        ArgumentNullException.ThrowIfNull(commentDiscussion);

        ArgumentNullException.ThrowIfNull(commentText);

        var comment = new Comment
        {
            CommentId = -1,
            ProjectId = commentDiscussion.ProjectId,
            AuthorUserId = currentUserId,
            CommentDiscussionId = commentDiscussion.CommentDiscussionId,
            CommentText = new CommentText()
            {
                CommentId = -1,
                Text = new MarkdownString(commentText),
            },
            IsCommentByPlayer = !commentDiscussion.HasMasterAccess(currentUserId),
            IsVisibleToPlayer = isVisibleToPlayer,
            ExtraAction = extraAction,
            CreatedAt = createdAt,
            LastEditTime = createdAt,
        };
        commentDiscussion.Comments.Add(comment);
        if (!isVisibleToPlayer)
        {
            _ = commentDiscussion.RequestMasterAccess(currentUserId);
        }

        //TODO: check access for discussion for players (claims & forums)
        return comment;
    }
}
