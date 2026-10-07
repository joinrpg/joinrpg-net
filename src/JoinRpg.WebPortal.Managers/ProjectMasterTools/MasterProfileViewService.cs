using JoinRpg.Data.Interfaces;
using JoinRpg.Domain;
using JoinRpg.Interfaces;
using JoinRpg.Services.Interfaces.ProjectAccess;
using JoinRpg.Web.ProjectMasterTools.Acl;

namespace JoinRpg.WebPortal.Managers.ProjectMasterTools;

internal class MasterProfileViewService(
    IProjectMetadataRepository projectMetadataRepository,
    IProjectMasterProfileRepository masterProfileRepository,
    IProjectAccessService projectAccessService,
    ICurrentUserAccessor currentUserAccessor) : IMasterProfileClient
{
    public async Task<MasterProfileViewModel> GetProfile(ProjectIdentification projectId, UserIdentification userId)
    {
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(projectId);
        // Свой профиль мастер видит и правит сам, чужой — с правом выдавать доступ (ADR019, §7).
        _ = projectInfo.RequestMasterAccess(
            currentUserAccessor,
            userId == currentUserAccessor.UserIdentificationOrDefault ? Permission.None : Permission.CanGrantRights);

        var master = projectInfo.GetMasterById(userId);
        var profile = (await masterProfileRepository.GetMasterProfiles(projectId)).Single(p => p.UserId == userId);
        return new MasterProfileViewModel
        {
            ProjectId = projectId,
            UserId = userId,
            Role = profile.Role.Value,
            Description = profile.Description?.Value,
            IsPublic = master.IsPublic,
        };
    }

    public Task SaveProfile(MasterProfileViewModel model)
        => projectAccessService.ChangeMasterProfile(new ChangeMasterProfileRequest
        {
            ProjectId = model.ProjectId,
            UserId = model.UserId,
            Role = new MasterRoleTitle(model.Role),
            Description = MarkdownString.FromOptional(model.Description),
            IsPublic = model.IsPublic,
        });
}
