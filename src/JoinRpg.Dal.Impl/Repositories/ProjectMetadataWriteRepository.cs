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

        public void Add(object entity) => ctx.Set(entity.GetType()).Add(entity);

        public IProjectAccommodationWriteAccess Accommodation { get; }
            = new ProjectAccommodationWriteAccess(ctx, projectId);
    }

    /// <summary>
    /// Запросы поселения на <c>DbContext</c> хэндла. Связи, нужные бизнес-логике
    /// (вместимость типа, жильцы, их заявки и игроки), грузятся явно, а не ленивой загрузкой.
    /// </summary>
    private sealed class ProjectAccommodationWriteAccess(MyDbContext ctx, ProjectIdentification projectId)
        : IProjectAccommodationWriteAccess
    {
        private int ProjectIntId => projectId.Value;

        public async Task<ProjectAccommodationType?> LoadRoomType(int roomTypeId)
        {
            var projectIntId = ProjectIntId;
            return await ctx.Set<ProjectAccommodationType>()
                .Include(t => t.ProjectAccommodations)
                .Include(t => t.ProjectAccommodations.Select(r => r.Inhabitants))
                .Include(t => t.Desirous)
                .SingleOrDefaultAsync(t => t.Id == roomTypeId && t.ProjectId == projectIntId);
        }

        public async Task<ProjectAccommodation?> LoadRoom(int roomId)
        {
            var projectIntId = ProjectIntId;
            return await RoomQuery()
                .SingleOrDefaultAsync(r => r.Id == roomId && r.ProjectId == projectIntId);
        }

        public async Task<IReadOnlyCollection<ProjectAccommodation>> LoadOccupiedRooms(int? roomTypeId)
        {
            var projectIntId = ProjectIntId;
            var query = RoomQuery()
                .Where(r => r.ProjectId == projectIntId)
                .Where(r => r.Inhabitants.Any());

            if (roomTypeId is int typeId)
            {
                query = query.Where(r => r.AccommodationTypeId == typeId);
            }

            return await query.ToListAsync();
        }

        public async Task<IReadOnlyCollection<AccommodationRequest>> LoadAccommodationRequests(
            IReadOnlyCollection<int> requestIds)
        {
            var projectIntId = ProjectIntId;
            return await ctx.Set<AccommodationRequest>()
                .Include(r => r.Subjects.Select(s => s.Player))
                .Include(r => r.Project)
                .Where(r => r.ProjectId == projectIntId && requestIds.Contains(r.Id))
                .ToListAsync();
        }

        private IQueryable<ProjectAccommodation> RoomQuery()
            => ctx.Set<ProjectAccommodation>()
                .Include(r => r.Project)
                .Include(r => r.ProjectAccommodationType)
                .Include(r => r.Inhabitants)
                .Include(r => r.Inhabitants.Select(i => i.Subjects.Select(c => c.Player)));
    }
}
