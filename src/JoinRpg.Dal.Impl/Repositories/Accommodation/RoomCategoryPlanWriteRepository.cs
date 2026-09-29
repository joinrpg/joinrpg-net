using JoinRpg.Data.Interfaces.Accommodation;
using JoinRpg.DomainTypes.Accommodation;
using JoinRpg.DomainTypes.Characters.Claims.Accommodation;

namespace JoinRpg.Dal.Impl.Repositories.Accommodation;

/// <summary>
/// Реализация <see cref="IRoomCategoryPlanWriteRepository"/> (ADR018, §10).
/// </summary>
/// <remarks>
/// Не регистрируется в DI (см. <c>Registraton</c>): создаётся только из <c>MyDbContext</c>,
/// чтобы трекинг и <c>SaveChanges</c> шли через один и тот же контекст.
/// </remarks>
internal class RoomCategoryPlanWriteRepository(MyDbContext ctx) : IRoomCategoryPlanWriteRepository
{
    // Ядро загрузки плана переиспользуется целиком: оно потому и принимает готовый ProjectInfo,
    // что на пути записи снимок метаданных собирается свой и обязан быть ровно тем же
    // экземпляром, что внутри плана (ADR018, §3).
    private readonly RoomCategoryPlanLoader loader = new(ctx);

    public async Task<IRoomCategoryPlanUpdateHandle> LoadPlanForUpdate(RoomCategoryIdentification categoryId)
    {
        var projectInfo = await LoadProjectInfo(categoryId.ProjectId);
        return await LoadHandle(projectInfo, categoryId);
    }

    public async Task<IRoomCategoryPlanUpdateHandle> LoadPlanForRoomUpdate(AccommodationRoomIdentification roomId)
    {
        var projectInfo = await LoadProjectInfo(roomId.ProjectId);

        // Лёгкий запрос «в какой категории эта комната»: пул в идентификаторе комнаты не
        // закодирован. Фильтр по проекту здесь обязателен — это он закрывает дефект 2 ADR018.
        var roomIntId = roomId.RoomId;
        var projectIntId = roomId.ProjectId.Value;
        var categoryIntId = await ctx.Set<ProjectAccommodation>()
            .AsNoTracking()
            .Where(room => room.Id == roomIntId && room.ProjectId == projectIntId)
            .Select(room => (int?)room.AccommodationTypeId)
            .FirstOrDefaultAsync()
            ?? throw new AccommodationRoomNotFoundException(roomId);

        return await LoadHandle(
            projectInfo,
            new RoomCategoryIdentification(roomId.ProjectId, categoryIntId));
    }

    private async Task<ProjectInfo> LoadProjectInfo(ProjectIdentification projectId)
    {
        var project = await ProjectLoaderCommon.GetProjectWithFieldsAsync(ctx, projectId.Value, skipCache: false)
            ?? throw new JoinRpgEntityNotFoundException(projectId.Value, "project");

        // Единственный источник истины Project -> ProjectInfo, тот же, что у read-пути.
        return ProjectMetadataRepository.CreateInfoFromProject(project, projectId);
    }

    private async Task<IRoomCategoryPlanUpdateHandle> LoadHandle(
        ProjectInfo projectInfo,
        RoomCategoryIdentification categoryId)
    {
        var plan = await loader.LoadOneAsync(projectInfo, categoryId)
            ?? throw new JoinRpgEntityNotFoundException(categoryId.RoomCategoryId, "room category");

        // См. замечание к RoomCategoryPlanLoader: сегодня ряд категории — это ряд типа проживания.
        var categoryIntId = categoryId.RoomCategoryId;
        var projectIntId = categoryId.ProjectId.Value;

        var category = await ctx.Set<ProjectAccommodationType>()
            .SingleOrDefaultAsync(type => type.Id == categoryIntId && type.ProjectId == projectIntId)
            ?? throw new JoinRpgEntityNotFoundException(categoryIntId, "room category");

        var rooms = await ctx.Set<ProjectAccommodation>()
            .Where(room => room.AccommodationTypeId == categoryIntId && room.ProjectId == projectIntId)
            .ToListAsync();

        // Группы нужны и нерасселённые: заселение (PR 5) двигает именно их.
        var groups = await ctx.Set<AccommodationRequest>()
            .Where(group => group.AccommodationTypeId == categoryIntId && group.ProjectId == projectIntId)
            .ToListAsync();

        var projectId = categoryId.ProjectId;
        return new Handle(
            ctx,
            projectInfo,
            plan,
            category,
            rooms.ToDictionary(room => new AccommodationRoomIdentification(projectId, room.Id)),
            groups.ToDictionary(group => new AccommodationRequestIdentification(projectId, group.Id)));
    }

    private sealed class Handle(
        MyDbContext ctx,
        ProjectInfo projectInfo,
        RoomCategoryPlan plan,
        ProjectAccommodationType category,
        IReadOnlyDictionary<AccommodationRoomIdentification, ProjectAccommodation> rooms,
        IReadOnlyDictionary<AccommodationRequestIdentification, AccommodationRequest> groups)
        : IRoomCategoryPlanUpdateHandle
    {
        public ProjectInfo ProjectInfo { get; } = projectInfo;

        public RoomCategoryPlan Plan { get; } = plan;

        public ProjectAccommodationType Category { get; } = category;

        public IReadOnlyDictionary<AccommodationRoomIdentification, ProjectAccommodation> Rooms { get; } = rooms;

        public IReadOnlyDictionary<AccommodationRequestIdentification, AccommodationRequest> Groups { get; } = groups;

        public void Add(object entity) => _ = ctx.Set(entity.GetType()).Add(entity);

        public void Remove(object entity) => _ = ctx.Set(entity.GetType()).Remove(entity);
    }
}
