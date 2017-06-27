﻿using System;
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
    public bool HasMasterAccess { get; }
    [ReadOnly(true), DisplayName("Входит в группы")]
    public IReadOnlyCollection<CharacterGroupLinkViewModel> ParentGroups { get; }

    public CharacterParentGroupsViewModel([NotNull] Character character, bool hasMasterAccess)
    {
      if (character == null) throw new ArgumentNullException(nameof(character));
      HasMasterAccess = hasMasterAccess;
      ParentGroups = character
        .GetParentGroupsToTop()
        .Where(group => !group.IsRoot && !group.IsSpecial)
        .Select(g => new CharacterGroupLinkViewModel(g)).ToList();
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

    public PlotDisplayViewModel Plot { get; }

    public bool HidePlayer { get; }
    public bool HasAccess { get; }

    public CustomFieldsViewModel Fields { get; }

    public CharacterNavigationViewModel Navigation { get; }

    public CharacterDetailsViewModel (int? currentUserIdOrDefault, Character character, IReadOnlyCollection<PlotElement> plots)
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
      Plot = PlotDisplayViewModel.Published(plots, currentUserIdOrDefault, character);
    }
  }
}
