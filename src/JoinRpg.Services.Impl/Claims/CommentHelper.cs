using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.PrimitiveTypes.Claims;
using JoinRpg.PrimitiveTypes.Notifications;

namespace JoinRpg.Services.Impl.Claims;

internal class CommentHelper(ICurrentUserAccessor currentUserAccessor)
{
    public Comment CreateCommentForClaim(
      Claim claim,
      DateTime createdAt,
      string commentText,
      ClaimOperationType claimOperationType,
      ProjectInfo projectInfo, CommentExtraAction? extraAction = null)
    {
        var isVisibleToPlayer = claimOperationType != ClaimOperationType.MasterSecretChange;

        if (claimOperationType == ClaimOperationType.MasterSecretChange || claimOperationType == ClaimOperationType.MasterVisibleChange)
        {
            projectInfo.RequestMasterAccess(currentUserAccessor);
        }

        var comment = CreateCommentForDiscussion(claim.CommentDiscussion,
          createdAt,
          commentText,
          isVisibleToPlayer,
          extraAction);

        SetClaimTimes(claim, createdAt, isVisibleToPlayer);

        return comment;
    }

    public Comment CreateCommentForForumThread(
      ForumThread forumThread,
      DateTime createdAt,
      string commentText,
      bool isVisibleToPlayer)
    {
        var comment = CreateCommentForDiscussion(forumThread.CommentDiscussion,
          createdAt,
          commentText,
          isVisibleToPlayer,
          extraAction: null);

        return comment;
    }

    private void SetClaimTimes(Claim claim, DateTime createdAt, bool isVisibleToPlayer)
    {
        claim.LastUpdateDateTime = createdAt;
        if (claim.Player.UserId == currentUserAccessor.UserId)
        {
            claim.LastPlayerCommentAt = createdAt;
        }
        else
        {
            claim.LastMasterCommentAt = createdAt;
            claim.LastMasterCommentBy_Id = currentUserAccessor.UserId;

            if (isVisibleToPlayer)
            {
                claim.LastVisibleMasterCommentAt = createdAt;
                claim.LastVisibleMasterCommentBy_Id = currentUserAccessor.UserId;
            }
        }
    }

    private Comment CreateCommentForDiscussion(CommentDiscussion commentDiscussion, DateTime createdAt, string commentText, bool isVisibleToPlayer, CommentExtraAction? extraAction)
    {
        ArgumentNullException.ThrowIfNull(commentDiscussion);

        ArgumentNullException.ThrowIfNull(commentText);

        var comment = new Comment
        {
            CommentId = -1,
            ProjectId = commentDiscussion.ProjectId,
            AuthorUserId = currentUserAccessor.UserId,
            CommentDiscussionId = commentDiscussion.CommentDiscussionId,
            CommentText = new CommentText()
            {
                CommentId = -1,
                Text = new MarkdownString(commentText),
            },
            IsCommentByPlayer = !commentDiscussion.HasMasterAccess(currentUserAccessor.UserId),
            IsVisibleToPlayer = isVisibleToPlayer,
            ExtraAction = extraAction,
            CreatedAt = createdAt,
            LastEditTime = createdAt,
        };
        commentDiscussion.Comments.Add(comment);
        if (!isVisibleToPlayer)
        {
            _ = commentDiscussion.RequestMasterAccess(currentUserAccessor.UserId);
        }

        //TODO: check access for discussion for players (claims & forums)
        return comment;
    }

    //Если parentComment не равен нулю comment.SetParentCommentAndCheck(parentComment, claimOperationType);
    internal (Comment, ClaimSimpleChangedNotification) AddClaimCommentWithNotification(
        string commentText,
        Claim claim,
        ProjectInfo projectInfo,
        CommentExtraAction? commentExtraAction,
        ClaimOperationType claimOperationType, DateTime now)
    {
        // Этот метод вызывается ДО сохранения всего в базу
        // А вот ClaimSimpleChangedNotification будет обрабатываться сервисом и доп. данные будут загружаться ПОСЛЕ сохранения

        var comment = CreateCommentForClaim(claim,
            now,
            commentText,
            claimOperationType,
            projectInfo,
            commentExtraAction);

        ClaimSimpleChangedNotification claimSimpleChangedNotification = CreateNotificationFromComment(claim, claimOperationType, comment);
        return (comment, claimSimpleChangedNotification);
    }

    public ClaimSimpleChangedNotification CreateNotificationFromComment(Claim claim, ClaimOperationType claimOperationType, Comment comment)
    {
        return new ClaimSimpleChangedNotification(
            claim.GetId(),
            Player: claim.Player.ToUserInfoHeader(),
            comment.ExtraAction,
            currentUserAccessor.ToUserInfoHeader(),
            new NotificationEventTemplate(comment.CommentText.Text.Contents ?? ""),
            claimOperationType
            );
    }
}
