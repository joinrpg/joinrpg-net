using JoinRpg.Helpers;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace JoinRpg.Domain.Schedules;

public class ProgramItem(Character character)
{
    public int Id { get; } = character.CharacterId;
    public string Name { get; } = character.CharacterName;
    public MarkdownString Description { get; } = character.Description;
    public User[] Authors { get; } = new[] { character.ApprovedClaim?.Player }.WhereNotNull().ToArray();
    public int ProjectId { get; } = character.ProjectId;

    public bool ShowAuthors { get; } = !character.HidePlayerForCharacter;
}

public class ProgramItemPlaced
{
    public ProgramItemPlaced(ProgramItem programItem, List<ScheduleBuilder.ProgramItemSlot> slots)
    : this(programItem, slots.Select(x => x.Room).Distinct().ToList(), slots.Select(x => x.TimeSlot).ToList())
    {
    }

    private ProgramItemPlaced(ProgramItem item,
        IReadOnlyCollection<ScheduleRoom> rooms,
        IReadOnlyCollection<TimeSlot> timeSlots)
    {
        ProgramItem = item;
        Rooms = rooms;
        StartTime = timeSlots.Min(x => x.Options.StartTime);
        EndTime = timeSlots.Max(x => x.Options.EndTime);
    }

    public DateTimeOffset EndTime { get; set; }

    public DateTimeOffset StartTime { get; set; }

    public IReadOnlyCollection<ScheduleRoom> Rooms { get; }
    public ProgramItem ProgramItem { get; set; }
}

public record class ScheduleItemAttribute
{
    public MarkdownString Description { get; }
    public string Name { get; }
    public ProjectFieldVariantIdentification Id { get; }
    public int SeqId { get; }

    public ScheduleItemAttribute(ProjectFieldVariant variant, int seqId)
    {
        Id = variant.Id;
        Name = variant.Label;
        Description = variant.Description;
        SeqId = seqId;
    }
}

public record class ScheduleRoom : ScheduleItemAttribute
{
    public ScheduleRoom(ProjectFieldVariant variant, int seqId) : base(variant, seqId)
    {
    }
}

public record class TimeSlot : ScheduleItemAttribute
{
    public TimeSlot(TimeSlotFieldVariant variant, int seqId) : base(variant, seqId)
    {
        Options = variant.TimeSlotOptions;
    }

    public TimeSlotOptions Options { get; }
}

public record ScheduleResult(
    IReadOnlyList<ProgramItem> NotScheduled,
    IReadOnlyList<ScheduleRoom> Rooms,

    IReadOnlyList<TimeSlot> TimeSlots,

    IReadOnlyList<ProgramItem> Conflicted,

    List<List<ProgramItem?>> Slots,

    List<ProgramItemPlaced> AllItems,
    ProjectScheduleSettings ProjectScheduleSettings)
{ }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
