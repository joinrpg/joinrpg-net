using System.Collections.Generic;
using System.Web;
using JoinRpg.DataModel;
using JoinRpg.Domain;

namespace JoinRpg.Web.Models
{
  public interface ICharacterFieldsViewModel
  {
    bool HasPlayerAccessToCharacter { get; set; }
    bool HasMasterAccess { get; set; }
    IEnumerable<CharacterFieldValue> CharacterFields { get; set; }
  }

  public class CharacterDetailsViewModel : ICharacterFieldsViewModel
  {
    public string ProjectName { get; set; }

    public int ProjectId { get; set; }
    public int CharacterId { get; set;}

    public string CharacterName { get; set; }

    public HtmlString Description { get; set; }

    public bool CanAddClaim { get; set; }

    public User ApprovedClaimUser { get; set; }

    public int? ApprovedClaimId { get; set; }

    public bool HasPlayerAccessToCharacter { get; set; }

    public bool HasMasterAccess { get; set; }

    public IEnumerable<CharacterFieldValue> CharacterFields { get; set; }

    public IEnumerable<Claim> DiscussedClaims { get; set; }

    public IEnumerable<Claim> RejectedClaims { get; set; }

  }
}
