using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace JoinRpg.Web.Models
{
  public abstract class AddGameObjectViewModelBase
  {
    public int ProjectId { get; set; }

    [Required, DisplayName("Является частью локаций")]
    public List<int> ParentCharacterGroupIds { get; set; } = new List<int>();

    [DisplayName("Публично?")]
    public bool IsPublic { get; set; } = true;

    [DataType(DataType.MultilineText),DisplayName("Описание")]
    public string Description { get; set; }
    public CharacterGroupListViewModel Data { get; set; }
  }
}