using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using JetBrains.Annotations;
using JoinRpg.DataModel;
using JoinRpg.Web.Models.CommonTypes;
using JoinRpg.Web.Models.Plot;

namespace JoinRpg.Web.Models
{
  
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
    [Display(Name = "Описание персонажа")]
    public MarkdownViewModel Description { get; set; }

    [ReadOnly(true), DisplayName("Входит в группы")]
    public CharacterParentGroupsViewModel ParentGroups { get; set; }

    public User Player { get; set; }

    public IEnumerable<PlotElementViewModel> Plot { get; set; }

    public bool HidePlayer { get; set; }
    public bool HasAccess { get;set; }

    public CustomFieldsViewModel Fields { get; set; }

    public CharacterNavigationViewModel Navigation { get; set; }
  }
}
