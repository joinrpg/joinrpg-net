using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace JoinRpg.Web.Models
{
  public class AddGameObjectViewModelBase
  {
    public int ProjectId { get; set; }

    [Required]
    public List<int> ParentCharacterGroupIds { get; set; } = new List<int>();

    [Required]
    public string Name
    { get; set; }

    public bool IsPublic { get; set; } = true;
    public CharacterGroupListViewModel Data { get; set; }
  }
}