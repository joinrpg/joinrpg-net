using JoinRpg.Data.Interfaces;
using JoinRpg.Helpers;
using JoinRpg.PrimitiveTypes;
using JoinRpg.PrimitiveTypes.ProjectMetadata;

namespace JoinRpg.WebPortal.Managers;
public class ProjectMetadataRepositoryCacheDecorator : IProjectMetadataRepository
{
    private readonly IProjectMetadataRepository repository;
    private readonly PerRequestCache<ProjectIdentification, ProjectInfo> cache;

    public ProjectMetadataRepositoryCacheDecorator(IProjectMetadataRepository repository, PerRequestCache<ProjectIdentification, ProjectInfo> cache)
    {
        this.repository = repository;
        this.cache = cache;
    }

    public async Task<ProjectInfo> GetProjectMetadata(ProjectIdentification projectId)
    {
        var projectInfo = cache.TryGet(projectId);
        if (projectInfo == null)
        {
            projectInfo = await repository.GetProjectMetadata(projectId);
            cache.Set(projectId, projectInfo);
        }
        return projectInfo;
    }
}
