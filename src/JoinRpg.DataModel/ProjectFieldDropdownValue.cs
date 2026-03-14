using System.ComponentModel.DataAnnotations;
using JoinRpg.DataModel.Finances;
using JoinRpg.Helpers;

namespace JoinRpg.DataModel;

// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global used by LINQ
public class ProjectFieldDropdownValue : IDeletableSubEntity, IProjectEntity, IValidatableObject
{
    public int ProjectFieldDropdownValueId { get; set; }
    public int ProjectFieldId { get; set; }

    public virtual ProjectField ProjectField { get; set; }

    public int ProjectId { get; set; }

    int IOrderableEntity.Id => ProjectFieldDropdownValueId;

    public virtual Project Project { get; set; }

    bool IDeletableSubEntity.CanBePermanentlyDeleted => !WasEverUsed;

    public bool IsActive { get; set; }

    public bool WasEverUsed { get; set; }

    public bool PlayerSelectable { get; set; }

    [Required]
    public string Label { get; set; }

    public MarkdownString Description { get; set; }

    public MarkdownString MasterDescription { get; set; }

    #region Finance

    /// <summary>
    /// Base price associated with this field value.
    /// </summary>
    /// <remarks>
    /// Actual price depends on current date via <see cref="PriceValue"/> objects.
    /// </remarks>
    public int Price { get; set; }

    /// <summary>
    /// The name used to display in receipt.
    /// When not specified, the <see cref="Label"/> property value will be taken but truncated to 64 characters.
    /// </summary>
    [StringLength(64)]
    public string? ReceiptName { get; set; }

    /// <summary>
    /// Collection of related price values.
    /// </summary>
    public ICollection<PriceValue>? PriceValues { get; set; }

    #endregion

    /// <summary>
    /// External value for external IT systems
    /// </summary>
    public string? ProgrammaticValue { get; set; }

    public virtual CharacterGroup? CharacterGroup { get; set; }

    public virtual int? CharacterGroupId { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (PlayerSelectable && !ProjectField.CanPlayerEdit)
        {
            yield return new ValidationResult("Can't enable selection for variant, because field is not player-editable");
        }
    }
}
