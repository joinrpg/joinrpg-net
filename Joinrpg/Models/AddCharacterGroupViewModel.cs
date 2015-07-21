using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JoinRpg.Web.Models
{
  public class AddCharacterGroupViewModel 
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
