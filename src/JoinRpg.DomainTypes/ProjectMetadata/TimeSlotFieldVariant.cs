using System.Text.Json;

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
        // Вариант строится при загрузке метаданных проекта: одно битое значение не должно ронять весь проект
        TimeSlotOptions = TryParse(ProgrammaticValue) ?? TimeSlotOptions.CreateDefault(projectTimeZone);
        StartTime = TimeSlotOptions.GetStartTime(projectTimeZone);
        EndTime = TimeSlotOptions.GetEndTime(projectTimeZone);
    }

    private static TimeSlotOptions? TryParse(string? programmaticValue)
    {
        if (programmaticValue is null)
        {
            return null;
        }
        try
        {
            var options = TimeSlotOptions.FromJson(programmaticValue);
            return options?.IsValid == true ? options : null;
        }
        catch (Exception ex) when (ex is JsonException or FormatException or ArgumentOutOfRangeException)
        {
            return null;
        }
    }
}
