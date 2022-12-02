using JoinRpg.DataModel;
using JoinRpg.Helpers;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace JoinRpg.Domain.Schedules;

public class ProgramItem
{
    public int Id { get; internal set; }
    public string Name { get; internal set; }
    public MarkdownString Description { get; internal set; }
    public User[] Authors { get; internal set; }
    public int ProjectId { get; set; }

    public ProgramItem(Character character)
    {
        Id = character.CharacterId;
        Name = character.CharacterName;
        Description = character.Description;
        Authors = new[] { character.ApprovedClaim?.Player }.WhereNotNull().ToArray();
        ProjectId = character.ProjectId;
    }
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
    public int Id { get; }
    public int SeqId { get; }

    public ScheduleItemAttribute(ProjectFieldDropdownValue variant, int seqId)
    {
        Id = variant.ProjectFieldDropdownValueId;
        Name = variant.Label;
        Description = variant.Description;
        SeqId = seqId;
    }
}

public record class ScheduleRoom : ScheduleItemAttribute
{
    public ScheduleRoom(ProjectFieldDropdownValue variant, int seqId) : base(variant, seqId)
    {
    }
}

public record class TimeSlot : ScheduleItemAttribute
{
    public TimeSlot(ProjectFieldDropdownValue variant, int seqId) : base(variant, seqId)
    {
        Options = variant.GetTimeSlotOptions();
    }

    public TimeSlotOptions Options { get; }
}

public record ScheduleResult(
    IReadOnlyList<ProgramItem> NotScheduled,
    IReadOnlyList<ScheduleRoom> Rooms,

    IReadOnlyList<TimeSlot> TimeSlots,

    IReadOnlyList<ProgramItem> Conflicted,

    List<List<ProgramItem?>> Slots,

    List<ProgramItemPlaced> AllItems)
{ }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
