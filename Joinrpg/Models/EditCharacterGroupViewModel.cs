namespace JoinRpg.Web.Models
{
  public class EditCharacterGroupViewModel : AddGameObjectViewModelBase
  {
    public int CharacterGroupId { get; set; }
    public string OriginalName { get; set; }
  }

  public class AddCharacterGroupViewModel : AddGameObjectViewModelBase
  {
  }
}
