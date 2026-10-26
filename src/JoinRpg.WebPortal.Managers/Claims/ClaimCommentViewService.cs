using JoinRpg.Data.Interfaces;
using JoinRpg.Domain;
using JoinRpg.Interfaces;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Claims.Comments;

namespace JoinRpg.WebPortal.Managers.Claims;

internal class ClaimCommentViewService(
    IClaimService claimService,
    IProjectMetadataRepository projectMetadataRepository,
    ICurrentUserAccessor currentUserAccessor) : IClaimCommentClient
{
    public async Task AddComment(AddClaimCommentRequest request)
    {
        if (!request.IsVisibleToPlayer)
        {
            // Скрыть комментарий от игрока может только мастер — как в форме комментария (ForumController)
            var projectInfo = await projectMetadataRepository.GetProjectMetadata(request.ClaimId.ProjectId);
            _ = projectInfo.RequestMasterAccess(currentUserAccessor);
        }

        await claimService.AddComment(
            request.ClaimId,
            request.ParentCommentId?.CommentId,
            request.IsVisibleToPlayer,
            request.CommentText);
    }
}
