using JoinRpg.Common.PrimitiveTypes.Users;
using JoinRpg.Data.Interfaces;
using JoinRpg.Web.ProjectCommon;

namespace JoinRpg.WebPortal.Managers;

internal class ProjectMasterViewService(IProjectMetadataRepository projectRepository) : IMasterClient
{
    public async Task<List<UserInfoHeader>> GetMasters(int projectId)
    {
        var project = await projectRepository.GetProjectMetadata(new(projectId));
        return project.Masters
            .Select(acl => acl.UserInfo)
            .OrderBy(a => a.DisplayName.DisplayName)
            .ToList();
    }
}
