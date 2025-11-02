namespace JoinRpg.Domain.Schedules;

/// <summary>
/// Builds schedule from program item data
/// </summary>
public class ScheduleBuilder(IReadOnlyCollection<Character> characters, ProjectInfo projectInfo)
{
    private readonly IReadOnlyCollection<Character> characters = characters.Where(ch => ch.IsActive).ToList();
    private readonly ProjectInfo projectInfo = projectInfo;

    private ProjectFieldInfo TimeSlotField { get; } = projectInfo.TimeSlotField ?? throw new Exception("Schedule not enabled");

    private ProjectFieldInfo RoomField { get; } = projectInfo.RoomField ?? throw new Exception("Schedule not enabled");

    private HashSet<ProgramItem> NotScheduled { get; } = new HashSet<ProgramItem>();

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

        foreach (var character in characters.Where(ch => ch.CharacterType != PrimitiveTypes.CharacterType.Slot))
        {
            var programItem = new ProgramItem(character);
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

    private List<ProgramItemSlot> SelectSlots(ProgramItem programItem, Character character)
    {
        var fields = character.GetFieldsDict(projectInfo);

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
