using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using JoinRpg.Helpers;

namespace JoinRpg.DataModel
{
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
    /// Price associated with current field.
    /// Will be used in payment calculations.
    /// Value usage differs on FieldType. Value will be:
    /// - Number: multiplied with entered number
    /// - CheckBox: used if checkbox was checked
    /// - Dropdown, Multiselect: ignored, see values for prices
    /// - String, Text, Header: ignored
    /// </summary>
    public int Price { get; set; }

    bool IDeletableSubEntity.CanBePermanentlyDeleted => !WasEverUsed;

    public virtual ICollection<ProjectFieldDropdownValue> DropdownValues { get; set; } =
      new HashSet<ProjectFieldDropdownValue>();

    public string ValuesOrdering { get; set; }

    public virtual CharacterGroup CharacterGroup { get; set; }
    public int CharacterGroupId { get; set; }

    [NotMapped]
    public int[] AvailableForCharacterGroupIds
    {
      get { return AviableForImpl._parentCharacterGroupIds; }
      set { AviableForImpl.ListIds = value.Select(v => v.ToString()).JoinStrings(","); }
    }

    public IntList AviableForImpl { get; set; } = new IntList();

    public virtual ICollection<PluginFieldMapping> Mappings { get; set; }= new HashSet<PluginFieldMapping>();

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
            new List<string> {nameof(IsPublic), nameof(CanPlayerView)});
      }

      if (!CanPlayerView && CanPlayerEdit)
      {
        yield return
          new ValidationResult("It's incosistent that player can edit but can't see field.",
            new List<string> {nameof(IsPublic), nameof(CanPlayerView)});
      }

      if (FieldBoundTo == FieldBoundTo.Claim && ValidForNpc)
      {
        yield return
          new ValidationResult("Claim-bound field shoudn't be valid for NPC.",
            new List<string> {nameof(FieldBoundTo), nameof(ValidForNpc)});
      }

      if (!CanPlayerView && IncludeInPrint)
      {
        yield return 
          new ValidationResult("Include in print fields must be player visible.");
      }

      if (MandatoryStatus != MandatoryStatus.Optional && FieldType == ProjectFieldType.Header)
      {
        yield return  new ValidationResult("Header can't be mandatory");
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
  
  public enum FieldBoundTo
  {
    Character,
    Claim,
  }

  public enum MandatoryStatus
  {
    Optional, 
    Recommended,
    Required,
  }
}
