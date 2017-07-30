using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using JetBrains.Annotations;
using JoinRpg.Helpers;

namespace JoinRpg.DataModel
{
  // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global (virtual methods used by LINQ)
  public class CharacterGroup : IClaimSource, IDeletableSubEntity, IValidatableObject, IEquatable<CharacterGroup>, ICreatedUpdatedTrackedForEntity
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

    [NotNull, ItemNotNull]
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

    // ReSharper disable once UnusedAutoPropertyAccessor.Global assigned by EF
    public virtual User ResponsibleMasterUser { get; set; }

    public int? ResponsibleMasterUserId { get; set; }

    public string ChildCharactersOrdering { get; set; }
    public string ChildGroupsOrdering { get; set; }

    // ReSharper disable once UnusedAutoPropertyAccessor.Global assigned by EF
    public virtual ICollection<PlotFolder> DirectlyRelatedPlotFolders { get; set; }

    // ReSharper disable once UnusedAutoPropertyAccessor.Global assigned by EF
    public virtual ICollection<PlotElement> DirectlyRelatedPlotElements { get; set; }

    // ReSharper disable once UnusedAutoPropertyAccessor.Global assigned by EF
    public virtual ICollection<UserSubscription> Subscriptions { get; set; }

    public bool CanBePermanentlyDeleted
      => !ChildGroups.Any() && !Characters.Any() && !DirectlyRelatedPlotFolders.Any() && !Claims.Any();

    #region Implementation of IValidatableObject
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
      if (!IsRoot && !ParentCharacterGroupIds.Any())
      {
        yield return new ValidationResult("Character group should be part of tree");
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
        if (ParentGroups.Any(pg => pg.CharacterGroupId == CharacterGroupId))
        {
          yield return new ValidationResult("Character group can't be self-child");
        }
      }
    }
    #endregion

    public bool Equals(CharacterGroup other) => other?.CharacterGroupId == CharacterGroupId;

    public virtual ICollection<ForumThread> ForumThreads { get; set; } = new HashSet<ForumThread>();

    [Required]
    public DateTime CreatedAt { get; set; }

    [ForeignKey(nameof(CreatedById))]
    public virtual User CreatedBy { get; set; }

    public int CreatedById { get; set; }

    [Required]
    public DateTime UpdatedAt { get; set; }

    [ForeignKey(nameof(UpdatedById))]
    public virtual User UpdatedBy { get; set; }

    public int UpdatedById { get; set; }
  }
}
