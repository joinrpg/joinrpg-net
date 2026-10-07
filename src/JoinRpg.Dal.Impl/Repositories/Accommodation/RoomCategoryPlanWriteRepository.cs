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
/// Третий запрос — за трекаемыми группами жильцов — делается только по явной просьбе операции
/// (<see cref="RoomCategoryPlanTracking.WithGroups"/>), то есть у заселения и выселения.
/// </para>
/// </remarks>
internal class RoomCategoryPlanWriteRepository(MyDbContext ctx) : IRoomCategoryPlanWriteRepository
{
    // Ядро загрузки плана переиспользуется целиком: оно потому и принимает готовый ProjectInfo,
    // что конструктор агрегата требует ровно того же экземпляра, что и внутри плана (ADR018, §3).
    private readonly RoomCategoryPlanLoader loader = new(ctx);

    public async Task<IRoomCategoryPlanUpdateHandle> LoadPlanForUpdate(
        ProjectInfo projectInfo,
        RoomCategoryIdentification categoryId,
        RoomCategoryPlanTracking tracking)
    {
        var plan = await loader.LoadOneAsync(projectInfo, categoryId)
            ?? throw new JoinRpgEntityNotFoundException(categoryId.RoomCategoryId, "room category");

        return await LoadHandle(projectInfo, plan, tracking);
    }

    public async Task<IRoomCategoryPlanUpdateHandle> LoadPlanForRoomUpdate(
        ProjectInfo projectInfo,
        AccommodationRoomIdentification roomId,
        RoomCategoryPlanTracking tracking)
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

        return await LoadHandle(projectInfo, plan, tracking);
    }

    public async Task<IRoomCategoryPlanUpdateHandle> LoadPlanForGroupUpdate(
        ProjectInfo projectInfo,
        AccommodationRequestIdentification groupId)
    {
        // Как и с комнатой, отдельного запроса «какого типа эта группа» не нужно: план ищется
        // сразу по принадлежности группы категории. Фильтр по проекту внутри загрузчика тот же.
        var groupIntId = groupId.AccommodationRequestId;
        var plans = await loader.LoadAsync(
            projectInfo,
            category => category.Desirous.Any(group => group.Id == groupIntId));

        var plan = plans.SingleOrDefault()
            ?? throw new AccommodationGroupNotFoundException(groupId);

        // Назвать корень агрегата через группу может только операция, которая её и двигает,
        // поэтому трекаемые группы здесь нужны всегда.
        return await LoadHandle(projectInfo, plan, RoomCategoryPlanTracking.WithGroups);
    }

    private async Task<IRoomCategoryPlanUpdateHandle> LoadHandle(
        ProjectInfo projectInfo,
        RoomCategoryPlan plan,
        RoomCategoryPlanTracking tracking)
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

        // Группы — отдельным запросом и только по запросу операции. Вторым Include их не взять:
        // рядом с комнатами это дало бы декартово произведение, а управлению комнатами группы
        // не нужны вовсе (ADR018, §10).
        // Заявки группы (Subjects) подтягиваем сразу: письма о заселении и выселении собираются
        // по ним (ADR018, §11), а без Include ленивая загрузка EF6 давала бы запрос на каждую
        // группу — при выселении целого типа это десятки лишних запросов.
        // Жильцы комнат пула берутся тем же запросом, даже если тип у группы чужой (такие строки
        // остались от времён до ADR018, дефект 5): тогда загрузка покрывает Inhabitants каждой
        // комнаты целиком, и коллекцию можно объявить загруженной — см. ниже.
        var groups = tracking == RoomCategoryPlanTracking.WithGroups
            ? (await ctx.Set<AccommodationRequest>()
                .Include(group => group.Subjects)
                .Where(group => group.ProjectId == projectIntId
                    && (group.AccommodationTypeId == categoryIntId
                        || group.Accommodation!.AccommodationTypeId == categoryIntId))
                .ToListAsync())
                .ToDictionary(group => new AccommodationRequestIdentification(projectId, group.Id))
            : null;

        if (groups is not null)
        {
            // Заселение и выселение ведут обратную навигацию room.Inhabitants сами (дефект 6), а
            // первое обращение к ней — ленивая загрузка жильцов комнаты (#5070). Все жильцы уже
            // в трекере и разложены по комнатам relationship fixup'ом, поэтому повторно за ними
            // не ходим.
            foreach (var room in category.ProjectAccommodations)
            {
                ctx.Entry(room).Collection(r => r.Inhabitants).IsLoaded = true;
                room.Inhabitants ??= [];
            }
        }

        return new Handle(
            ctx,
            projectInfo,
            plan,
            category,
            category.ProjectAccommodations.ToDictionary(
                room => new AccommodationRoomIdentification(projectId, room.Id)),
            groups);
    }

    private sealed class Handle(
        MyDbContext ctx,
        ProjectInfo projectInfo,
        RoomCategoryPlan plan,
        ProjectAccommodationType category,
        IReadOnlyDictionary<AccommodationRoomIdentification, ProjectAccommodation> rooms,
        IReadOnlyDictionary<AccommodationRequestIdentification, AccommodationRequest>? groups)
        : IRoomCategoryPlanUpdateHandle
    {
        public ProjectInfo ProjectInfo { get; } = projectInfo;

        public RoomCategoryPlan Plan { get; } = plan;

        public ProjectAccommodationType Category { get; } = category;

        public IReadOnlyDictionary<AccommodationRoomIdentification, ProjectAccommodation> Rooms { get; } = rooms;

        public IReadOnlyDictionary<AccommodationRequestIdentification, AccommodationRequest> Groups
            => groups ?? throw new InvalidOperationException(
                "Операция не запрашивала трекаемые группы жильцов: нужен RoomCategoryPlanTracking.WithGroups");

        public void Add(object entity) => _ = ctx.Set(entity.GetType()).Add(entity);

        public void Remove(object entity) => _ = ctx.Set(entity.GetType()).Remove(entity);
    }
}
