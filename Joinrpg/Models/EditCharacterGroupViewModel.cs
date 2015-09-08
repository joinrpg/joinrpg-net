using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace JoinRpg.Web.Models
{
  public class EditCharacterGroupViewModel : AddGameObjectViewModelBase
  {
    public int CharacterGroupId { get; set; }
    public string OriginalName { get; set; }

    [DisplayName("Название локации"), Required]
    public string Name { get; set; }
  }

  public class AddCharacterGroupViewModel : AddGameObjectViewModelBase
  {
    [DisplayName("Название локации"), Required]
    public string Name { get; set; }
  }
}
