using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.PrimitiveTypes.Claims;
using JoinRpg.PrimitiveTypes.Notifications;

namespace JoinRpg.Services.Impl.Claims;

internal class CommentHelper(ICurrentUserAccessor currentUserAccessor)
{
    private record class CommentCommand(ClaimOperationType ClaimOperationType, string CommentText, CommentExtraAction? CommentExtraAction, DateTime CreatedAt, ProjectInfo ProjectInfo)
    {
        public bool FromPlayer => ClaimOperationType == ClaimOperationType.PlayerChange;

        public bool IsVisibleToPlayer => ClaimOperationType != ClaimOperationType.MasterSecretChange;

        public bool FromMaster => ClaimOperationType != ClaimOperationType.PlayerChange;
    }

    public Comment CreateCommentForForumThread(
      ForumThread forumThread,
      DateTime createdAt,
      string commentText,
      ProjectInfo projectInfo, ClaimOperationType claimOperationType)
    {
        var commentCommand = new CommentCommand(claimOperationType, commentText, CommentExtraAction: null, createdAt.Date, projectInfo);

        return CreateCommentForDiscussion(forumThread.CommentDiscussion, commentCommand);
    }

    public (Comment, ClaimSimpleChangedNotification) CreateClaimCommentWithNotification(
        string commentText,
        Claim claim,
        ProjectInfo projectInfo,
        CommentExtraAction? commentExtraAction,
        ClaimOperationType claimOperationType, DateTime now)
    {
        // Этот метод вызывается ДО сохранения комментария в базу
        // А вот ClaimSimpleChangedNotification будет обрабатываться сервисом и доп. данные будут загружаться ПОСЛЕ сохранения

        var commentCommand = new CommentCommand(claimOperationType, commentText, commentExtraAction, now, projectInfo);

        var comment = CreateCommentForDiscussion(claim.CommentDiscussion, commentCommand);

        SetClaimTimes(claim, commentCommand);

        var claimSimpleChangedNotification = new ClaimSimpleChangedNotification(
            claim.GetId(),
            comment.ExtraAction,
            currentUserAccessor.ToUserInfoHeader(),
            new NotificationEventTemplate(comment.CommentText.Text.Contents ?? ""),
            claimOperationType
            );
        return (comment, claimSimpleChangedNotification);
    }

    private void SetClaimTimes(Claim claim, CommentCommand commentCommand)
    {
        claim.LastUpdateDateTime = commentCommand.CreatedAt;
        if (commentCommand.FromPlayer)
        {
            claim.LastPlayerCommentAt = commentCommand.CreatedAt;
        }
        else
        {
            claim.LastMasterCommentAt = commentCommand.CreatedAt;
            claim.LastMasterCommentBy_Id = currentUserAccessor.UserId;

            if (commentCommand.IsVisibleToPlayer)
            {
                claim.LastVisibleMasterCommentAt = commentCommand.CreatedAt;
                claim.LastVisibleMasterCommentBy_Id = currentUserAccessor.UserId;
            }
        }
    }

    private Comment CreateCommentForDiscussion(CommentDiscussion commentDiscussion, CommentCommand commentCommand)
    {
        ArgumentNullException.ThrowIfNull(commentDiscussion);

        ArgumentNullException.ThrowIfNull(commentCommand);

        var comment = new Comment
        {
            CommentId = -1,
            ProjectId = commentDiscussion.ProjectId,
            AuthorUserId = currentUserAccessor.UserId,
            CommentDiscussionId = commentDiscussion.CommentDiscussionId,
            CommentText = new CommentText()
            {
                CommentId = -1,
                Text = new MarkdownString(commentCommand.CommentText),
            },
            IsCommentByPlayer = !commentCommand.ProjectInfo.HasMasterAccess(currentUserAccessor),
            IsVisibleToPlayer = commentCommand.IsVisibleToPlayer,
            ExtraAction = commentCommand.CommentExtraAction,
            CreatedAt = commentCommand.CreatedAt,
            LastEditTime = commentCommand.CreatedAt,
        };
        commentDiscussion.Comments.Add(comment);

        if (commentCommand.FromMaster)
        {
            commentCommand.ProjectInfo.RequestMasterAccess(currentUserAccessor);
        }

        //TODO: check access for discussion for players (claims & forums)
        return comment;
    }
}
