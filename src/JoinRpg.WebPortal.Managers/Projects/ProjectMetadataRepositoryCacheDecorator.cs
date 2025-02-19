using JoinRpg.Data.Interfaces;
using JoinRpg.Helpers;
using JoinRpg.PrimitiveTypes;
using JoinRpg.PrimitiveTypes.ProjectMetadata;

namespace JoinRpg.WebPortal.Managers.Projects;
public class ProjectMetadataRepositoryCacheDecorator(
    IProjectMetadataRepository repository,
    PerRequestCache<ProjectIdentification, ProjectInfo> projectMetadataCache) : IProjectMetadataRepository
{
    public async Task<ProjectInfo> GetProjectMetadata(ProjectIdentification projectId, bool skipCache)
    {
        if (!skipCache && projectMetadataCache.TryGet(projectId) is ProjectInfo value)
        {
            return value;
        }
        var projectInfo = await repository.GetProjectMetadata(projectId, skipCache);
        projectMetadataCache.Set(projectId, projectInfo);

        return projectInfo;
    }

    Task<ProjectMastersListInfo> IProjectMetadataRepository.GetMastersList(ProjectIdentification projectId) => repository.GetMastersList(projectId);
}
