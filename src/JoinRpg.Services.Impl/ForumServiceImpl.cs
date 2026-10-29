using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.Forums;
using JoinRpg.Services.Impl.Claims;
using JoinRpg.Services.Impl.Comments;

namespace JoinRpg.Services.Impl;

internal class ForumServiceImpl(IUnitOfWork unitOfWork,
                                ICurrentUserAccessor currentUserAccessor,
                                ForumNotificationService forumNotificationService,
                                IProjectMetadataRepository projectMetadataRepository,
                                CommentHelper commentHelper
    ) : DbServiceImplBase(unitOfWork, currentUserAccessor), IForumService, IForumCommentOperation
{
    public async Task<ForumThreadIdentification> CreateThread(CharacterGroupIdentification characterGroupId, string header, string commentText, bool hideFromUser, bool emailEverybody)
    {
        var group = await LoadProjectSubEntityAsync<CharacterGroup>(characterGroupId);

        // Раньше активность проверял только [ProjectShouldBeActive] на контроллере.
        var projectInfo = (await projectMetadataRepository.GetProjectMetadata(characterGroupId.ProjectId))
            .RequestMasterAccess(currentUserAccessor)
            .EnsureProjectActive();
        ClaimOperationType claimOperationType = GetOperationType(hideFromUser, projectInfo);

        var forumThread = new ForumThread()

        {
            CharacterGroupId = characterGroupId.CharacterGroupId,
            ProjectId = characterGroupId.ProjectId,
            Header = Required(header),
            CreatedAt = Now,
            ModifiedAt = Now,
            AuthorUserId = CurrentUserId,
            IsVisibleToPlayer = !hideFromUser,
            CommentDiscussion = new CommentDiscussion() { ProjectId = characterGroupId.ProjectId, Project = group.Project, },
        };

        var comment = commentHelper.CreateCommentForForumThread(forumThread,
            Now,
            commentText,
            projectInfo,
            claimOperationType);


        group.ForumThreads.Add(forumThread);
        await UnitOfWork.SaveChangesAsync();

        if (emailEverybody)
        {
            var email = new ForumMessageNotification(
                new ForumCommentIdentification(comment.ProjectId, forumThread.ForumThreadId, comment.CommentId),
                currentUserAccessor.ToUserInfoHeader(),
                new MarkdownDbValue(commentText),
                header,
                IsVisibleToPlayer: forumThread.IsVisibleToPlayer && comment.IsVisibleToPlayer
                );

            await forumNotificationService.SendNotification(email);
        }
        return forumThread.GetId();
    }

    /// <remarks>
    /// Снаружи вызывается только через <see cref="ICommentDiscussionService"/> (ADR026 §5): общие
    /// правила обсуждения — родитель из этого обсуждения, видимый ответ на скрытый, скрытие только
    /// мастером — проверяет он. Проверки родителя здесь не дубль: у не-мастера сервис их пропускает,
    /// чтобы они шли после проверки доступа. Раньше видимый ответ на скрытый здесь молча становился скрытым.
    /// </remarks>
    public async Task AddComment(ForumThreadIdentification forumThreadId, int? parentCommentId, bool isVisibleToPlayer, string commentText)
    {
        var (forumThread, projectInfo) = await GetForumThread(forumThreadId);
        _ = projectInfo.EnsureProjectActive();

        var parentComment = parentCommentId is int parentId
            ? forumThread.CommentDiscussion.Comments.SingleOrDefault(c => c.CommentId == parentId)
                ?? throw new ParentCommentNotFoundException(parentId)
            : null;

        if (isVisibleToPlayer && parentComment?.IsVisibleToPlayer == false)
        {
            throw new VisibleReplyToHiddenCommentException(parentComment.CommentId);
        }

        var comment = commentHelper.CreateCommentForForumThread(forumThread,
            Now,
            commentText,
            projectInfo,
            GetOperationType(!isVisibleToPlayer, projectInfo));

        comment.Parent = parentComment;

        await UnitOfWork.SaveChangesAsync();

        var email = new ForumMessageNotification(
                new ForumCommentIdentification(comment.ProjectId, forumThread.ForumThreadId, comment.CommentId),
                currentUserAccessor.ToUserInfoHeader(),
                new MarkdownDbValue(commentText),
                forumThread.Header,
                IsVisibleToPlayer: forumThread.IsVisibleToPlayer && comment.IsVisibleToPlayer,
                ParentCommentAuthor: parentComment?.Author.ToUserInfoHeader()
                );

        await forumNotificationService.SendNotification(email);

    }

    private ClaimOperationType GetOperationType(bool hideFromUser, ProjectInfo projectInfo)
    {
        ClaimOperationType claimOperationType;
        if (projectInfo.HasMasterAccess(currentUserAccessor.UserIdentification))
        {
            claimOperationType = hideFromUser ? ClaimOperationType.MasterSecretChange : ClaimOperationType.MasterVisibleChange;
        }
        else
        {
            claimOperationType = ClaimOperationType.PlayerChange;
        }

        return claimOperationType;
    }

    private async Task<(ForumThread, ProjectInfo)> GetForumThread(ForumThreadIdentification forumThreadId)
    {
        var forumThread = await ForumRepository.GetThread(forumThreadId);
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(forumThreadId.ProjectId);
        var isMaster = projectInfo.HasMasterAccess(currentUserAccessor);
        var isPlayer = forumThread.IsVisibleToPlayer &&
                       (await ClaimsRepository.GetClaimsForPlayer(forumThreadId.ProjectId, ClaimStatusSpec.Approved, CurrentUserId)).Any(
                         claim => claim.Character.IsPartOfGroup(forumThread.CharacterGroupId, projectInfo));

        if (!isMaster && !isPlayer)
        {
            throw new NoAccessToProjectException(projectInfo, CurrentUserId);
        }
        return (forumThread, projectInfo);
    }

}
