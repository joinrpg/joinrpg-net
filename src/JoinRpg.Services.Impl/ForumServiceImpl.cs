using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.PrimitiveTypes.Claims;
using JoinRpg.PrimitiveTypes.Forums;
using JoinRpg.Services.Impl.Claims;

namespace JoinRpg.Services.Impl;

internal class ForumServiceImpl(IUnitOfWork unitOfWork,
                                ICurrentUserAccessor currentUserAccessor,
                                ForumNotificationService forumNotificationService,
                                IProjectMetadataRepository projectMetadataRepository,
                                CommentHelper commentHelper
    ) : DbServiceImplBase(unitOfWork, currentUserAccessor), IForumService
{
    public async Task<ForumThreadIdentification> CreateThread(CharacterGroupIdentification characterGroupId, string header, string commentText, bool hideFromUser, bool emailEverybody)
    {
        var group = await LoadProjectSubEntityAsync<CharacterGroup>(characterGroupId);

        var projectInfo = (await projectMetadataRepository.GetProjectMetadata(characterGroupId.ProjectId)).RequestMasterAccess(currentUserAccessor);
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
                new MarkdownString(commentText),
                header
                );

            await forumNotificationService.SendNotification(email);
        }
        return forumThread.GetId();
    }

    public async Task AddComment(ForumThreadIdentification forumThreadId, int? parentCommentId, bool isVisibleToPlayer, string commentText)
    {
        var (forumThread, projectInfo) = await GetForumThread(forumThreadId);

        var parentComment = forumThread.CommentDiscussion.Comments.SingleOrDefault(c => c.CommentId == parentCommentId);

        var visibleToPlayerUpdated = isVisibleToPlayer && parentComment?.IsVisibleToPlayer != false;

        var comment = commentHelper.CreateCommentForForumThread(forumThread,
            Now,
            commentText,
            projectInfo,
            GetOperationType(!isVisibleToPlayer, projectInfo));

        comment.Parent = parentComment;

        var email = new ForumMessageNotification(
                new ForumCommentIdentification(comment.ProjectId, forumThread.ForumThreadId, comment.CommentId),
                currentUserAccessor.ToUserInfoHeader(),
                new MarkdownString(commentText),
                forumThread.Header,
                ParentCommentAuthor: parentComment?.Author.ToUserInfoHeader()
                );

        await UnitOfWork.SaveChangesAsync();

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
                         claim => claim.Character.IsPartOfGroup(forumThread.CharacterGroupId));

        if (!isMaster && !isPlayer)
        {
            throw new NoAccessToProjectException(projectInfo, CurrentUserId);
        }
        return (forumThread, projectInfo);
    }

}
