using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using JoinRpg.DataModel;

namespace JoinRpg.Web.Models
{
  public class AddGameObjectViewModelBase
  {
    [Required]
    public int ProjectId { get; set; }

    [Required]
    public List<int> ParentCharacterGroupIds { get; set; } = new List<int>();

    [Required]
    public string Name
    { get; set; }

    public bool IsPublic { get; set; } = true;

    [DataType(DataType.MultilineText)]
    public string Description { get; set; }
    public CharacterGroupListViewModel Data { get; set; }
  }
}