using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using JetBrains.Annotations;
using Joinrpg.Markdown;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Web.Models.Plot;

namespace JoinRpg.Web.Models.Characters
{
  
  public class CharacterParentGroupsViewModel
  {
    public bool HasMasterAccess { get; private set; }
    [ReadOnly(true), DisplayName("Входит в группы")]
    public IEnumerable<CharacterGroupLinkViewModel> ParentGroups { get; private set; }

    public CharacterParentGroupsViewModel([NotNull] Character character, bool hasMasterAccess)
    {
      if (character == null) throw new ArgumentNullException(nameof(character));
      HasMasterAccess = hasMasterAccess;
      //TODO: Remove special groups from here
      ParentGroups = character.Groups.Select(g => new CharacterGroupLinkViewModel(g)).ToArray();
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
    [Display(Name = "Описание персонажа")]
    public IHtmlString Description { get; }

    [ReadOnly(true), DisplayName("Входит в группы")]
    public CharacterParentGroupsViewModel ParentGroups { get; }

    public User Player { get; }

    public IEnumerable<PlotElementViewModel> Plot { get; }

    public bool HidePlayer { get; }
    public bool HasAccess { get; }

    public CustomFieldsViewModel Fields { get; }

    public CharacterNavigationViewModel Navigation { get; }

    public CharacterDetailsViewModel (int? currentUserIdOrDefault, Character character, IEnumerable<PlotElement> plots)
    {
      Description = character.Description.ToHtmlString();
      Player = character.ApprovedClaim?.Player;
      HasAccess = character.HasAnyAccess(currentUserIdOrDefault);
      ParentGroups = new CharacterParentGroupsViewModel(character, character.HasMasterAccess(currentUserIdOrDefault));
      HidePlayer = character.HidePlayerForCharacter;
      Navigation =
        CharacterNavigationViewModel.FromCharacter(character, CharacterNavigationPage.Character,
          currentUserIdOrDefault);
      Fields = new CustomFieldsViewModel(currentUserIdOrDefault, character, disableEdit: true);
      Plot = plots.ToViewModels(character.HasMasterAccess(currentUserIdOrDefault), character.CharacterId);
    }
  }
}
