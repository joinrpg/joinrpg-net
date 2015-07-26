using System.Collections.Generic;
using System.ComponentModel;
using JoinRpg.DataModel;

namespace JoinRpg.Web.Models
{
  public class ClaimViewModel
  {
    public int ClaimId { get; set; }
    public int ProjectId { get; set; }
    public string ClaimName { get; set; }
    public User Player { get; set; }
    public string ProjectName { get; set; }
    public Claim.Status Status { get; set; }
    public bool IsMyClaim { get; set; }
    public bool HasMasterAccess { get; set; }
    public IEnumerable<Comment> Comments { get; set; }
    [DisplayName("Заявка на персонажа")]
    public string CharacterName { get; set; } 
    public int? CharacterId { get; set; }
    [DisplayName("Заявка в локацию")]
    public string GroupName { get; set; }
    public int? CharacterGroupId { get; set; }
    public int OtherClaimsForThisCharacterCount { get; set; }
    public int OtherClaimsFromThisPlayerCount
    { get; set; }
  }
}
