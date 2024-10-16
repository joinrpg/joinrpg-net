using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using JoinRpg.DataModel;
using JoinRpg.PrimitiveTypes;
using JoinRpg.PrimitiveTypes.ProjectMetadata;

namespace JoinRpg.Web.Models.Characters;

public abstract class CharacterViewModelBase : IProjectIdAware, IValidatableObject
{
    public int ProjectId { get; set; }

    [ReadOnly(true)]
    public string ProjectName { get; set; }

    [Required]
    public CharacterTypeInfo CharacterTypeInfo { get; set; }

    [ReadOnly(true)]
    public bool CharactersHaveNameField { get; set; }

    [DisplayName("Имя персонажа")]
    public string Name { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (AllowToSetGroups && ParentCharacterGroupIds.Length == 0)
        {
            yield return new ValidationResult(
                "Персонаж должен принадлежать хотя бы к одной группе");
        }
    }

    public CustomFieldsViewModel Fields { get; set; }

    [DisplayName("Является частью групп")]
    public int[] ParentCharacterGroupIds { get; set; } = [];

    [ReadOnly(true)]
    public bool AllowToSetGroups { get; set; }

    protected void FillFields(Character field, int currentUserId, ProjectInfo projectInfo)
    {
        Fields = new CustomFieldsViewModel(currentUserId, field, projectInfo);
        CharactersHaveNameField = projectInfo.CharacterNameField is not null;
        AllowToSetGroups = projectInfo.AllowToSetGroups;
    }
}
