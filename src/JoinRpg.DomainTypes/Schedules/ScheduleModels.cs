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
        LastModifiedAt = GetLastModifiedAt(character);

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
    /// Последнее изменение пункта (UTC): правка самого персонажа или полей расписания — время
    /// и зал хранятся в вариантах полей, ведущий — в своём поле. Не учитывает смену часового
    /// пояса проекта и смену имени ведущего в его профиле.
    /// </summary>
    public DateTime LastModifiedAt { get; }

    /// <summary>Последнее изменение полей, из которых строится сетка (время, зал, ведущий).</summary>
    public static DateTime GetScheduleFieldsLastModifiedAt(ProjectInfo projectInfo)
        => new[] { projectInfo.TimeSlotField, projectInfo.RoomField, projectInfo.ScheduleAuthorField }
            .WhereNotNull()
            .Select(field => field.Marks.UpdatedAt)
            .DefaultIfEmpty(DateTime.MinValue)
            .Max();

    private static DateTime GetLastModifiedAt(CharacterInfo character)
    {
        var fieldsModifiedAt = GetScheduleFieldsLastModifiedAt(character.ProjectInfo);
        return character.UpdatedAt > fieldsModifiedAt ? character.UpdatedAt : fieldsModifiedAt;
    }

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

        return character.GetFieldById(authorField.Id)?.UserIds ?? [];
    }
}

public class ProgramItemPlaced
{
    public ProgramItemPlaced(ProgramItem programItem, List<ScheduleBuilder.ProgramItemSlot> slots, TimeZoneInfo projectTimeZone)
    : this(programItem, slots.Select(x => x.Room).Distinct().ToList(), slots.Select(x => x.TimeSlot).ToList(), projectTimeZone)
    {
    }

    private ProgramItemPlaced(ProgramItem item,
        IReadOnlyCollection<ScheduleRoom> rooms,
        IReadOnlyCollection<TimeSlot> timeSlots,
        TimeZoneInfo projectTimeZone)
    {
        ProgramItem = item;
        Rooms = rooms;
        StartTime = timeSlots.Min(x => x.StartTime);
        EndTime = timeSlots.Max(x => x.EndTime);
        ProjectTimeZone = projectTimeZone;
    }

    /// <summary>
    /// Часовой пояс проекта, в котором заданы <see cref="StartTime"/> и <see cref="EndTime"/>
    /// </summary>
    public TimeZoneInfo ProjectTimeZone { get; }

    public DateTimeOffset EndTime { get; set; }

    public DateTimeOffset StartTime { get; set; }

    public IReadOnlyCollection<ScheduleRoom> Rooms { get; }
    public ProgramItem ProgramItem { get; set; }
}

/// <summary>
/// Пункт программы, не размещённый в сетке целиком: нет помещения или слота (#5754),
/// либо выбран удалённый вариант.
/// </summary>
/// <param name="Rooms">Выбранные помещения, включая удалённые — по ним видно, почему пункт не в сетке.</param>
/// <param name="TimeSlots">Выбранные слоты, включая удалённые.</param>
public record ProgramItemNotScheduled(
    ProgramItem ProgramItem,
    IReadOnlyList<ProjectFieldVariant> Rooms,
    IReadOnlyList<ProjectFieldVariant> TimeSlots);

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
    IReadOnlyList<ProgramItemNotScheduled> NotScheduled,
    IReadOnlyList<ScheduleRoom> Rooms,

    IReadOnlyList<TimeSlot> TimeSlots,

    IReadOnlyList<ProgramItem> Conflicted,

    List<List<ProgramItem?>> Slots,

    List<ProgramItemPlaced> AllItems,
    ProjectScheduleSettings ProjectScheduleSettings,
    // Последнее изменение (UTC) полей расписания и всех переданных пунктов, включая не размещённые.
    // Удалённых персонажей сборщик не видит — их учитывает вызывающий.
    DateTime LastModifiedAt)
{ }
