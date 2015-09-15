using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using JoinRpg.DataModel;
using JoinRpg.Domain;

namespace JoinRpg.Web.Models
{
  public interface ICharacterFieldsViewModel
  {
    [Display(Name = "Имя персонажа"),Required]
    string CharacterName { get; set; }
    bool HasPlayerAccessToCharacter { get; set; }
    bool HasMasterAccess { get; set; }
    IEnumerable<CharacterFieldValue> CharacterFields { get; set; }
    [Display(Name = "Описание персонажа")]
    MarkdownString Description { get; set; }
  }

  public class CharacterDetailsViewModel : ICharacterFieldsViewModel
  {
    public string ProjectName { get; set; }

    public int ProjectId { get; set; }
    public int CharacterId { get; set;}
    [Display(Name="Имя персонажа")]
    public string CharacterName { get; set; }

    [Display(Name = "Описание персонажа")]
    public MarkdownString Description { get; set; }

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
