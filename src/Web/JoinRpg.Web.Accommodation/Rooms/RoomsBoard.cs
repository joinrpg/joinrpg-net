namespace JoinRpg.Web.Accommodation.Rooms;

/// <summary>
/// Комната на клиенте: в отличие от <see cref="AccommodationRoomViewModel"/>, её состав меняется
/// сразу, ещё до ответа сервера (оптимистичное обновление).
/// </summary>
public sealed class BoardRoom(AccommodationRoomIdentification roomId, string name, IEnumerable<AccommodationGroupViewModel> groups)
{
    public AccommodationRoomIdentification RoomId { get; } = roomId;

    public string Name { get; set; } = name;

    public List<AccommodationGroupViewModel> Groups { get; } = [.. groups];

    public int Occupancy => Groups.Sum(g => g.Persons);

    public bool IsOccupied => Groups.Count > 0;
}

/// <summary>
/// Состояние контрола расселения на клиенте: комнаты с жильцами и нерасселённые группы.
/// </summary>
/// <remarks>
/// Свободное место считается по тому же правилу, что и на сервере
/// (<c>RoomCategoryPlan.GetFreeSpace</c>, ADR018, «Правило свободного места»): тип, под которым
/// группа въехала, ограничивает всю комнату. Сервер всё равно проверяет место сам — здесь
/// правило нужно, чтобы не предлагать заведомо невлезающее.
/// </remarks>
public sealed class RoomsBoard
{
    public RoomsBoard(RoomTypeRoomsViewModel model)
    {
        TypeCapacity = model.TypeCapacity;
        RoomCapacity = model.RoomCapacity;
        Rooms = [.. model.Rooms.Select(r => new BoardRoom(r.RoomId, r.Name, r.Groups))];
        Unassigned = [.. model.UnassignedGroups];
    }

    private RoomsBoard(RoomsBoard other)
    {
        TypeCapacity = other.TypeCapacity;
        RoomCapacity = other.RoomCapacity;
        Rooms = [.. other.Rooms.Select(r => new BoardRoom(r.RoomId, r.Name, r.Groups))];
        Unassigned = [.. other.Unassigned];
    }

    /// <summary>Сколько человек селится в комнату по типу проживания страницы</summary>
    public int TypeCapacity { get; }

    /// <summary>Физическая вместимость комнаты пула</summary>
    public int RoomCapacity { get; }

    public List<BoardRoom> Rooms { get; private set; }

    public List<AccommodationGroupViewModel> Unassigned { get; private set; }

    /// <summary>Копия для отката оптимистичного обновления</summary>
    public RoomsBoard Snapshot() => new(this);

    /// <summary>Вернуть состояние из снимка</summary>
    public void Restore(RoomsBoard snapshot)
    {
        var copy = snapshot.Snapshot();
        Rooms = copy.Rooms;
        Unassigned = copy.Unassigned;
    }

    public BoardRoom GetRoom(AccommodationRoomIdentification roomId) => Rooms.Single(r => r.RoomId == roomId);

    /// <summary>
    /// Сколько человек вмещает комната при нынешних (и уже выбранных) жильцах: физический предел,
    /// ужатый вместимостями типов тех, кто въехал.
    /// </summary>
    public int GetEffectiveCapacity(BoardRoom room, IEnumerable<AccommodationGroupViewModel>? selected = null)
        => room.Groups.Concat(selected ?? [])
            .Select(g => g.TypeCapacity)
            .Aggregate(RoomCapacity, Math.Min);

    /// <summary>Сколько ещё человек влезет в комнату под типом с вместимостью <paramref name="typeCapacity"/></summary>
    public int GetFreeSpace(BoardRoom room, int typeCapacity, IReadOnlyCollection<AccommodationGroupViewModel>? selected = null)
    {
        var occupancy = room.Occupancy + (selected?.Sum(g => g.Persons) ?? 0);
        return Math.Max(0, Math.Min(GetEffectiveCapacity(room, selected), typeCapacity) - occupancy);
    }

    /// <summary>Сколько ещё человек влезет в комнату под типом проживания страницы</summary>
    public int GetFreeSpace(BoardRoom room, IReadOnlyCollection<AccommodationGroupViewModel>? selected = null)
        => GetFreeSpace(room, TypeCapacity, selected);

    /// <summary>Влезет ли группа в комнату вместе с уже выбранными</summary>
    public bool Fits(BoardRoom room, AccommodationGroupViewModel group, IReadOnlyCollection<AccommodationGroupViewModel>? selected = null)
        => group.Persons <= GetFreeSpace(room, group.TypeCapacity, selected);

    /// <summary>Заселить нерасселённые группы в комнату</summary>
    public void MoveToRoom(BoardRoom room, IEnumerable<AccommodationGroupViewModel> groups)
    {
        foreach (var group in groups)
        {
            _ = Unassigned.Remove(group);
            room.Groups.Add(group);
        }
    }

    /// <summary>Выселить группу из комнаты: она уходит в конец списка нерасселённых</summary>
    public void Kick(BoardRoom room, AccommodationGroupViewModel group)
    {
        if (room.Groups.Remove(group))
        {
            Unassigned.Add(group);
        }
    }

    /// <summary>Выселить всех из комнаты</summary>
    public void KickAll(BoardRoom room)
    {
        foreach (var group in room.Groups.ToList())
        {
            Kick(room, group);
        }
    }

    /// <summary>
    /// Выселить из всех комнат тех, кто живёт по типу проживания <paramref name="typeId"/>. Соседи
    /// по другим типам той же категории остаются (ADR020).
    /// </summary>
    public void KickAllOfType(AccommodationTypeIdentification typeId)
    {
        foreach (var room in Rooms)
        {
            foreach (var group in room.Groups.Where(g => g.TypeId == typeId).ToList())
            {
                Kick(room, group);
            }
        }
    }

    /// <summary>Сколько групп типа проживания <paramref name="typeId"/> расселено</summary>
    public int AssignedGroupsOfTypeCount(AccommodationTypeIdentification typeId)
        => Rooms.Sum(r => r.Groups.Count(g => g.TypeId == typeId));

    /// <summary>
    /// «Заселить всех»: жадно, комната за комнатой в порядке списка, каждую — нерасселёнными
    /// группами в порядке списка, пока влезают. Возвращает, кого в какую комнату поселили.
    /// </summary>
    public IReadOnlyList<(BoardRoom Room, IReadOnlyList<AccommodationGroupViewModel> Groups)> PlaceAll()
    {
        var result = new List<(BoardRoom, IReadOnlyList<AccommodationGroupViewModel>)>();
        foreach (var room in Rooms)
        {
            var placed = new List<AccommodationGroupViewModel>();
            foreach (var group in Unassigned.ToList())
            {
                if (Fits(room, group))
                {
                    MoveToRoom(room, [group]);
                    placed.Add(group);
                }
            }

            if (placed.Count > 0)
            {
                result.Add((room, placed));
            }
        }

        return result;
    }

    /// <summary>Вернуть группы из комнаты в нерасселённые на их прежние места (откат неудачного заселения)</summary>
    public void RollbackMove(BoardRoom room, IReadOnlyList<AccommodationGroupViewModel> groups, RoomsBoard snapshot)
    {
        foreach (var group in groups)
        {
            _ = room.Groups.Remove(group);
        }

        // Порядок нерасселённых восстанавливаем по снимку: вернувшиеся группы встают на свои места,
        // а не в конец списка.
        var returned = groups.ToHashSet();
        Unassigned = [.. snapshot.Unassigned.Where(g => returned.Contains(g) || Unassigned.Contains(g))];
    }
}
