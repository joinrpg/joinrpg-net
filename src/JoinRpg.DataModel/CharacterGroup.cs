using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using JoinRpg.Helpers;
using JoinRpg.PrimitiveTypes;

namespace JoinRpg.DataModel;

// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global (virtual methods used by LINQ)
public class CharacterGroup : IWorldObject, IDeletableSubEntity, IValidatableObject, IEquatable<CharacterGroup>, ICreatedUpdatedTrackedForEntity, ILinkableWithName
{
    public int CharacterGroupId { get; set; }

    public LinkType LinkType => LinkType.ResultCharacterGroup;
    public string Identification => CharacterGroupId.ToString();
    int? ILinkable.ProjectId => ProjectId;

    public int ProjectId { get; set; }
    int IOrderableEntity.Id => CharacterGroupId;

    public virtual Project Project { get; set; }

    public string CharacterGroupName { get; set; }

    public bool IsRoot { get; set; }

    [NotMapped]
    public int[] ParentCharacterGroupIds
    {
        get => ParentGroupsImpl._parentCharacterGroupIds;
        set => ParentGroupsImpl.ListIds = value.Select(v => v.ToString()).JoinStrings(",");
    }

    public IntList ParentGroupsImpl { get; set; } = new IntList();

    public IEnumerable<CharacterGroup> ParentGroups
      => Project.CharacterGroups.Where(c => ParentCharacterGroupIds.Contains(c.CharacterGroupId));

    string IWorldObject.Name => CharacterGroupName;
    string ILinkableWithName.Name => CharacterGroupName;

    /// <summary>
    /// 
    /// </summary>
    /// <remarks>Check may be you need to use ordered groups</remarks>
    public virtual IEnumerable<CharacterGroup> ChildGroups
      => Project.CharacterGroups.Where(cg => cg.ParentCharacterGroupIds.Contains(CharacterGroupId));

    public bool IsPublic { get; set; }
    public bool IsSpecial { get; set; }

    public IEnumerable<Character> Characters => Project.Characters.Where(cg => cg.ParentCharacterGroupIds.Contains(CharacterGroupId));

    public bool IsActive { get; set; }

    public MarkdownString Description { get; set; } = new MarkdownString();

    // ReSharper disable once UnusedAutoPropertyAccessor.Global assigned by EF
    public virtual User? ResponsibleMasterUser { get; set; }

    public int? ResponsibleMasterUserId { get; set; }

    public string ChildCharactersOrdering { get; set; }
    public string ChildGroupsOrdering { get; set; }

    public virtual ICollection<PlotElement> DirectlyRelatedPlotElements { get; set; }

    // ReSharper disable once UnusedAutoPropertyAccessor.Global assigned by EF
    public virtual ICollection<UserSubscription> Subscriptions { get; set; }

    public bool CanBePermanentlyDeleted
      => !ChildGroups.Any() && !Characters.Any() && !DirectlyRelatedPlotElements.Any();

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
                yield return new ValidationResult($"Character group {CharacterGroupId} somehow is self-child. List of parents {string.Join(", ", ParentCharacterGroupIds)}");
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
