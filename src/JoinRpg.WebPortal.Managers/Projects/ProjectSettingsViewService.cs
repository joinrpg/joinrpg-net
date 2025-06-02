using JoinRpg.Data.Interfaces;
using JoinRpg.PrimitiveTypes.ProjectMetadata;
using JoinRpg.Services.Interfaces.Projects;
using JoinRpg.Web.ProjectMasterTools.Settings;

namespace JoinRpg.WebPortal.Managers.Projects;
internal class ProjectSettingsViewService(IProjectMetadataRepository projectMetadataRepository, IProjectService projectService) : IProjectSettingsClient
{
    async Task<ProjectPublishSettingsViewModel> IProjectSettingsClient.GetPublishSettings(ProjectIdentification projectId)
    {
        var project = await projectMetadataRepository.GetProjectMetadata(projectId);
        return new ProjectPublishSettingsViewModel()
        {
            CloneSettings = (ProjectCloneSettingsView)project.CloneSettings,
            ProjectId = projectId,
            ProjectName = project.ProjectName,
            ProjectStatus = project.ProjectStatus,
            PublishEnabled = project.PublishPlot,
        };
    }
    async Task IProjectSettingsClient.SavePublishSettings(ProjectPublishSettingsViewModel model)
    {
        await projectService.SetPublishSettings(model.ProjectId, (ProjectCloneSettings)model.CloneSettings, model.PublishEnabled);
    }
}
