using JoinRpg.Data.Interfaces;
using JoinRpg.Domain;
using JoinRpg.Interfaces;
using JoinRpg.Services.Interfaces.ProjectAccess;
using JoinRpg.Web.ProjectCommon.Masters;
using JoinRpg.Web.ProjectMasterTools.Acl;

namespace JoinRpg.WebPortal.Managers.ProjectMasterTools;

internal class MasterAccessViewService(
    IProjectMetadataRepository projectMetadataRepository,
    IProjectAccessService projectAccessService,
    ICurrentUserAccessor currentUserAccessor) : IMasterAccessClient
{
    public async Task<AddMasterViewModel> GetAddMaster(ProjectIdentification projectId, UserIdentification userId)
    {
        var projectInfo = await GetProjectWithGrantRights(projectId);
        return new AddMasterViewModel
        {
            ProjectId = projectId,
            UserId = userId,
            Role = AddMasterViewModel.DefaultRole,
            IsPublic = true,
            Permissions = ToViewModels(projectInfo, userId),
        };
    }

    public Task AddMaster(AddMasterViewModel model)
        => projectAccessService.GrantAccess(new GrantAccessRequest
        {
            ProjectId = model.ProjectId,
            UserId = model.UserId,
            Permissions = ToPermissions(model.Permissions),
            Role = new MasterRoleTitle(model.Role),
            Description = model.Description.ToOptionalMarkdown(),
            IsPublic = model.IsPublic,
        });

    public async Task<MasterPermissionsViewModel> GetPermissions(ProjectIdentification projectId, UserIdentification userId)
    {
        var projectInfo = await GetProjectWithGrantRights(projectId);
        return new MasterPermissionsViewModel
        {
            ProjectId = projectId,
            UserId = userId,
            Permissions = ToViewModels(projectInfo, userId),
            IsFormerMaster = projectInfo.FormerMasters.Any(m => m.UserId == userId),
        };
    }

    public Task SavePermissions(MasterPermissionsViewModel model)
        => projectAccessService.ChangeAccess(new ChangeAccessRequest
        {
            ProjectId = model.ProjectId,
            UserId = model.UserId,
            Permissions = ToPermissions(model.Permissions),
        });

    private async Task<ProjectInfo> GetProjectWithGrantRights(ProjectIdentification projectId)
        => (await projectMetadataRepository.GetProjectMetadata(projectId))
            .RequestMasterAccess(currentUserAccessor, Permission.CanGrantRights);

    private static List<MasterPermissionValueViewModel> ToViewModels(ProjectInfo projectInfo, UserIdentification userId)
        => [.. projectInfo.GetPermissionViewModels(userId)
            .Select(badge => new MasterPermissionValueViewModel { Permission = badge.Permission, Value = badge.Value })];

    private static Permission[] ToPermissions(IEnumerable<MasterPermissionValueViewModel> permissions)
        => [.. permissions.Where(p => p.Value).Select(p => p.Permission)];
}
