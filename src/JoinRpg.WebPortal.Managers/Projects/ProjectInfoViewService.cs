using JoinRpg.Data.Interfaces;
using JoinRpg.Markdown;
using JoinRpg.Web.ProjectCommon;
using JoinRpg.Web.ProjectCommon.Projects;

namespace JoinRpg.WebPortal.Managers.Projects;
internal class ProjectInfoViewService(IProjectMetadataRepository projectMetadataRepository) : IProjectInfoClient
{
    async Task<ProjectInfoViewModel> IProjectInfoClient.GetProjectInfo(ProjectIdentification projectId)
    {
        var project = await projectMetadataRepository.GetProjectMetadata(projectId);
        return new ProjectInfoViewModel(
            ProjectId: project.ProjectId,
            Name: project.ProjectName,
            Masters: [.. project.Masters.Select(m => m.ToUserLinkViewModel())],
            DescriptionHtml: project.ProjectDescription.ToHtmlString(),
            KogdaIgraLinkedIds: [.. project.KogdaIgraLinkedIds]
            );
    }
}
