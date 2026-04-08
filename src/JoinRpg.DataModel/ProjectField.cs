using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using JoinRpg.DataModel.Finances;
using JoinRpg.Helpers;
using JoinRpg.PrimitiveTypes.ProjectMetadata;

namespace JoinRpg.DataModel;

// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global (used by LINQ)
public class ProjectField : IProjectEntity, IDeletableSubEntity, IValidatableObject
{
    public int ProjectFieldId { get; set; }

    public string FieldName { get; set; }

    public ProjectFieldType FieldType { get; set; }

    public FieldBoundTo FieldBoundTo { get; set; }

    public MandatoryStatus MandatoryStatus { get; set; }

    public bool IsPublic { get; set; }

    public bool CanPlayerView { get; set; }

    public bool CanPlayerEdit { get; set; }

    public MarkdownString Description { get; set; }

    public MarkdownString MasterDescription { get; set; }

    public virtual Project Project { get; set; }

    public int ProjectId { get; set; }
    int IOrderableEntity.Id => ProjectFieldId;

    public bool IsActive { get; set; }

    public bool WasEverUsed { get; set; }

    public bool ValidForNpc { get; set; }

    public bool IncludeInPrint { get; set; }

    public bool ShowOnUnApprovedClaims { get; set; }

    /// <summary>
    /// When true, this field may have assigned prices.
    /// </summary>
    /// <remarks>
    /// Has effect only when field has certain <see cref="FieldType"/>:
    /// <ul>
    /// <li><see cref="ProjectFieldType.Checkbox"/></li>
    /// <li><see cref="ProjectFieldType.Number"/></li>
    /// <li><see cref="ProjectFieldType.Dropdown"/></li>
    /// <li><see cref="ProjectFieldType.MultiSelect" /></li>
    /// </ul>
    /// </remarks>
    public bool Payable { get; set; }

    /// <summary>
    /// Base price associated with this field.
    /// </summary>
    /// <remarks>
    /// Actual price depends on current date via <see cref="PriceValue"/> objects.
    /// </remarks>
    public int Price { get; set; }

    /// <summary>
    /// Defines how this field should be interpreted when preparing a receipt.
    /// </summary>
    public ReceiptItemType ReceiptItemType { get; set; }

    /// <summary>
    /// The name used to display in receipt. When not specified, the <see cref="FieldName"/> is used.
    /// </summary>
    /// <remarks>
    /// When <see cref="FieldType"/> is <see cref="ProjectFieldType.Dropdown"/> or <see cref="ProjectFieldType.MultiSelect"/>
    /// this value has to be specified independently in each <see cref="ProjectFieldDropdownValue"/>.
    /// </remarks>
    [StringLength(64)]
    public string? ReceiptName { get; set; }

    /// <summary>
    /// When true, a discount can be applied to price of this field.
    /// </summary>
    /// <remarks>
    /// Whereas the relative discount set in <see cref="Claim.DiscountPercent"/> will only affect the total sum while calculation,
    /// the <see cref="Claim.DiscountedPrice"/> will exclude discountable field from total calculation.
    /// </remarks>
    public bool Discountable { get; set; }

    /// <summary>
    /// Collection of related price values.
    /// </summary>
    public ICollection<PriceValue>? PriceValues { get; set; }

    bool IDeletableSubEntity.CanBePermanentlyDeleted => !WasEverUsed;

    public virtual ICollection<ProjectFieldDropdownValue> DropdownValues { get; set; } =
      new HashSet<ProjectFieldDropdownValue>();

    public string ValuesOrdering { get; set; }

    public virtual CharacterGroup? CharacterGroup { get; set; }
    public virtual int? CharacterGroupId { get; set; }

    /// <summary>
    /// External value for external IT systems
    /// </summary>
    public string? ProgrammaticValue { get; set; }

    [NotMapped]
    public int[] AvailableForCharacterGroupIds
    {
        get => AviableForImpl._parentCharacterGroupIds;
        set => AviableForImpl.ListIds = value.Select(v => v.ToString()).JoinStrings(",");
    }

    public IntList AviableForImpl { get; set; } = new IntList();

    public IEnumerable<CharacterGroup> GroupsAvailableFor
    {
        get
        {
            if (!AvailableForCharacterGroupIds.Any())
            {
                return Enumerable.Empty<CharacterGroup>();
                // For most common case skip touching Project.CharacterGroups
                // As it may trigger lazy load
            }
            return Project.CharacterGroups.Where(c => AvailableForCharacterGroupIds.Contains(c.CharacterGroupId));
        }
    }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (IsPublic && !CanPlayerView)
        {
            yield return
              new ValidationResult("Public fields must be player visible.",
                new List<string> { nameof(IsPublic), nameof(CanPlayerView) });
        }

        if (!CanPlayerView && CanPlayerEdit)
        {
            yield return
              new ValidationResult("It's incosistent that player can edit but can't see field.",
                new List<string> { nameof(IsPublic), nameof(CanPlayerView) });
        }

        if (FieldBoundTo == FieldBoundTo.Claim && ValidForNpc)
        {
            yield return
              new ValidationResult("Claim-bound field shoudn't be valid for NPC.",
                new List<string> { nameof(FieldBoundTo), nameof(ValidForNpc) });
        }

        if (!CanPlayerView && IncludeInPrint)
        {
            yield return
              new ValidationResult("Include in print fields must be player visible.");
        }

        if (MandatoryStatus != MandatoryStatus.Optional && FieldType == ProjectFieldType.Header)
        {
            yield return new ValidationResult("Header can't be mandatory");
        }

        if (!FieldType.SupportsPricing() && Price != 0)
        {
            yield return new ValidationResult(string.Format("Pricing is not supported for {0} fields", FieldType.ToString()));
        }

        if (FieldType.SupportsPricing() && !FieldType.SupportsPricingOnField() && Price != 0)
        {
            yield return new ValidationResult(string.Format("Fields with multiple values support pricing only in values"));
        }

    }

    public override string ToString() => $"ProjectField(Id={ProjectFieldId}, Name={FieldName})";
}
