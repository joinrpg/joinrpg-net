using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace JoinRpg.Web.Models
{
  public class AddCharacterViewModel : EditGameObjectViewModelBase
  {
    [DisplayName("Принимать заявки на этого персонажа")]
    public bool IsAcceptingClaims { get; set; } = true;

    [DisplayName("Имя персонажа"), Required]
    public string Name { get; set; }

    public override IEnumerable<CharacterGroupListItemViewModel> PossibleParents => Data.ActiveGroups;
  }

}