using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using JoinRpg.Helpers;
using JoinRpg.PrimitiveTypes;

namespace JoinRpg.DataModel;


public class Character : IWorldObject, IFieldContainter, ICreatedUpdatedTrackedForEntity, ILinkableWithName
{
    public int CharacterId { get; set; }
    public int ProjectId { get; set; }
    int IOrderableEntity.Id => CharacterId;
    [NotMapped]
    public int[] ParentCharacterGroupIds
    {
        get => ParentGroupsImpl._parentCharacterGroupIds;
        set => ParentGroupsImpl.ListIds = value.Select(v => v.ToString()).JoinStrings(",");
    }

    public IntList ParentGroupsImpl { get; set; } = new IntList();

    public virtual Project Project { get; set; }

    IEnumerable<CharacterGroup> IWorldObject.ParentGroups => Groups;

    public IEnumerable<CharacterGroup> Groups => Project.CharacterGroups.Where(c => ParentCharacterGroupIds.Contains(c.CharacterGroupId));

    public string CharacterName { get; set; }

    string IWorldObject.Name => CharacterName;
    string ILinkableWithName.Name => CharacterName;

    public bool IsPublic { get; set; }

    /// <summary>
    /// TODO удалить
    /// </summary>
    [Obsolete("Использовать CharacterType или ClaimSourceExtensions.IsAcceptingClaims()")]
    public bool IsAcceptingClaims { get; set; }

    /// <summary>
    /// Contains values of fields for this character
    /// </summary>
    public string JsonData { get; set; }

    public bool CanBePermanentlyDeleted = false;//TODO: remove the property as well.

    public bool IsActive { get; set; }

    /// <summary>
    /// This field used only in roles tree
    /// </summary>
    public MarkdownString Description { get; set; } = new MarkdownString();

    public virtual HashSet<Claim> Claims { get; set; } = [];

    public virtual Claim? ApprovedClaim { get; set; }

    [ForeignKey(nameof(ApprovedClaim))/*, InverseProperty(null)*/]
    public int? ApprovedClaimId { get; set; }

    public virtual ICollection<UserSubscription> Subscriptions { get; set; }
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

    public CharacterType CharacterType { get; set; }

    /// <summary>
    /// Maximum limit of characters that could be created from this slot
    /// </summary>
    public int? CharacterSlotLimit { get; set; }

    /// <summary>
    /// If this character was originally created from slot, link to original.
    /// </summary>
    public Character? OriginalCharacterSlot { get; set; }
}

