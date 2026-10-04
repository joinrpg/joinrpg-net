using JoinRpg.Common.PrimitiveTypes.Users;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Helpers;

namespace JoinRpg.DomainTypes.Schedules;

/// <summary>
/// Пункт программы — персонаж, размещаемый в сетке расписания.
/// </summary>
public class ProgramItem
{
    /// <param name="authorUsers">
    /// Пользователи, на которых ссылается поле-ведущий, разрезолвленные одной пачкой на всю
    /// сетку (ADR017 §4). Собрать их id — <see cref="CollectAuthorUserIds"/>.
    /// </param>
    public ProgramItem(CharacterInfo character, IReadOnlyDictionary<UserIdentification, UserInfoHeader> authorUsers)
    {
        Id = character.Id;
        Name = character.CharacterName;
        Description = character.Description;

        if (character.ProjectInfo.ScheduleAuthorField is { } authorField
            && GetAuthorFieldIds(character) is { Count: > 0 } authorIds)
        {
            // Ведущий указан явно (#4512). Настройка «скрыть игрока» тут не при чём: мастер
            // называет в поле третье лицо, и видимость решает само поле (ADR017 §6).
            // Ненайденный пользователь просто пропадает из списка — как и в показе user-полей.
            Authors = [.. authorIds
                .Select(id => authorUsers.TryGetValue(id, out var user) ? user : null)
                .WhereNotNull()];
            ShowAuthors = authorField.IsPublic;
        }
        else
        {
            // Поля нет или оно пустое — ведущим считается игрок утверждённой заявки
            Authors = [.. new[] { character.ApprovedClaim?.Player }.WhereNotNull()];
            ShowAuthors = !character.HidePlayerForCharacter;
        }
    }

    public CharacterIdentification Id { get; }
    public string Name { get; }
    public MarkdownString Description { get; }
    public UserInfoHeader[] Authors { get; }

    public bool ShowAuthors { get; }

    /// <summary>
    /// Идентификаторы всех пользователей, указанных полем-ведущим у переданных персонажей.
    /// Их надо разрезолвить одним запросом до построения сетки (ADR017 §4).
    /// </summary>
    public static IReadOnlyCollection<UserIdentification> CollectAuthorUserIds(IEnumerable<CharacterInfo> characters)
        => [.. characters.SelectMany(GetAuthorFieldIds).Distinct()];

    /// <summary>Значение поля-ведущего или пустой список, если поля в проекте нет.</summary>
    private static IReadOnlyList<UserIdentification> GetAuthorFieldIds(CharacterInfo character)
    {
        if (character.ProjectInfo.ScheduleAuthorField is not { } authorField)
        {
            return [];
        }

        return character.GetAllFields().SingleOrDefault(f => f.Field.Id == authorField.Id)?.UserIds ?? [];
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
        StartTime = timeSlots.Min(x => x.StartTime);
        EndTime = timeSlots.Max(x => x.EndTime);
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
        StartTime = variant.StartTime;
        EndTime = variant.EndTime;
    }

    public TimeSlotOptions Options { get; }

    /// <summary>
    /// Начало слота в часовом поясе проекта
    /// </summary>
    public DateTimeOffset StartTime { get; }

    /// <summary>
    /// Конец слота в часовом поясе проекта
    /// </summary>
    public DateTimeOffset EndTime { get; }
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
