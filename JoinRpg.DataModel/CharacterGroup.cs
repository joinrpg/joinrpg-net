using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JoinRpg.DataModel
{
  public class CharacterGroup : IWorldObject
  {

    public int CharacterGroupId { get; set; }

    public int ProjectId { get; set; }

    public virtual Project Project { get; set; }

    public string CharacterGroupName { get; set; }

    public bool IsRoot { get; set; }

    public virtual ICollection<CharacterGroup> ParentGroups { get; set; }
    string IWorldObject.Name => CharacterGroupName;

    public virtual ICollection<CharacterGroup> ChildGroups { get; set; }

    public bool IsPublic { get; set; }

    public int AvaiableDirectSlots { get; set; }

    public virtual ICollection<Character> Characters { get; set; }

    public bool IsActive { get; set; }

    public MarkdownString Description { get; set; } = new MarkdownString();

    public bool IsAvailable => AvaiableDirectSlots > 0;

    public virtual ICollection<Claim>  Claims { get; set; }
  }

}
