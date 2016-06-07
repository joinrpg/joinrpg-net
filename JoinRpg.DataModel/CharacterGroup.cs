using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using JetBrains.Annotations;
using JoinRpg.Helpers;

namespace JoinRpg.DataModel
{
  [ComplexType]
  public class IntList
  {
    private string _internalData;
    internal int[] _parentCharacterGroupIds;

    [EditorBrowsable(EditorBrowsableState.Never), UsedImplicitly]
    public string ListIds
    {
      get { return _internalData; }
      set
      {
        _internalData = value;
        _parentCharacterGroupIds = value.ToIntList();
      }
    }
  }
  // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global (virtual methods used by LINQ)
  public class CharacterGroup : IClaimSource, IDeletableSubEntity, IValidatableObject
  {


    public int CharacterGroupId { get; set; }

    public int ProjectId { get; set; }
    int IOrderableEntity.Id => CharacterGroupId;

    public virtual Project Project { get; set; }

    public string CharacterGroupName { get; set; }

    public bool IsRoot { get; set; }

    [NotMapped]
    public int[] ParentCharacterGroupIds
    {
      get { return ParentGroupsImpl._parentCharacterGroupIds; }
      set { ParentGroupsImpl.ListIds = value.Select(v => v.ToString()).JoinStrings(","); }
    }

    public IntList ParentGroupsImpl { get; set; } = new IntList();

    public IEnumerable<CharacterGroup> ParentGroups
      => Project.CharacterGroups.Where(c => ParentCharacterGroupIds.Contains(c.CharacterGroupId));

    string IWorldObject.Name => CharacterGroupName;

    /// <summary>
    /// 
    /// </summary>
    /// <remarks>Check may be you need to use ordered groups</remarks>
    public virtual IEnumerable<CharacterGroup> ChildGroups
      => Project.CharacterGroups.Where(cg => cg.ParentCharacterGroupIds.Contains(CharacterGroupId));

    public bool IsPublic { get; set; }
    public bool IsSpecial { get; set; }

    public int AvaiableDirectSlots { get; set; }

    public bool HaveDirectSlots { get; set; }

    public bool DirectSlotsUnlimited => AvaiableDirectSlots == -1;

    public IEnumerable<Character> Characters => Project.Characters.Where(cg => cg.ParentCharacterGroupIds.Contains(CharacterGroupId));

    public bool IsActive { get; set; }

    public MarkdownString Description { get; set; } = new MarkdownString();

    /// <summary>
    /// Can add claim directly to character group (not individual characters)
    /// </summary>
    public bool IsAvailable => HaveDirectSlots && AvaiableDirectSlots != 0 && Project.IsAcceptingClaims;

    public IEnumerable<Claim> Claims => Project.Claims.Where(c => c.CharacterGroupId == CharacterGroupId);

    public virtual User ResponsibleMasterUser { get; set; }

    public int? ResponsibleMasterUserId { get; set; }

    public string ChildCharactersOrdering { get; set; }
    public string ChildGroupsOrdering { get; set; }

    public virtual ICollection<PlotFolder> DirectlyRelatedPlotFolders { get; set; }

    public virtual ICollection<PlotElement> DirectlyRelatedPlotElements { get; set; }

    public virtual ICollection<UserSubscription> Subscriptions { get; set; }

    public bool CanBePermanentlyDeleted
      => !ChildGroups.Any() && !Characters.Any() && !DirectlyRelatedPlotFolders.Any() && !Claims.Any();

    #region Implementation of IValidatableObject
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
      if (ParentGroups == null)
      {
        yield break; //Prevent validation
      }
      if (IsSpecial)
      {
        if (ParentGroups.Any(pg => !pg.IsRoot && !pg.IsSpecial))
        {
          yield return new ValidationResult("Special group can only be child of root group or another special group");
        }
      }
      else
      {
        if (ParentGroups.Any(pg => pg.IsSpecial))
        {
          yield return new ValidationResult("Non-special group can't be child of special group");
        }
      }
    }
    #endregion
  }

}
