using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using JoinRpg.DataModel;
using JoinRpg.Helpers.Validation;
using JoinRpg.PrimitiveTypes;

namespace JoinRpg.Web.Models.Characters;

public abstract class CharacterViewModelBase : IProjectIdAware, IValidatableObject
{
    public int ProjectId { get; set; }

    [ReadOnly(true)]
    public string ProjectName { get; set; }

    [Display(Name = "Публично?",
        Description =
            "Публичные сущности показываются в сетке ролей, их описание и карточки доступны всем.")]
    public bool IsPublic { get; set; } = true;

    [Required]
    public CharacterTypeInfo CharacterTypeInfo { get; set; }

    [ReadOnly(true)]
    public bool CharactersHaveNameField { get; set; }

    [DisplayName("Имя персонажа")]
    public string Name { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!ParentCharacterGroupIds.Any())
        {
            yield return new ValidationResult(
                "Персонаж должен принадлежать хотя бы к одной группе");
        }
    }

    [Display(Name = "Всегда скрывать имя игрока",
        Description = "Скрыть личность игрока, который играет данного персонажа.")]
    public bool HidePlayerForCharacter { get; set; }

    public CustomFieldsViewModel Fields { get; set; }

    [CannotBeEmpty, DisplayName("Является частью групп")]
    public int[] ParentCharacterGroupIds { get; set; } = new int[0] { };

    protected void FillFields(Character field, int currentUserId)
    {
        Fields = new CustomFieldsViewModel(currentUserId, field);
        CharactersHaveNameField = field.Project.Details.CharacterNameField is not null;
    }
}
