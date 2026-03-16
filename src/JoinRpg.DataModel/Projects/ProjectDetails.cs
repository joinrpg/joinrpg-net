using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using JoinRpg.DataModel.Finances;
using JoinRpg.PrimitiveTypes.ProjectMetadata;

namespace JoinRpg.DataModel;

public class ProjectDetails : IValidatableObject
{
    [Key] public int ProjectId { get; set; }
    public MarkdownString ClaimApplyRules { get; set; } = new MarkdownString();
    public MarkdownString ProjectAnnounce { get; set; } = new MarkdownString();

    public bool EnableManyCharacters { get; set; }
    public bool PublishPlot { get; set; }

    #region Finance

    public bool FinanceWarnOnOverPayment { get; set; } = true;

    /// <summary>
    /// The minimal required payment to be made to stop increasing the total price depending on date.
    /// </summary>
    /// <remarks>
    /// This property is not mutually exclusive with <see cref="MinPaidPercentToFixPrice"/>.
    /// When both defined, price will be fixed when the first condition has met.
    /// </remarks>
    [Range(0, int.MaxValue)]
    public int? MinPaymentToFixPrice { get; set; }

    /// <summary>
    /// The minimal required payment in percent from total to be made to stop increasing the total price depending on date.
    /// </summary>
    /// <remarks>
    /// This property is not mutually exclusive with <see cref="MinPaymentToFixPrice"/>.
    /// When both defined, price will be fixed when the first condition has met.
    /// </remarks>
    [Range(0, 100)]
    public int? MinPaidPercentToFixPrice { get; set; }

    /// <summary>
    /// When true, a discount could be set for a claim payment.
    /// </summary>
    public bool EnableDiscounts { get; set; }

    /// <summary>
    /// Describes the conditions a user should meet to get a discount.
    /// </summary>
    public MarkdownString DiscountConditions { get; set; } = new();

    /// <summary>
    /// Defines how the primary position in receipt should be identified.
    /// </summary>
    /// <remarks>The <see cref="ReceiptItemType.IncludeIntoPrimary"/> is not allowed here.</remarks>
    public ReceiptItemType PrimaryPaymentReceiptItemType { get; set; } = ReceiptItemType.Service;

    /// <summary>
    /// The named used to display in receipt.
    /// When not specified, it will be automatically combined from the project's name and the word "Билет".
    /// </summary>
    [StringLength(64)]
    public string? PrimaryPaymentReceiptName { get; set; }

    [Obsolete("Use the Discountable instead")]
    public bool PreferentialFeeEnabled { get; set; } = false;

    [Obsolete("Use the DiscountConditions instead")]
    public MarkdownString PreferentialFeeConditions { get; set; } = new MarkdownString();

    #endregion

    public bool EnableCheckInModule { get; set; } = false;
    public bool CheckInProgress { get; set; } = false;
    public bool AllowSecondRoles { get; set; } = false;
    public bool AutoAcceptClaims { get; set; } = false;
    public bool EnableAccommodation { get; set; } = false;

    /// <summary>
    ///   Null = bound to player name
    ///   Other value = bound to that field
    /// </summary>
    public virtual ProjectField? CharacterNameField { get; set; }
    /// <summary>
    ///     Null = no character description
    ///     Other value = bound to that field
    /// </summary>
    public virtual ProjectField? CharacterDescription { get; set; }

    /// <summary>
    /// If schedule module enabled on project
    /// </summary>
    public bool ScheduleEnabled { get; set; }

    public string FieldsOrdering { get; set; }

    public string? PlotFoldersOrdering { get; set; }

    [ForeignKey(nameof(DefaultTemplateCharacter))]
    public int? DefaultTemplateCharacterId { get; set; }

    public virtual Character? DefaultTemplateCharacter { get; set; }

    public int? ClonedFromProjectId { get; set; }

    public Project? ClonedFromProject { get; set; }

    public bool DisableKogdaIgraMapping { get; set; }

    public ProjectCloneSettings ProjectCloneSettings { get; set; } = ProjectCloneSettings.CanBeClonedByMaster;

    public MandatoryStatus RequireRealName { get; set; } = MandatoryStatus.Optional;
    public MandatoryStatus RequireTelegram { get; set; } = MandatoryStatus.Optional;
    public MandatoryStatus RequireVkontakte { get; set; } = MandatoryStatus.Optional;
    public MandatoryStatus RequirePhone { get; set; } = MandatoryStatus.Optional;

    public MandatoryStatus RequirePassport { get; set; } = MandatoryStatus.Optional;
    public MandatoryStatus RequireRegistrationAddress { get; set; } = MandatoryStatus.Optional;
    public bool IsPublicProject { get; set; } = true;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (CharacterNameField != null)
        {
            if (CharacterNameField.FieldType != ProjectFieldType.String || CharacterNameField.FieldBoundTo != FieldBoundTo.Character)
            {
                yield return new ValidationResult("Incorrect type of field", [nameof(CharacterNameField)]);
            }
        }
        if (CharacterDescription != null &&
            (CharacterDescription.FieldType != ProjectFieldType.Text || CharacterDescription.FieldBoundTo != FieldBoundTo.Character))
        {
            yield return new ValidationResult("Incorrect type of field", [nameof(CharacterDescription)]);
        }
    }
}
