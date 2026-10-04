namespace JoinRpg.DomainTypes.ProjectMetadata;

public record class TimeSlotFieldVariant : ProjectFieldVariant
{
    public TimeSlotOptions TimeSlotOptions { get; }

    /// <summary>
    /// Начало слота в часовом поясе проекта
    /// </summary>
    public DateTimeOffset StartTime { get; }

    /// <summary>
    /// Конец слота в часовом поясе проекта
    /// </summary>
    public DateTimeOffset EndTime { get; }

    public TimeSlotFieldVariant(ProjectFieldVariantIdentification Id,
    string Label,
    int Price,
    bool IsPlayerSelectable,
    bool IsActive,
    CharacterGroupIdentification? CharacterGroupId,
    MarkdownString? Description,
    MarkdownString? MasterDescription,
    string? ProgrammaticValue,
    bool wasEverUsed,
    string parentFieldName,
    TimeZoneInfo projectTimeZone)
        : base(Id, Label, Price, IsPlayerSelectable, IsActive, CharacterGroupId, Description, MasterDescription, ProgrammaticValue, wasEverUsed, parentFieldName)
    {
        TimeSlotOptions = (ProgrammaticValue is null ? null : TimeSlotOptions.FromJson(ProgrammaticValue))
            ?? TimeSlotOptions.CreateDefault(projectTimeZone);
        StartTime = TimeSlotOptions.GetStartTime(projectTimeZone);
        EndTime = TimeSlotOptions.GetEndTime(projectTimeZone);
    }
}
