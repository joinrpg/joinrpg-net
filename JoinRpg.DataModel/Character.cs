using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JoinRpg.DataModel
{
  public class Character
  {
    public int CharacterId { get; set; }
    public int ProjectId { get; set; }

    public virtual Project Project { get; set; }

    public virtual ICollection<CharacterGroup> Groups { get; set; }

    public string Name { get; set; }

    public bool IsPublic { get; set; }

    /// <summary>
    /// By default, all characters playable. NPC have this set to false, so players can't apply to this Character
    /// </summary>
    public bool IsPlayable { get; set; }

    /// <summary>
    /// Contains values of fields for this character
    /// </summary>
    public string JsonData { get; set; }

    public bool IsActive { get; set; }
  }

  
}
