using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace JoinRpg.Web.Models
{
  public class AddCharacterViewModel : AddGameObjectViewModelBase
  {
    [DisplayName("Принимать заявки на этого персонажа")]
    public bool IsAcceptingClaims { get; set; } = true;

    [DisplayName("Имя персонажа"), Required]
    public string Name
    { get; set; }
  }
}
