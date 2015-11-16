using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using JetBrains.Annotations;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Web.Models.CommonTypes;
using JoinRpg.Web.Models.Plot;

namespace JoinRpg.Web.Models
{
  public class CharacterFieldsViewModel
  {
    public bool HasPlayerAccessToCharacter { get; set; }
    public bool HasMasterAccess { get; set; }
    public bool EditAllowed { get; set; }
    public IEnumerable<CharacterFieldValue> CharacterFields { get; set; }

    public bool AnyFieldEditable
      => EditAllowed && (HasMasterAccess || CharacterFields.Any(field => field.Field.CanPlayerEdit));
  }

  public class CharacterParentGroupsViewModel
  {
    public bool HasMasterAccess { get; private set; }
    [ReadOnly(true), DisplayName("Входит в группы")]
    public IEnumerable<ICharacterGroupLinkViewModel> ParentGroups { get; private set; }

    public static CharacterParentGroupsViewModel FromCharacter([NotNull] Character character, bool hasMasterAccess)
    {
      if (character == null) throw new ArgumentNullException(nameof(character));
      return new CharacterParentGroupsViewModel()
      {
        HasMasterAccess = hasMasterAccess,
        ParentGroups = character.Groups.Select(g => new CharacterGroupLinkViewModel(g)).ToList()
      };
    }
  }

  public interface ICharacterWithPlayerViewModel
  {
    User Player { get; }
    bool HidePlayer { get; }
    bool HasAccess { get; }
  }

  public class CharacterDetailsViewModel : ICharacterWithPlayerViewModel
  {

    public int ProjectId { get; set; }
    public int CharacterId { get; set;}

    [Display(Name="Имя персонажа")]
    public string CharacterName { get; set; }

    [Display(Name = "Описание персонажа")]
    public MarkdownViewModel Description { get; set; }

    [ReadOnly(true), DisplayName("Входит в группы")]
    public CharacterParentGroupsViewModel ParentGroups { get; set; }

    public bool CanAddClaim { get; set; }

    public User Player { get; set; }

    public int? ApprovedClaimId { get; set; }

    public bool HasMasterAccess { get; set; }

    public IEnumerable<ClaimListItemViewModel> DiscussedClaims { get; set; }

    public IEnumerable<ClaimListItemViewModel> RejectedClaims { get; set; }

    public IEnumerable<PlotElementViewModel> Plot { get; set; }

    public bool HidePlayer { get; set; }
    public bool HasAccess { get;set; }

    public CharacterFieldsViewModel Fields { get; set; }
  }
}
