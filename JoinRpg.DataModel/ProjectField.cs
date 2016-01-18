using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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

    public bool IsPublic { get; set; }

    public bool CanPlayerView { get; set; }

    public bool CanPlayerEdit { get; set; }

    public MarkdownString Description { get; set; }

    public virtual Project Project { get; set; }

    public int ProjectId { get; set; }
    int IOrderableEntity.Id => ProjectFieldId;

    public bool IsActive { get; set; }

    public bool WasEverUsed { get; set; }

    bool IDeletableSubEntity.CanBePermanentlyDeleted => !WasEverUsed;

    public virtual ICollection<ProjectFieldDropdownValue> DropdownValues { get; set; }

    public string ValuesOrdering { get; set; }

    public virtual CharacterGroup CharacterGroup { get; set; }
    public int CharacterGroupId { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
      if (IsPublic && !CanPlayerView)
      {
        yield return
          new ValidationResult("Public fields must be player visible",
            new List<string> {nameof(IsPublic), nameof(CanPlayerView)});
      }

      if (!CanPlayerView && CanPlayerEdit)
      {
        yield return
          new ValidationResult("It's incosistent that player can edit but can't see field.",
            new List<string> {nameof(IsPublic), nameof(CanPlayerView)});
      }
    }
  }

  public enum ProjectFieldType
  {
    String,
    Text,
    Dropdown,
    Checkbox,
    MultiSelect,
    Header
  }

  public enum FieldBoundTo
  {
    Character,
    Claim
  }
}