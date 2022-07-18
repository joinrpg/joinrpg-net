using JoinRpg.Data.Interfaces;
using JoinRpg.Web.Models.CharacterGroups;
using JoinRpg.Web.ProjectCommon;

namespace JoinRpg.WebPortal.Managers;
internal class ProjectMasterViewService : IMasterClient
{
    private readonly IProjectRepository projectRepository;

    public ProjectMasterViewService(IProjectRepository projectRepository)
    {
        this.projectRepository = projectRepository;
    }

    public async Task<List<MasterViewModel>> GetMasters(int projectId)
    {
        var project = await projectRepository.GetProjectAsync(projectId);
        return project.GetMasterListViewModel().ToList();
    }
}
