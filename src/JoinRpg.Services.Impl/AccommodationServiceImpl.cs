using JoinRpg.Data.Interfaces.Accommodation;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.DomainTypes.Accommodation;
using JoinRpg.Services.Impl.Accommodation;
using JoinRpg.Services.Impl.Projects;

namespace JoinRpg.Services.Impl;

/// <summary>
/// Сервис комнат и заселения. Целиком работает поверх агрегата плана поселения
/// (ADR018) через <see cref="IAccommodationPropsService"/>.
/// </summary>
// Класс internal, потому что принимает internal-сервис: публичный конструктор с internal-параметром
// компилятор не пропустит. Наружу сервис виден через IAccommodationService, как и ClaimServiceImpl.
internal class AccommodationServiceImpl(
    IUnitOfWork unitOfWork,
    ICurrentUserAccessor currentUserAccessor,
    IProjectMetadataRepository projectMetadataRepository,
    IAccommodationPropsService accommodationPropsService)
    : DbServiceImplBase(unitOfWork, currentUserAccessor), IAccommodationService
{
    public async Task OccupyRoom(
        AccommodationRoomIdentification roomId,
        IReadOnlyCollection<AccommodationRequestIdentification> groupIds)
    {
        await accommodationPropsService.ChangePlanForRoom(
            roomId,
            Permission.CanSetPlayersAccommodations,
            ProjectActiveRequirement.MustBeActive,
            // Заселение двигает группы, значит трекаемые группы пула нужны: третий запрос
            // (ADR018, §10).
            RoomCategoryPlanTracking.WithGroups,
            (RoomId: roomId, GroupIds: groupIds),
            ctx =>
            {
                var planRoom = ctx.Plan.GetRoom(ctx.Request.RoomId);
                var room = ctx.GetRoomForChange(ctx.Request.RoomId);

                // Свободное место считаем явно по снимку плана, а не по навигации EF: полагаться
                // на relationship fixup трекера — это и был дефект 6 ADR018. Пересчитываем его по
                // мере заселения: каждая группа и занимает места, и может ужать комнату
                // вместимостью своего типа («Правило свободного места»). Для первой группы эта
                // формула — в точности Plan.GetFreeSpace(room, type).
                var effectiveCapacity = ctx.Plan.GetEffectiveCapacity(ctx.Request.RoomId);
                var occupancy = planRoom.Occupancy;

                var moved = new List<AccommodationGroupInfo>();
                foreach (var groupId in ctx.Request.GroupIds)
                {
                    // Группа берётся из того же плана, что и комната, поэтому группу чужого пула
                    // в эту комнату не поселить (дефект 5 ADR018). Промах — доменное исключение,
                    // а не NRE (дефект 7).
                    var groupInfo = ctx.Plan.GetGroup(groupId);
                    var typeCapacity = ctx.Plan
                        .GetAccommodationType(groupInfo.AccommodationTypeId)
                        .Capacity;

                    var freeSpace = Math.Max(0, Math.Min(effectiveCapacity, typeCapacity) - occupancy);
                    if (freeSpace < groupInfo.SubjectsCount)
                    {
                        throw new JoinRpgInsufficientRoomSpaceException(ctx.Request.RoomId);
                    }

                    effectiveCapacity = Math.Min(effectiveCapacity, typeCapacity);
                    occupancy += groupInfo.SubjectsCount;

                    var group = ctx.GetGroupForChange(groupId);
                    group.AccommodationId = room.Id;
                    group.Accommodation = room;
                    // Обратную навигацию ведём сами, не надеясь на трекер (дефект 6).
                    room.Inhabitants.Add(group);
                    moved.Add(groupInfo);
                }

                if (moved.Count == 0)
                {
                    return;
                }

                // Уведомление уходит после успешного сохранения, один раз на операцию (ADR018, §11).
                // Те, кто уже жил в комнате, — по снимку плана, взятому строго ДО мутации. Из них
                // вычитаем вселяемых: просьба вселить того, кто уже в комнате, проверку места
                // проходит, и без вычитания игрок попал бы в текст дважды — и «вселился», и «уже
                // был».
                var movedIds = moved.Select(group => group.Id).ToHashSet();
                QueueRoomNotification(
                    ctx, ctx.Request.RoomId, planRoom.Name,
                    RoomOccupancyChangeKind.Occupied,
                    changed: moved,
                    remaining: [.. planRoom.Inhabitants.Where(i => !movedIds.Contains(i.Id))]);
            });
    }

    public async Task UnOccupyGroup(AccommodationRequestIdentification groupId)
    {
        await accommodationPropsService.ChangePlanForGroup(
            groupId,
            Permission.CanSetPlayersAccommodations,
            ProjectActiveRequirement.MustBeActive,
            groupId,
            ctx =>
            {
                var groupInfo = ctx.Plan.GetGroup(ctx.Request);
                if (groupInfo.RoomId is not AccommodationRoomIdentification roomId)
                {
                    // Группа и так не расселена — выселять нечего: выселение идемпотентно
                    // (ADR018, §1).
                    return;
                }

                EvictFromRoom(ctx, roomId, [groupInfo]);
            });
    }

    public async Task UnOccupyRoom(AccommodationRoomIdentification roomId)
    {
        await accommodationPropsService.ChangePlanForRoom(
            roomId,
            Permission.CanSetPlayersAccommodations,
            ProjectActiveRequirement.MustBeActive,
            RoomCategoryPlanTracking.WithGroups,
            roomId,
            ctx =>
            {
                var planRoom = ctx.Plan.GetRoom(ctx.Request);
                if (planRoom.Inhabitants.Count > 0)
                {
                    EvictFromRoom(ctx, ctx.Request, [.. planRoom.Inhabitants]);
                }
            });
    }

    public async Task UnOccupyRoomType(AccommodationTypeIdentification typeId)
    {
        // Категорию по типу проживания знают только метаданные — конвертации идентификаторов
        // в домене нет и заводить её нельзя (ADR018, §2).
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(typeId.ProjectId);
        var categoryId = projectInfo.AccommodationSettings.GetTypeById(typeId).RoomCategoryId;

        // Тип принадлежит одной категории, значит все его группы живут в комнатах одного пула:
        // одна загрузка плана, один проход, одно сохранение (ADR018, §1, дефект 3).
        await accommodationPropsService.ChangePlan(
            categoryId,
            Permission.CanSetPlayersAccommodations,
            ProjectActiveRequirement.MustBeActive,
            RoomCategoryPlanTracking.WithGroups,
            typeId,
            ctx => EvictPlacedGroups(ctx, group => group.AccommodationTypeId == ctx.Request));
    }

    public async Task UnOccupyAllRooms(ProjectIdentification projectId)
    {
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(projectId);

        // Цикл по категориям, по мутации и транзакции на каждую (ADR018, §1): единой транзакции
        // на проект нет намеренно — это стоило бы отдельного входа в props-сервис по проекту и
        // исключения из правила «операция не выходит за границу агрегата». Частичное выполнение
        // здесь не страшно: выселение идемпотентно, повторный запуск дочищает остаток.
        var categories = projectInfo.AccommodationSettings.Types
            .Select(type => type.RoomCategoryId)
            .Distinct()
            .ToList();

        foreach (var categoryId in categories)
        {
            await accommodationPropsService.ChangePlan(
                categoryId,
                Permission.CanSetPlayersAccommodations,
                ProjectActiveRequirement.MustBeActive,
                RoomCategoryPlanTracking.WithGroups,
                categoryId,
                ctx => EvictPlacedGroups(ctx, _ => true));
        }
    }

    /// <summary>
    /// Выселяет из пула все расселённые группы, подходящие под условие, — по одному проходу
    /// на комнату. Сохранение при этом одно на всю операцию: его делает props-сервис.
    /// </summary>
    private static void EvictPlacedGroups(
        RoomCategoryPlanMutationContext ctx,
        Func<AccommodationGroupInfo, bool> selector)
    {
        var byRoom = ctx.Plan.Groups
            .Where(group => group.RoomId is not null && selector(group))
            .GroupBy(group => group.RoomId!);

        foreach (var roomGroups in byRoom)
        {
            EvictFromRoom(ctx, roomGroups.Key, [.. roomGroups]);
        }
    }

    /// <summary>
    /// Выселяет перечисленные группы из одной комнаты и ставит письмо в очередь.
    /// </summary>
    private static void EvictFromRoom(
        RoomCategoryPlanMutationContext ctx,
        AccommodationRoomIdentification roomId,
        IReadOnlyCollection<AccommodationGroupInfo> groupInfos)
    {
        var room = ctx.GetRoomForChange(roomId);

        foreach (var groupInfo in groupInfos)
        {
            var group = ctx.GetGroupForChange(groupInfo.Id);
            group.AccommodationId = null;
            group.Accommodation = null;
            _ = room.Inhabitants.Remove(group);
        }

        // Кто остался в комнате — по снимку плана, а не по навигации EF-сущности.
        var evictedIds = groupInfos.Select(group => group.Id).ToHashSet();
        var planRoom = ctx.Plan.GetRoom(roomId);
        var staying = planRoom.Inhabitants
            .Where(inhabitant => !evictedIds.Contains(inhabitant.Id))
            .ToList();

        // Уведомления ставятся в очередь и уходят все разом после единственного сохранения операции
        // (ADR018, §11). Уведомление на комнату, а не на операцию, потому что оно несёт ровно одну
        // комнату, а массовое выселение трогает их несколько.
        QueueRoomNotification(
            ctx, roomId, planRoom.Name,
            RoomOccupancyChangeKind.Evicted,
            changed: groupInfos,
            remaining: staying);
    }

    /// <summary>
    /// Ставит в очередь уведомление об изменении состава жителей комнаты. Собирается по доменному
    /// снимку плана: имена игроков и получателей сервис уведомлений добирает сам по идентификаторам
    /// заявок, поэтому EF-навигация здесь больше не нужна.
    /// </summary>
    /// <param name="changed">Группы, которых операция сдвинула.</param>
    /// <param name="remaining">Группы, которые остались (или уже были) в комнате.</param>
    private static void QueueRoomNotification(
        RoomCategoryPlanMutationContext ctx,
        AccommodationRoomIdentification roomId,
        string roomName,
        RoomOccupancyChangeKind kind,
        IReadOnlyCollection<AccommodationGroupInfo> changed,
        IReadOnlyCollection<AccommodationGroupInfo> remaining)
    {
        var notification = new RoomOccupancyNotification(
            roomId,
            roomName,
            ctx.Plan.Id,
            // Инициатор — текущий пользователь запроса, он же есть в контексте мутации.
            ctx.CurrentUser.ToUserInfoHeader(),
            [.. changed.SelectMany(group => group.Subjects)],
            [.. remaining.SelectMany(group => group.Subjects)],
            kind);

        ctx.AddRoomNotification(notification);
    }

    public async Task<IReadOnlyCollection<AccommodationRoomIdentification>> AddRooms(
        RoomCategoryIdentification categoryId,
        IReadOnlyCollection<string> roomNames)
    {
        //TODO: Implement rooms names checking
        var created = await accommodationPropsService.ChangePlan(
            categoryId,
            Permission.CanManageAccommodation,
            ProjectActiveRequirement.MustBeActive,
            // Создание комнат групп жильцов не трогает — за трекаемыми группами не ходим.
            RoomCategoryPlanTracking.RoomsOnly,
            roomNames,
            ctx =>
            {
                var created = new List<ProjectAccommodation>();
                foreach (var name in ctx.Request)
                {
                    var room = new ProjectAccommodation
                    {
                        Name = name,
                        AccommodationTypeId = ctx.Category.Id,
                        ProjectId = ctx.Category.ProjectId,
                        ProjectAccommodationType = ctx.Category,
                        Inhabitants = [],
                    };
                    ctx.AddEntity(room);
                    created.Add(room);
                }
                return created;
            });

        // Id генерируются базой при SaveChanges — читаем уже после возврата из props-сервиса.
        return [.. created.Select(room => room.GetId())];
    }

    public Task RenameRoom(AccommodationRoomIdentification roomId, string name)
        => accommodationPropsService.ChangePlanForRoom(
            roomId,
            Permission.CanManageAccommodation,
            ProjectActiveRequirement.MustBeActive,
            RoomCategoryPlanTracking.RoomsOnly,
            (RoomId: roomId, Name: name),
            ctx => ctx.GetRoomForChange(ctx.Request.RoomId).Name = ServiceValidation.Required(ctx.Request.Name));

    public Task DeleteRoom(AccommodationRoomIdentification roomId)
        => accommodationPropsService.ChangePlanForRoom(
            roomId,
            Permission.CanManageAccommodation,
            ProjectActiveRequirement.MustBeActive,
            // Заселённость видна по доменному снимку плана — трекаемые группы удалению не нужны.
            RoomCategoryPlanTracking.RoomsOnly,
            roomId,
            ctx =>
            {
                // Заселённость смотрим по доменному снимку плана, а не по навигации EF-сущности.
                if (ctx.Plan.GetRoom(ctx.Request).IsOccupied)
                {
                    throw new RoomIsOccupiedException(ctx.Request);
                }

                ctx.RemoveEntity(ctx.GetRoomForChange(ctx.Request));
            });
}
