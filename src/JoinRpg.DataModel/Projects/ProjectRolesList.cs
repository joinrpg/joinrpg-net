using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Helpers;

namespace JoinRpg.DataModel;

public class ProjectRolesList : IProjectEntity, IValidatableObject
{
    [Key]
    public int ProjectRolesListId { get; set; }

    public int ProjectId { get; set; }

    [ForeignKey(nameof(ProjectId))]
    public virtual Project Project { get; set; }

    [Required]
    public string Name { get; set; }

    public int? CharacterGroupId { get; set; }

    [ForeignKey("CharacterGroupId")]
    public virtual CharacterGroup? CharacterGroup { get; set; }

    public bool PublicMode { get; set; }

    [NotMapped]
    public int[] FieldIds
    {
        get => FieldsImpl._parentCharacterGroupIds; // Reusing the same backing field pattern
        set => FieldsImpl.ListIds = value.Select(v => v.ToString()).JoinStrings(",");
    }

    public IntList FieldsImpl { get; set; } = new IntList();

    public ProjectRolesListVisibilityMode ContactsColumn { get; set; }

    public ProjectRolesListVisibilityMode GroupsColumn { get; set; }

    #region interface implementations

    int IOrderableEntity.Id => ProjectId;

    Project IProjectEntity.Project => Project;

    #endregion

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            yield return new ValidationResult("Name is required", [nameof(Name)]);
        }
    }
}
