using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;
using JetBrains.Annotations;
using JoinRpg.DataModel;
using JoinRpg.Web.Models.CommonTypes;

namespace JoinRpg.Web.Models
{
  public class CharacterGroupLinkViewModel
  {
    public int CharacterGroupId { get; }
    public string Name { get; }
    public bool IsPublic { get; }
    public int ProjectId { get; }
    public bool IsActive { get; }

    public CharacterGroupLinkViewModel(CharacterGroup group)
    {
      CharacterGroupId = group.CharacterGroupId;
      Name = group.CharacterGroupName;
      IsPublic = group.IsPublic;
      ProjectId = group.ProjectId;
      IsActive = group.IsActive;
    }
  }

  public class CharacterGroupWithDescViewModel : CharacterGroupLinkViewModel
  {
    public MarkdownViewModel Description { get; }
    public CharacterGroupWithDescViewModel(CharacterGroup group) : base(group)
    {
      Description = new MarkdownViewModel(group.Description);
    }
  }

  public class CharacterGroupListItemViewModel : IEquatable<CharacterGroupListItemViewModel>
  {
    public int RootGroupId
    { get; set; }
    public int CharacterGroupId { get; set; }

    [DisplayName("Название группы ролей")]
    public string Name { get; set; }

    public int DeepLevel { get; set; }

    public bool FirstCopy { get; set; }

    [DisplayName("Слотов для заявок в группу")]
    public int AvaiableDirectSlots { get; set; }

    public IList<CharacterViewModel> Characters { get; set; }

    public IEnumerable<CharacterViewModel> ActiveCharacters => Characters.Where(c => c.IsActive);

    public IEnumerable<CharacterViewModel> PublicCharacters => Characters.Where(c => c.IsActive && c.IsPublic);

    public IEnumerable<CharacterGroupListItemViewModel> ChildGroups { get; set; }

    public int TotalSlots { get; set; }
    public int TotalCharacters { get; set; }

    public int TotalNpcCharacters { get; set; }

    public int TotalCharactersWithPlayers { get; set; }

    public int TotalDiscussedClaims { get; set; }

    public int TotalActiveClaims { get; set; }

    [CanBeNull]
    public HtmlString Description { get; set; }

    public IEnumerable<CharacterGroupListItemViewModel> Path { get; set; }

    public bool IsRoot => DeepLevel == 0;

    public bool IsRootGroup { get; set; }
    public bool IsActive { get; set; }
    public bool IsPublic { get; set; }

    public bool IsSpecial { get; set; }

    public string BoundExpression { get; set; } = "";

    public int ActiveClaimsCount { get; set; }

    public bool FirstInGroup { get; set; }
    public bool LastInGroup { get; set; }

    public int ProjectId { get; set; }

    public bool IsAcceptingClaims { get; set; }

    public int TotalAcceptedClaims { get; set; }

    public bool Unlimited  { get; set; }

    public CharacterGroupListItemViewModel()
    {
      
    }

    public bool Equals(CharacterGroupListItemViewModel other) => other.CharacterGroupId == CharacterGroupId;

    public override bool Equals(object obj)
    {
      var cg = obj as CharacterGroupListItemViewModel;
      return cg != null && Equals(cg);
    }

    public override int GetHashCode()
    {
      return CharacterGroupId;
    }

    public override string ToString()
    {
      return $"ChGroup(Name={Name})";
    }
  }

}
