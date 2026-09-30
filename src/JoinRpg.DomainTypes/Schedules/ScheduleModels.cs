using JoinRpg.Common.PrimitiveTypes.Users;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Helpers;

namespace JoinRpg.DomainTypes.Schedules;

/// <summary>
/// Пункт программы — персонаж, размещаемый в сетке расписания.
/// </summary>
public class ProgramItem(CharacterInfo character)
{
    public CharacterIdentification Id { get; } = character.Id;
    public string Name { get; } = character.CharacterName;
    public MarkdownString Description { get; } = character.Description;
    public UserInfoHeader[] Authors { get; } = new[] { character.ApprovedClaim?.Player }.WhereNotNull().ToArray();

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
    public MarkdownString? Description { get; }
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
