using System.Collections.Generic;
using System.Linq;

namespace JoinRpg.DataModel
{
  public class Character : IClaimSource
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

    public bool IsActive { get; set; }

    public MarkdownString Description { get; set; } = new MarkdownString();

    public virtual ICollection<Claim> Claims { get; set; }

    public Claim ApprovedClaim => Claims.SingleOrDefault(c => c.IsApproved);

    public bool IsAvailable => IsAcceptingClaims && ApprovedClaim == null;
  }

  
}
