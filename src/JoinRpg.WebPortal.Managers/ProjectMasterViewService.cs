using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Web.ProjectCommon;

namespace JoinRpg.WebPortal.Managers;
internal class ProjectMasterViewService(IProjectRepository projectRepository) : IMasterClient
{
    public async Task<List<MasterViewModel>> GetMasters(int projectId)
    {
        var project = await projectRepository.GetProjectAsync(projectId);
        return GetMasterListViewModel(project);
    }

    private static List<MasterViewModel> GetMasterListViewModel(Project project)
    {
        return project.ProjectAcls
            .Select(acl => new MasterViewModel(acl.User.UserId, acl.User.ExtractDisplayName()))
            .OrderBy(a => a.DisplayName.DisplayName)
            .ToList();
    }
}
