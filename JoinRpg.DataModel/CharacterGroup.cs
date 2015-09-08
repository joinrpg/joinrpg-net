using System;
using System.Collections.Generic;
using System.Linq;

namespace JoinRpg.DataModel
{
  // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global (virtual methods used by LINQ)
  public class CharacterGroup : IClaimSource, IDeletableSubEntity
  {

    public int CharacterGroupId { get; set; }

    public int ProjectId { get; set; }
    int IProjectSubEntity.Id => CharacterGroupId;

    public virtual Project Project { get; set; }

    public string CharacterGroupName { get; set; }

    public bool IsRoot { get; set; }

    public virtual ICollection<CharacterGroup> ParentGroups { get; set; }
    string IWorldObject.Name => CharacterGroupName;

    public virtual ICollection<CharacterGroup> ChildGroups { get; set; }

    public IEnumerable<CharacterGroup> ActiveChildGroups => ChildGroups.Where(cg => cg.IsActive);

    public bool IsPublic { get; set; }

    public int AvaiableDirectSlots { get; set; }

    public virtual ICollection<Character> Characters { get; set; }

    public bool IsActive { get; set; }

    public MarkdownString Description { get; set; } = new MarkdownString();

    public bool IsAvailable => AvaiableDirectSlots > 0;

    public virtual ICollection<Claim>  Claims { get; set; }

    public virtual ICollection<PlotFolder> DirectlyRelatedPlotFolders { get; set; }

    public virtual ICollection<PlotElement>  DirectlyRelatedPlotElements { get; set; }

    public IEnumerable<Character> CharactersWithOnlyParent => WithOnlyParent(Characters);
    public IEnumerable<CharacterGroup> ChildGroupsWithOnlyParent => WithOnlyParent(ChildGroups);

    public bool CanBePermanentlyDeleted
      => !ChildGroups.Any() && !Characters.Any() && !DirectlyRelatedPlotFolders.Any();

    private IEnumerable<T> WithOnlyParent<T>(IEnumerable<T> worldObjects) where T:IWorldObject
    {
      return worldObjects.Where(obj => obj.ParentGroups.All(group => @group.CharacterGroupId == CharacterGroupId));
    }
  }

}
