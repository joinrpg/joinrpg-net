using System.Collections.Generic;
using System.Linq;

namespace JoinRpg.DataModel
{
  // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global used by LINQ
  public class Character : IClaimSource, IDeletableSubEntity
  {
    public int CharacterId { get; set; }
    public int ProjectId { get; set; }
    int IProjectSubEntity.Id => CharacterId;
    ICollection<CharacterGroup> IWorldObject.ParentGroups => Groups;

    public virtual Project Project { get; set; }

    public virtual ICollection<CharacterGroup> Groups { get; set; }

    public string CharacterName { get; set; }

    string IWorldObject.Name => CharacterName;

    public bool IsPublic { get; set; }

    public bool IsAcceptingClaims { get; set; }

    /// <summary>
    /// Contains values of fields for this character
    /// </summary>
    public string JsonData { get; set; }

    public bool CanBePermanentlyDeleted => !Claims.Any();

    public bool IsActive { get; set; }

    public MarkdownString Description { get; set; } = new MarkdownString();

    public virtual ICollection<Claim> Claims { get; set; }

    public Claim ApprovedClaim => Claims.SingleOrDefault(c => c.IsApproved);

    public virtual ICollection<UserSubscription> Subscriptions { get; set; }

    /// <summary>
    /// Is available for claims (IsAcceptingClaims + has no approved claim)
    /// </summary>
    public bool IsAvailable => IsAcceptingClaims && ApprovedClaim == null;

    public User ResponsibleMasterUser => null; // We don't implement yet of setting responsible masters for indv. characters. I think the group will be enough now

    //TODO: Implement plot element order. Save here data like "{12,13,158,46}" where numbers is PlotElementIds
    public string PlotElementOrderData { get; set; }

    public virtual ICollection<PlotElement> DirectlyRelatedPlotElements { get; set; }
  }

  
}
