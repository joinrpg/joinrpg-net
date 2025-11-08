using JoinRpg.Data.Interfaces;
using JoinRpg.Markdown;
using JoinRpg.Web.Games.Projects;
using JoinRpg.Web.ProjectCommon;

namespace JoinRpg.WebPortal.Managers.Projects;
internal class ProjectInfoViewService(IProjectMetadataRepository projectMetadataRepository) : IProjectInfoClient
{
    async Task<ProjectInfoViewModel> IProjectInfoClient.GetProjectInfo(ProjectIdentification projectId)
    {
        var project = await projectMetadataRepository.GetProjectMetadata(projectId);
        var details = await projectMetadataRepository.GetProjectDetails(projectId);
        return new ProjectInfoViewModel(
            ProjectId: project.ProjectId,
            Name: project.ProjectName,
            Masters: [.. project.Masters.Select(m => m.ToUserLinkViewModel())],
            DescriptionHtml: details.ProjectDescription.ToHtmlString(),
            KogdaIgraLinkedIds: [.. details.KogdaIgraLinkedIds]
            );
    }
}
