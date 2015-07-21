using System.ComponentModel;

namespace JoinRpg.Web.Models
{
  public class AddCharacterViewModel : AddGameObjectViewModelBase
  {
    [DisplayName("Принимать заявки на этого персонажа")]
    public bool IsAcceptingClaims { get; set; } = true;
  }
}
