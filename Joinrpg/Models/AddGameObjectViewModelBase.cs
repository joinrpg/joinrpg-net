using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace JoinRpg.Web.Models
{
  public class AddGameObjectViewModelBase
  {
    public int ProjectId { get; set; }

    [Required, DisplayName("Является частью локаций")]
    public List<int> ParentCharacterGroupIds { get; set; } = new List<int>();

    [Required]
    public string Name
    { get; set; }

    [DisplayName("Публично?")]
    public bool IsPublic { get; set; } = true;

    [DataType(DataType.MultilineText),DisplayName("Описание")]
    public string Description { get; set; }
    public CharacterGroupListViewModel Data { get; set; }
  }
}