namespace JoinRpg.Dal.Impl.Repositories;

internal class ProjectMetadataWriteRepository(MyDbContext ctx) : IProjectMetadataWriteRepository
{
    public async Task<IProjectMetadataUpdateHandle> LoadProjectForUpdate(ProjectIdentification projectId)
    {
        var project = await ProjectLoaderCommon.GetProjectWithFieldsAsync(ctx, projectId.Value, skipCache: false)
            ?? throw new InvalidOperationException($"Project with {projectId} not found");

        return new ProjectMetadataUpdateHandle(ctx, project, projectId);
    }

    public Task<RestoredShellInsertResult> InsertRestoredCharacterGroupShell(RestoredCharacterGroupShell shell)
    {
        EnsureSameProject(shell.CharacterGroupId, shell.RootGroupId);

        // Поля — как у CharacterGroupService.AddCharacterGroup: обычная активная группа, родитель —
        // корневая (список родителей — строка id), ответственного и порядка детей нет; аудит — сейчас.
        const string insert = """
            INSERT INTO dbo.CharacterGroups (CharacterGroupId, ProjectId, CharacterGroupName, IsRoot, IsSpecial,
                IsPublic, IsActive, Description_Contents, ParentGroupsImpl_ListIds, ResponsibleMasterUserId,
                ChildCharactersOrdering, ChildGroupsOrdering, CreatedAt, CreatedById, UpdatedAt, UpdatedById)
            VALUES (@p0, @p1, @p3, 0, 0,
                0, 1, N'', CAST(@p2 AS nvarchar(20)), NULL,
                NULL, NULL, @p4, @p5, @p4, @p5);
            """;

        return RestoredShellInsert.Execute(
            ctx,
            "CharacterGroups",
            "CharacterGroupId",
            insert,
            shell.CharacterGroupId.CharacterGroupId,
            shell.CharacterGroupId.ProjectId.Value,
            shell.RootGroupId.CharacterGroupId,
            shell.Name,
            shell.CreatedAt,
            shell.CreatedBy.Value);
    }

    private static void EnsureSameProject(CharacterGroupIdentification groupId, CharacterGroupIdentification rootGroupId)
    {
        if (groupId.ProjectId != rootGroupId.ProjectId)
        {
            throw new ArgumentException($"Корневая группа {rootGroupId} не из проекта группы {groupId}", nameof(rootGroupId));
        }
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

        public void Add(object entity) => _ = ctx.Set(entity.GetType()).Add(entity);

        public void Remove(object entity) => ctx.Set(entity.GetType()).Remove(entity);
    }
}
