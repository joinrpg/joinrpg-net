using JoinRpg.Data.Interfaces;
using JoinRpg.Web.ProjectCommon;

namespace JoinRpg.WebPortal.Managers;

internal class ProjectMasterViewService(IProjectMetadataRepository projectRepository) : IMasterClient
{
    public async Task<List<MasterViewModel>> GetMasters(int projectId)
    {
        var project = await projectRepository.GetProjectMetadata(new(projectId));
        return project.Masters
            .Select(acl => new MasterViewModel(acl.UserId, acl.Name))
            .OrderBy(a => a.DisplayName.DisplayName)
            .ToList();
    }
}
