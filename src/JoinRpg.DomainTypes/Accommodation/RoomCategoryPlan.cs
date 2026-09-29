using JoinRpg.DomainTypes.Characters.Claims.Accommodation;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.DomainTypes.ProjectMetadata.Accommodation;

namespace JoinRpg.DomainTypes.Accommodation;

/// <summary>
/// План поселения одной категории комнат: её комнаты, типы проживания, которые из них селятся,
/// и группы жильцов.
/// Привязан к конкретному экземпляру <see cref="ProjectInfo"/>, кешированию между запросами
/// не подлежит.
/// </summary>
/// <remarks>
/// Корень агрегата — пул комнат, а не тип проживания (ADR018). Сегодня пул ровно один на тип,
/// но модель с самого начала устроена как «комнаты плюс множество типов, которые из них селятся»,
/// чтобы будущее разделение типа и категории было расширением, а не переделкой.
/// </remarks>
public record class RoomCategoryPlan
{
    private readonly Dictionary<AccommodationRoomIdentification, RoomInfo> roomsById;
    private readonly Dictionary<AccommodationRequestIdentification, AccommodationGroupInfo> groupsById;
    private readonly Dictionary<AccommodationTypeIdentification, AccommodationTypeInfo> typesById;

    public RoomCategoryIdentification Id { get; }
    public ProjectInfo ProjectInfo { get; }

    /// <summary>Типы проживания, селящиеся из этого пула</summary>
    public IReadOnlyCollection<AccommodationTypeInfo> AccommodationTypes { get; }

    /// <summary>
    /// Физическая вместимость комнаты этого пула — комнаты внутри категории одинаковы.
    /// Не путать с <see cref="AccommodationTypeInfo.Capacity"/>: та говорит, скольких мы селим
    /// в комнату по данному типу проживания.
    /// </summary>
    public int RoomCapacity { get; }

    public IReadOnlyCollection<RoomInfo> Rooms { get; }

    /// <summary>Все группы этого пула, включая ещё не расселённые</summary>
    public IReadOnlyCollection<AccommodationGroupInfo> Groups { get; }

    public RoomCategoryPlan(
        RoomCategoryIdentification id,
        ProjectInfo projectInfo,
        IReadOnlyCollection<AccommodationTypeInfo> accommodationTypes,
        int roomCapacity,
        IReadOnlyCollection<RoomInfo> rooms,
        IReadOnlyCollection<AccommodationGroupInfo> groups)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(projectInfo);
        ArgumentNullException.ThrowIfNull(accommodationTypes);
        ArgumentNullException.ThrowIfNull(rooms);
        ArgumentNullException.ThrowIfNull(groups);

        if (id.ProjectId != projectInfo.ProjectId)
        {
            throw new ArgumentException(
                $"Room category {id} does not belong to project {projectInfo.ProjectId}", nameof(id));
        }

        // Инвариант ссылочного равенства ADR013: единственный источник правды о типе проживания —
        // метаданные проекта, второго экземпляра типа в системе быть не должно.
        foreach (var type in accommodationTypes)
        {
            var fromMetadata = projectInfo.AccommodationSettings.GetTypeByIdOrDefault(type.Id);
            if (!ReferenceEquals(fromMetadata, type))
            {
                throw new ArgumentException(
                    $"Accommodation type {type.Id} is not the very instance from ProjectInfo.AccommodationSettings",
                    nameof(accommodationTypes));
            }
        }

        foreach (var room in rooms)
        {
            if (room.Id.ProjectId != id.ProjectId)
            {
                throw new ArgumentException(
                    $"Room {room.Id} does not belong to project {id.ProjectId}", nameof(rooms));
            }
        }

        foreach (var group in groups)
        {
            if (group.Id.ProjectId != id.ProjectId)
            {
                throw new ArgumentException(
                    $"Group {group.Id} does not belong to project {id.ProjectId}", nameof(groups));
            }
        }

        typesById = accommodationTypes.ToDictionary(t => t.Id);
        roomsById = rooms.ToDictionary(r => r.Id);
        groupsById = groups.ToDictionary(g => g.Id);

        foreach (var group in groups)
        {
            if (!typesById.ContainsKey(group.AccommodationTypeId))
            {
                throw new ArgumentException(
                    $"Group {group.Id} is bought by accommodation type {group.AccommodationTypeId}, "
                        + $"which does not occupy room category {id}",
                    nameof(groups));
            }

            // Группа с указанной комнатой обязана лежать ровно в одной комнате плана,
            // нерасселённая — ни в одной.
            var roomsWithGroup = rooms.Count(r => r.Inhabitants.Any(i => i.Id == group.Id));
            var expected = group.RoomId is null ? 0 : 1;
            if (roomsWithGroup != expected)
            {
                throw new ArgumentException(
                    $"Group {group.Id} has RoomId={group.RoomId?.ToString() ?? "null"}, "
                        + $"but is listed as inhabitant of {roomsWithGroup} room(s)",
                    nameof(groups));
            }
        }

        foreach (var room in rooms)
        {
            foreach (var inhabitant in room.Inhabitants)
            {
                if (!groupsById.TryGetValue(inhabitant.Id, out var group) || group != inhabitant)
                {
                    throw new ArgumentException(
                        $"Inhabitant {inhabitant.Id} of room {room.Id} is not among plan groups",
                        nameof(rooms));
                }

                if (inhabitant.RoomId != room.Id)
                {
                    throw new ArgumentException(
                        $"Group {inhabitant.Id} lives in room {room.Id}, but its RoomId is "
                            + $"{inhabitant.RoomId?.ToString() ?? "null"}",
                        nameof(rooms));
                }
            }
        }

        Id = id;
        ProjectInfo = projectInfo;
        AccommodationTypes = accommodationTypes;
        RoomCapacity = roomCapacity;
        Rooms = rooms;
        Groups = groups;
    }

    /// <summary>Группы, которые ещё не расселены по комнатам</summary>
    public IEnumerable<AccommodationGroupInfo> UnassignedGroups
        => Groups.Where(g => g.RoomId is null);

    /// <summary>
    /// Сколько всего мест в пуле. Считается по комнатам пула, а не по типам проживания: после
    /// разделения типа и категории сумма по типам посчитала бы одни и те же комнаты дважды.
    /// </summary>
    public int TotalCapacity => Rooms.Count * RoomCapacity;

    /// <summary>Комната по идентификатору</summary>
    /// <exception cref="KeyNotFoundException">Комнаты с таким идентификатором нет в этом плане</exception>
    public RoomInfo GetRoom(AccommodationRoomIdentification roomId)
        => roomsById.TryGetValue(roomId, out var room)
            ? room
            : throw new KeyNotFoundException($"Не найдена комната с ID={roomId} в категории {Id}");

    /// <summary>Группа по идентификатору</summary>
    /// <exception cref="KeyNotFoundException">Группы с таким идентификатором нет в этом плане</exception>
    public AccommodationGroupInfo GetGroup(AccommodationRequestIdentification groupId)
        => groupsById.TryGetValue(groupId, out var group)
            ? group
            : throw new KeyNotFoundException($"Не найдена группа проживающих с ID={groupId} в категории {Id}");

    /// <summary>Тип проживания, селящийся из этого пула</summary>
    /// <exception cref="KeyNotFoundException">Тип проживания не селится из этого пула</exception>
    public AccommodationTypeInfo GetAccommodationType(AccommodationTypeIdentification typeId)
        => typesById.TryGetValue(typeId, out var type)
            ? type
            : throw new KeyNotFoundException($"Тип проживания с ID={typeId} не селится из категории {Id}");

    /// <summary>
    /// Сколько человек вмещает комната при нынешних жильцах: физический предел, ужатый
    /// вместимостями типов тех, кто уже въехал.
    /// </summary>
    /// <remarks>
    /// Тип, под которым группу поселили, ограничивает комнату целиком, а не одно место: «Люкс
    /// на одного», въехавший в двухместный «Люкс», делает комнату полной. См. ADR018,
    /// «Правило свободного места».
    /// </remarks>
    public int GetEffectiveCapacity(AccommodationRoomIdentification roomId)
    {
        var room = GetRoom(roomId);
        var capacity = RoomCapacity;
        foreach (var inhabitant in room.Inhabitants)
        {
            capacity = Math.Min(capacity, GetAccommodationType(inhabitant.AccommodationTypeId).Capacity);
        }
        return capacity;
    }

    /// <summary>Комната заполнена — никого больше не поселить</summary>
    public bool IsFull(AccommodationRoomIdentification roomId)
        => GetRoom(roomId).Occupancy >= GetEffectiveCapacity(roomId);

    /// <summary>
    /// Сколько человек ещё можно поселить в комнату под данным типом проживания.
    /// Правило — ADR018, «Правило свободного места».
    /// </summary>
    /// <remarks>
    /// Никогда не отрицательно: комната могла оказаться переполненной, если вместимость типа
    /// уменьшили уже после заселения.
    /// </remarks>
    public int GetFreeSpace(AccommodationRoomIdentification roomId, AccommodationTypeIdentification typeId)
    {
        var room = GetRoom(roomId);
        var type = GetAccommodationType(typeId);
        var limit = Math.Min(GetEffectiveCapacity(roomId), type.Capacity);
        return Math.Max(0, limit - room.Occupancy);
    }
}
