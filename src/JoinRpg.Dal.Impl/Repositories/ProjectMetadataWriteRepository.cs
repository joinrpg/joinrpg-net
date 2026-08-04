namespace JoinRpg.Dal.Impl.Repositories;

internal class ProjectMetadataWriteRepository(MyDbContext ctx) : IProjectMetadataWriteRepository
{
    public async Task<IProjectMetadataUpdateHandle> LoadProjectForUpdate(ProjectIdentification projectId)
    {
        var project = await ProjectLoaderCommon.GetProjectWithFieldsAsync(ctx, projectId.Value, skipCache: false)
            ?? throw new InvalidOperationException($"Project with {projectId} not found");

        return new ProjectMetadataUpdateHandle(ctx, project, projectId);
    }

    private sealed class ProjectMetadataUpdateHandle(MyDbContext ctx, Project project, ProjectIdentification projectId)
        : IProjectMetadataUpdateHandle
    {
        public Project Project { get; private set; } = project;

        public ProjectInfo ProjectInfo { get; private set; }
            = ProjectMetadataRepository.CreateInfoFromProject(project, projectId);

        public async Task<ProjectInfo> Refresh()
        {
            Project = await ProjectLoaderCommon.GetProjectWithFieldsAsync(ctx, projectId.Value, skipCache: true)
                ?? throw new InvalidOperationException($"Project with {projectId} not found");
            return ProjectInfo = ProjectMetadataRepository.CreateInfoFromProject(Project, projectId);
        }

        public void Remove(object entity) => ctx.Set(entity.GetType()).Remove(entity);
    }
}
