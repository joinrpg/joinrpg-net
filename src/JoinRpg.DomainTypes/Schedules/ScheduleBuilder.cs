using JoinRpg.Common.PrimitiveTypes.Users;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.ProjectMetadata;

namespace JoinRpg.DomainTypes.Schedules;

/// <summary>
/// Builds schedule from program item data
/// </summary>
public class ScheduleBuilder
{
    private readonly IReadOnlyCollection<CharacterInfo> characters;
    private readonly ProjectInfo projectInfo;
    private readonly IReadOnlyDictionary<UserIdentification, UserInfoHeader> authorUsers;

    /// <param name="characters">
    /// Персонажи, из которых строится сетка. Отбор — на стороне вызывающего: удалённых сюда
    /// передавать не надо, отдельно они не отфильтровываются.
    /// </param>
    /// <param name="authorUsers">
    /// Пользователи, указанные полем-ведущим (#4512), разрезолвленные пачкой на всю сетку —
    /// см. <see cref="ProgramItem.CollectAuthorUserIds"/>. Пустой словарь, если поля в проекте нет.
    /// </param>
    public ScheduleBuilder(
        IReadOnlyCollection<CharacterInfo> characters,
        ProjectInfo projectInfo,
        IReadOnlyDictionary<UserIdentification, UserInfoHeader> authorUsers)
    {
        ArgumentNullException.ThrowIfNull(characters);
        ArgumentNullException.ThrowIfNull(projectInfo);
        ArgumentNullException.ThrowIfNull(authorUsers);

        foreach (var character in characters)
        {
            // Тот же инвариант, что и в CharacterInfo (ADR013): агрегат привязан к конкретному
            // экземпляру ProjectInfo, и сравнивать варианты полей по ссылке можно только внутри него.
            if (!ReferenceEquals(character.ProjectInfo, projectInfo))
            {
                throw new ArgumentException(
                    $"Character {character.Id} is bound to another ProjectInfo instance", nameof(characters));
            }
        }

        this.characters = characters;
        this.projectInfo = projectInfo;
        this.authorUsers = authorUsers;

        TimeSlotField = projectInfo.TimeSlotField ?? throw new Exception("Schedule not enabled");
        RoomField = projectInfo.RoomField ?? throw new Exception("Schedule not enabled");
    }

    private ProjectFieldInfo TimeSlotField { get; }

    private ProjectFieldInfo RoomField { get; }

    private HashSet<ProgramItem> NotScheduled { get; } = [];

    public class ProgramItemSlot(TimeSlot slot, ScheduleRoom room)
    {
        public ScheduleRoom Room { get; } = room;
        public TimeSlot TimeSlot { get; } = slot;
        public ProgramItem? ProgramItem { get; set; } = null;
    }

    private List<List<ProgramItemSlot>> Slots { get; set; } = null!; // Initialized in start of Build()
    private List<ScheduleRoom> Rooms { get; set; } = null!; // Initialized in start of Build()

    private List<TimeSlot> TimeSlots { get; set; } = null!; // Initialized in start of Build()

    private HashSet<ProgramItem> Conflicted { get; } = new HashSet<ProgramItem>();

    public ScheduleResult Build()
    {
        Rooms = InitializeScheduleRoomList(RoomField.SortedVariants).ToList();
        TimeSlots = InitializeTimeSlotList(TimeSlotField.SortedVariants.Cast<TimeSlotFieldVariant>()).ToList();
        Slots = InitializeSlots(TimeSlots, Rooms);

        var allItems = new List<ProgramItemPlaced>();

        foreach (var character in characters.Where(ch => ch.CharacterType != CharacterType.Slot))
        {
            var programItem = new ProgramItem(character, authorUsers);
            var slots = SelectSlots(programItem, character);
            PutItem(programItem, slots);
            if (slots.Any())
            {
                allItems.Add(new ProgramItemPlaced(programItem, slots));
            }
        }
        return new ScheduleResult(
            NotScheduled.ToList(),
            Rooms,
            TimeSlots,
            Conflicted.ToList(),
            Slots.Select(row => row.Select(r => r.ProgramItem).ToList()).ToList(),
            allItems,
            projectInfo.ProjectScheduleSettings
            );
    }

    private void PutItem(ProgramItem programItem, List<ProgramItemSlot> slots)
    {
        if (!slots.Any())
        {
            _ = NotScheduled.Add(programItem);
            return;
        }

        var conflicts = (from slot in slots
                         where slot.ProgramItem != null
                         select slot.ProgramItem).Distinct().ToList();

        if (conflicts.Any())
        {
            _ = Conflicted.Add(programItem);
            foreach (var conflict in conflicts)
            {
                _ = Conflicted.Add(conflict);
            }
        }
        else
        {
            foreach (var slot in slots)
            {
                slot.ProgramItem = programItem;
            }
        }
    }

    private List<ProgramItemSlot> SelectSlots(ProgramItem programItem, CharacterInfo character)
    {
        var fields = character.GetAllFields().ToDictionary(f => f.Field.Id);

        int[] GetSlotIndexes(FieldWithValue field, IEnumerable<ScheduleItemAttribute> items)
        {
            ProjectFieldVariantIdentification[] variantIds = [.. field.GetDropdownValues().Select(variant => variant.Id)];

            int[] indexes = [.. (from item in items where variantIds.Contains(item.Id) select item.SeqId)];
            if (indexes.Length < variantIds.Length) // Some variants not found, probably deleted
            {
                _ = NotScheduled.Add(programItem);
            }

            return indexes;
        }

        var slots = from timeSeqId in GetSlotIndexes(fields[TimeSlotField.Id], TimeSlots)
                    from roomSeqId in GetSlotIndexes(fields[RoomField.Id], Rooms)
                    select Slots[timeSeqId][roomSeqId];

        return slots.ToList();
    }

    private static List<List<ProgramItemSlot>> InitializeSlots(List<TimeSlot> timeSlots, List<ScheduleRoom> rooms)
        => timeSlots.Select(time => rooms.Select(room => new ProgramItemSlot(time, room)).ToList()).ToList();

    private static IEnumerable<ScheduleRoom> InitializeScheduleRoomList(IReadOnlyList<ProjectFieldVariant> readOnlyList)
    {
        var seqId = 0;
        foreach (var variant in readOnlyList.Where(x => x.IsActive))
        {
            var item = new ScheduleRoom(variant, seqId);
            yield return item;
            seqId++;
        }
    }

    private static IEnumerable<TimeSlot> InitializeTimeSlotList(IEnumerable<TimeSlotFieldVariant> readOnlyList)
    {
        var seqId = 0;
        foreach (var variant in readOnlyList.Where(x => x.IsActive))
        {
            var item = new TimeSlot(variant, seqId);
            yield return item;
            seqId++;
        }
    }
}
