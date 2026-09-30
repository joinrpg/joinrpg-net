using JoinRpg.Data.Interfaces.Accommodation;
using JoinRpg.DomainTypes.Accommodation;
using JoinRpg.DomainTypes.Characters.Claims.Accommodation;

namespace JoinRpg.Dal.Impl.Repositories.Accommodation;

/// <summary>
/// Реализация <see cref="IRoomCategoryPlanWriteRepository"/> (ADR018, §10).
/// </summary>
/// <remarks>
/// <para>
/// Не регистрируется в DI (см. <c>Registraton</c>): создаётся только из <c>MyDbContext</c>,
/// чтобы трекинг и <c>SaveChanges</c> шли через один и тот же контекст.
/// </para>
/// <para>
/// На одну мутацию делает ровно два запроса: доменный снимок плана (проекция, без трекинга) и
/// трекаемый ряд категории вместе с её комнатами. Снимок метаданных сюда приходит готовым.
/// </para>
/// </remarks>
internal class RoomCategoryPlanWriteRepository(MyDbContext ctx) : IRoomCategoryPlanWriteRepository
{
    // Ядро загрузки плана переиспользуется целиком: оно потому и принимает готовый ProjectInfo,
    // что конструктор агрегата требует ровно того же экземпляра, что и внутри плана (ADR018, §3).
    private readonly RoomCategoryPlanLoader loader = new(ctx);

    public async Task<IRoomCategoryPlanUpdateHandle> LoadPlanForUpdate(
        ProjectInfo projectInfo,
        RoomCategoryIdentification categoryId)
    {
        var plan = await loader.LoadOneAsync(projectInfo, categoryId)
            ?? throw new JoinRpgEntityNotFoundException(categoryId.RoomCategoryId, "room category");

        return await LoadHandle(projectInfo, plan);
    }

    public async Task<IRoomCategoryPlanUpdateHandle> LoadPlanForRoomUpdate(
        ProjectInfo projectInfo,
        AccommodationRoomIdentification roomId)
    {
        // Отдельного запроса «в какой категории эта комната» не нужно: план ищется сразу по
        // принадлежности комнаты категории. Фильтр по проекту внутри загрузчика обязателен —
        // это он закрывает дефект 2 ADR018.
        var roomIntId = roomId.RoomId;
        var plans = await loader.LoadAsync(
            projectInfo,
            category => category.ProjectAccommodations.Any(room => room.Id == roomIntId));

        var plan = plans.SingleOrDefault()
            ?? throw new AccommodationRoomNotFoundException(roomId);

        return await LoadHandle(projectInfo, plan);
    }

    private async Task<IRoomCategoryPlanUpdateHandle> LoadHandle(ProjectInfo projectInfo, RoomCategoryPlan plan)
    {
        // См. замечание к RoomCategoryPlanLoader: сегодня ряд категории — это ряд типа проживания.
        var categoryId = plan.Id;
        var categoryIntId = categoryId.RoomCategoryId;
        var projectIntId = categoryId.ProjectId.Value;

        // Комнаты берутся тем же запросом, что и категория: они нужны трекаемыми, а связь
        // «категория — комнаты» одна, декартова произведения не будет.
        var category = await ctx.Set<ProjectAccommodationType>()
            .Include(type => type.ProjectAccommodations)
            .SingleOrDefaultAsync(type => type.Id == categoryIntId && type.ProjectId == projectIntId)
            ?? throw new JoinRpgEntityNotFoundException(categoryIntId, "room category");

        var projectId = categoryId.ProjectId;
        return new Handle(
            ctx,
            projectInfo,
            plan,
            category,
            category.ProjectAccommodations.ToDictionary(
                room => new AccommodationRoomIdentification(projectId, room.Id)));
    }

    private sealed class Handle(
        MyDbContext ctx,
        ProjectInfo projectInfo,
        RoomCategoryPlan plan,
        ProjectAccommodationType category,
        IReadOnlyDictionary<AccommodationRoomIdentification, ProjectAccommodation> rooms)
        : IRoomCategoryPlanUpdateHandle
    {
        public ProjectInfo ProjectInfo { get; } = projectInfo;

        public RoomCategoryPlan Plan { get; } = plan;

        public ProjectAccommodationType Category { get; } = category;

        public IReadOnlyDictionary<AccommodationRoomIdentification, ProjectAccommodation> Rooms { get; } = rooms;

        public void Add(object entity) => _ = ctx.Set(entity.GetType()).Add(entity);

        public void Remove(object entity) => _ = ctx.Set(entity.GetType()).Remove(entity);
    }
}
