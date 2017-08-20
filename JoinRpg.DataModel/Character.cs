using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using JetBrains.Annotations;
using JoinRpg.Helpers;

namespace JoinRpg.DataModel
{
  // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global used by LINQ

  public class Character : IClaimSource, IFieldContainter, ICreatedUpdatedTrackedForEntity, ILinkable
  {
    public int CharacterId { get; set; }
    public int ProjectId { get; set; }
    int IOrderableEntity.Id => CharacterId;
    [NotMapped]
    public int[] ParentCharacterGroupIds
    {
      get { return ParentGroupsImpl._parentCharacterGroupIds; }
      set { ParentGroupsImpl.ListIds = value.Select(v => v.ToString()).JoinStrings(","); }
    }

    public IntList ParentGroupsImpl { get; set; } = new IntList();

    public virtual Project Project { get; set; }

    IEnumerable<CharacterGroup> IWorldObject.ParentGroups => Groups;

    public IEnumerable<CharacterGroup> Groups => Project.CharacterGroups.Where(c => ParentCharacterGroupIds.Contains(c.CharacterGroupId));

    public string CharacterName { get; set; }

    string IWorldObject.Name => CharacterName;

    public bool IsPublic { get; set; }

    public bool IsAcceptingClaims { get; set; }

    /// <summary>
    /// Contains values of fields for this character
    /// </summary>
    public string JsonData { get; set; }

    public bool CanBePermanentlyDeleted = false;//TODO: remove the property as well.

    public bool IsActive { get; set; }

    public MarkdownString Description { get; set; } = new MarkdownString();

    public virtual IEnumerable<Claim> Claims => Project.Claims.Where(c => c.CharacterId == CharacterId);

    [CanBeNull, InverseProperty(null)]
    public virtual  Claim ApprovedClaim { get; set; }

    [ForeignKey(nameof(ApprovedClaim)), InverseProperty(null)]
    public int? ApprovedClaimId { get; set; }

    public virtual ICollection<UserSubscription> Subscriptions { get; set; }
    public bool IsRoot => false; //Character is not "root group"

    /// <summary>
    /// Is available for claims (IsAcceptingClaims + has no approved claim)
    /// </summary>
    public bool IsAvailable => IsAcceptingClaims && ApprovedClaim == null && Project.IsAcceptingClaims;

    public User ResponsibleMasterUser => null; // We don't implement yet of setting responsible masters for indv. characters. I think the group will be enough now

    public string PlotElementOrderData { get; set; }

    public virtual ICollection<PlotElement> DirectlyRelatedPlotElements { get; set; }

    public bool HidePlayerForCharacter { get; set; }

    public bool IsHot { get; set; }

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

    /// <summary>
    /// Sets to true if this character is actually playing NOW.
    /// </summary>
    public bool InGame { get; set; } = false;

    public bool AutoCreated { get; set; } = false;

    LinkType ILinkable.LinkType => LinkType.ResultCharacter;

    string ILinkable.Identification => CharacterId.ToString();
    int? ILinkable.ProjectId => ProjectId;
  }

  
}
