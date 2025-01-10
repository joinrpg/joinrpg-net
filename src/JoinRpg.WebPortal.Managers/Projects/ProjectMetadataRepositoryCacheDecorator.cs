using JoinRpg.Data.Interfaces;
using JoinRpg.Helpers;
using JoinRpg.PrimitiveTypes;
using JoinRpg.PrimitiveTypes.ProjectMetadata;

namespace JoinRpg.WebPortal.Managers.Projects;
public class ProjectMetadataRepositoryCacheDecorator(
    IProjectMetadataRepository repository,
    PerRequestCache<ProjectIdentification, ProjectInfo> projectMetadataCache) : IProjectMetadataRepository
{
    public async Task<ProjectInfo> GetProjectMetadata(ProjectIdentification projectId)
    {
        var projectInfo = projectMetadataCache.TryGet(projectId);
        if (projectInfo == null)
        {
            projectInfo = await repository.GetProjectMetadata(projectId);
            projectMetadataCache.Set(projectId, projectInfo);
        }
        return projectInfo;
    }

    Task<ProjectMastersListInfo> IProjectMetadataRepository.GetMastersList(ProjectIdentification projectId) => repository.GetMastersList(projectId);
}
