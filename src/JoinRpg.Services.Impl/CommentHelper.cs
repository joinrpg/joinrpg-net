using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.PrimitiveTypes.Claims;
using JoinRpg.PrimitiveTypes.Notifications;
using JoinRpg.Services.Interfaces.Notification;

namespace JoinRpg.Services.Impl;

internal class CommentHelper(ICurrentUserAccessor currentUserAccessor)
{
    [Obsolete("Use AddCommentWithNotification")]
    public static Comment CreateCommentForClaim(
      Claim claim,
      int currentUserId,
      DateTime createdAt,
      string commentText,
      bool isVisibleToPlayer,
      CommentExtraAction? extraAction = null) => CreateCommentForClaimInternal(claim, currentUserId, createdAt, commentText, isVisibleToPlayer, extraAction);

    private static Comment CreateCommentForClaimInternal(
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
        CommentExtraAction? extraAction = null) => SetParent(CreateCommentForDiscussion(commentDiscussion, currentUserId, createdAt, commentText, isVisibleToPlayer, extraAction), parentComment);

    [Obsolete("Use SetParentCommentAndCheck")]
    public static Comment SetParent(Comment comment, Comment? parentComment)
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

        if (claimOperationType == ClaimOperationType.MasterSecretChange || claimOperationType == ClaimOperationType.MasterVisibleChange)
        {
            projectInfo.RequestMasterAccess(currentUserAccessor);

        }

        var comment = CreateCommentForClaimInternal(claim,
            currentUserAccessor.UserId,
            now,
            commentText,
            isVisibleToPlayer: claimOperationType != ClaimOperationType.MasterSecretChange,
            commentExtraAction);

        return (comment, new ClaimSimpleChangedNotification(
            claim.GetId(),
            Player: claim.Player.ToUserInfoHeader(),
            commentExtraAction,
            currentUserAccessor.ToUserInfoHeader(),
            new NotificationEventTemplate(commentText),
            claimOperationType
            ));
    }
}
