using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace JoinRpg.Web.Models
{

  public abstract class CharacterViewModelBase : GameObjectViewModelBase, IValidatableObject
  {
    [DisplayName("Принимать заявки на этого персонажа")]
    public bool IsAcceptingClaims
    { get; set; } = true;

    [DisplayName("Имя персонажа"), Required]
    public string Name
    { get; set; }
    public override IEnumerable<CharacterGroupListItemViewModel> PossibleParents => Data.ActiveGroups;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
      if (!ParentCharacterGroupIds.Any())
      {
        yield return new ValidationResult("Персонаж должен принадлежать хотя бы к одной группе");
      }
    }

    [Display(Name="Всегда скрывать имя игрока"), Required]
    public bool HidePlayerForCharacter { get; set; }
  }
  public class AddCharacterViewModel : CharacterViewModelBase
  {
  }

  public class EditCharacterViewModel : CharacterViewModelBase
  {
    public int CharacterId { get; set; }
  }

}