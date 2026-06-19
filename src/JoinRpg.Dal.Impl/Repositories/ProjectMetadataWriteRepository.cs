namespace JoinRpg.Dal.Impl.Repositories;

internal class ProjectMetadataWriteRepository(MyDbContext ctx) : IProjectMetadataWriteRepository
{
    public async Task<IProjectMetadataUpdateHandle> LoadProjectForUpdate(ProjectIdentification projectId)
    {
        var project = await ProjectLoaderCommon.GetProjectWithFieldsAsync(ctx, projectId.Value, skipCache: false)
            ?? throw new InvalidOperationException($"Project with {projectId} not found");

        return new ProjectMetadataUpdateHandle(project, projectId);
    }

    private sealed class ProjectMetadataUpdateHandle(Project project, ProjectIdentification projectId)
        : IProjectMetadataUpdateHandle
    {
        public Project Project { get; } = project;

        public ProjectInfo ProjectInfo { get; private set; }
            = ProjectMetadataRepository.CreateInfoFromProject(project, projectId);

        public ProjectInfo Refresh()
            => ProjectInfo = ProjectMetadataRepository.CreateInfoFromProject(Project, projectId);
    }
}
