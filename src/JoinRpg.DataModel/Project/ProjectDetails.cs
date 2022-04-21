using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;

namespace JoinRpg.DataModel;

public class ProjectDetails : IValidatableObject
{
    [Key] public int ProjectId { get; set; }
    public MarkdownString ClaimApplyRules { get; set; } = new MarkdownString();
    public MarkdownString ProjectAnnounce { get; set; } = new MarkdownString();

    public bool EnableManyCharacters { get; set; }
    public bool PublishPlot { get; set; }

    public bool FinanceWarnOnOverPayment { get; set; } = true;
    public bool PreferentialFeeEnabled { get; set; } = false;
    public MarkdownString PreferentialFeeConditions { get; set; } = new MarkdownString();

    public bool EnableCheckInModule { get; set; } = false;
    public bool CheckInProgress { get; set; } = false;
    public bool AllowSecondRoles { get; set; } = false;
    public bool AutoAcceptClaims { get; set; } = false;
    public bool EnableAccommodation { get; set; } = false;

    /// <summary>
    /// If true, character name is separate property of character, not bound to any field
    /// and could be edited only by master
    /// </summary>
    public bool CharacterNameLegacyMode { get; set; } = true;
    /// <summary>
    /// If legacy mode = true, always null
    /// If legacy mode = false.
    ///     Null = bound to player name
    ///     Other value = bound to that field
    /// </summary>
    [CanBeNull]
    public ProjectField? CharacterNameField { get; set; }
    /// <summary>
    ///     Null = no character description
    ///     Other value = bound to that field
    /// </summary>
    [CanBeNull]
    public ProjectField? CharacterDescription { get; set; }

    /// <summary>
    /// If schedule module enabled on project
    /// </summary>
    public bool ScheduleEnabled { get; set; }

    public string FieldsOrdering { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (CharacterNameField != null)
        {
            if (CharacterNameLegacyMode)

            {
                yield return new ValidationResult("Legacy mode is enabled");
            }
            if (CharacterNameField.FieldType != ProjectFieldType.String || CharacterNameField.FieldBoundTo != FieldBoundTo.Character)
            {
                yield return new ValidationResult("Incorrect type of field", new[] { nameof(CharacterNameField) });
            }
        }
        if (CharacterDescription != null &&
            (CharacterDescription.FieldType != ProjectFieldType.Text || CharacterDescription.FieldBoundTo != FieldBoundTo.Character))
        {
            yield return new ValidationResult("Incorrect type of field", new[] { nameof(CharacterDescription) });
        }
    }
}
