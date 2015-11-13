using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Web.Models.Plot;

namespace JoinRpg.Web.Models
{
  public interface ICharacterFieldsViewModel : ICharacterWithPlayerViewModel
  {
    [Display(Name = "Имя персонажа"),Required]
    string CharacterName { get; set; }
    bool HasPlayerAccessToCharacter { get; set; }
    bool HasMasterAccess { get; set; }
    IEnumerable<CharacterFieldValue> CharacterFields { get; set; }
    [Display(Name = "Описание персонажа")]
    MarkdownString Description { get; set; }
    [ReadOnly(true), DisplayName("Входит в группы")]
    IEnumerable<ICharacterGroupLinkViewModel> ParentGroups { get; }
    int ProjectId { get; }
    int? CharacterId { get; }
  }

  public interface ICharacterWithPlayerViewModel
  {
    User Player { get; }
    bool HidePlayer { get; }
    bool HasAccess { get; }
  }

  public class CharacterDetailsViewModel : ICharacterFieldsViewModel
  {
    public string ProjectName { get; set; }

    public int ProjectId { get; set; }
    public int CharacterId { get; set;}
    int? ICharacterFieldsViewModel.CharacterId => CharacterId;
    [Display(Name="Имя персонажа")]
    public string CharacterName { get; set; }

    [Display(Name = "Описание персонажа")]
    public MarkdownString Description { get; set; }

    public IEnumerable<ICharacterGroupLinkViewModel> ParentGroups { get; set; }

    public bool CanAddClaim { get; set; }

    public User Player { get; set; }

    public int? ApprovedClaimId { get; set; }

    public bool HasPlayerAccessToCharacter { get; set; }

    public bool HasMasterAccess { get; set; }

    public IEnumerable<CharacterFieldValue> CharacterFields { get; set; }

    public IEnumerable<ClaimListItemViewModel> DiscussedClaims { get; set; }

    public IEnumerable<ClaimListItemViewModel> RejectedClaims { get; set; }

    public IEnumerable<PlotElementViewModel> Plot { get; set; }

    public bool HidePlayer { get; set; }
    public bool HasAccess => HasPlayerAccessToCharacter || HasMasterAccess;
  }
}
