using System.Text.Json;
using JoinRpg.DataModel;

namespace JoinRpg.PrimitiveTypes.ProjectMetadata;

public record class TimeSlotFieldVariant : ProjectFieldVariant
{
    public TimeSlotOptions TimeSlotOptions { get; }

    public TimeSlotFieldVariant(ProjectFieldVariantIdentification Id,
    string Label,
    int Price,
    bool IsPlayerSelectable,
    bool IsActive,
    CharacterGroupIdentification? CharacterGroupId,
    MarkdownString Description,
    MarkdownString MasterDescription,
    string? ProgrammaticValue)
        : base(Id, Label, Price, IsPlayerSelectable, IsActive, CharacterGroupId, Description, MasterDescription, ProgrammaticValue)
    {
        if (ProgrammaticValue is not null)
        {
            TimeSlotOptions = JsonSerializer.Deserialize<TimeSlotOptions>(ProgrammaticValue) ?? TimeSlotOptions.CreateDefault();
        }
        else
        {
            TimeSlotOptions = TimeSlotOptions.CreateDefault();
        }
    }
}
