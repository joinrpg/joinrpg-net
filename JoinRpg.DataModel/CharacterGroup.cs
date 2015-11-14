using System.Collections.Generic;
using System.Linq;
using JoinRpg.Helpers;

namespace JoinRpg.DataModel
{
  // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global (virtual methods used by LINQ)
  public class CharacterGroup : IClaimSource, IDeletableSubEntity
  {

    public int CharacterGroupId { get; set; }

    public int ProjectId { get; set; }
    int IOrderableEntity.Id => CharacterGroupId;

    public virtual Project Project { get; set; }

    public string CharacterGroupName { get; set; }

    public bool IsRoot { get; set; }

    public virtual ICollection<CharacterGroup> ParentGroups { get; set; }
    string IWorldObject.Name => CharacterGroupName;

    /// <summary>
    /// 
    /// </summary>
    /// <remarks>Check may be you need to use ordered groups</remarks>
    public virtual ICollection<CharacterGroup> ChildGroups { get; set; }

    public IEnumerable<CharacterGroup> ActiveChildGroups => ChildGroups.Where(cg => cg.IsActive);

    public bool IsPublic { get; set; }

    public int AvaiableDirectSlots { get; set; }

    public bool HaveDirectSlots { get; set; }

    public bool DirectSlotsUnlimited => AvaiableDirectSlots == -1;

    public virtual ICollection<Character> Characters { get; set; }

    public bool IsActive { get; set; }

    public MarkdownString Description { get; set; } = new MarkdownString();

    /// <summary>
    /// Can add claim directly to character group (not individual characters)
    /// </summary>
    public bool IsAvailable => HaveDirectSlots && AvaiableDirectSlots != 0;

    public virtual ICollection<Claim> Claims { get; set; }

    public virtual User ResponsibleMasterUser { get; set; }

    public int? ResponsibleMasterUserId { get; set; }

    public string ChildCharactersOrdering { get; set; }
    public string ChildGroupsOrdering { get; set; }

    public virtual ICollection<PlotFolder> DirectlyRelatedPlotFolders { get; set; }

    public virtual ICollection<PlotElement> DirectlyRelatedPlotElements { get; set; }

    public virtual ICollection<UserSubscription> Subscriptions { get; set; }

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
