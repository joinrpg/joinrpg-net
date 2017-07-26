using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;
using JoinRpg.Web.Models.CommonTypes;

namespace JoinRpg.Web.Models.Characters
{
  public class CharacterGroupListItemViewModel : IEquatable<CharacterGroupListItemViewModel>, IMovableListItem
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
    
    public ICollection<CharacterViewModel> ActiveCharacters { get; set; }

    public IEnumerable<CharacterViewModel> PublicCharacters => ActiveCharacters.Where(c =>  c.IsPublic);

    public IEnumerable<CharacterGroupListItemViewModel> ChildGroups { get; set; }

    public int TotalActiveClaims { get; set; }

    public IHtmlString Description { get; set; }

    public IEnumerable<CharacterGroupListItemViewModel> Path { get; set; }

    public bool IsRoot => DeepLevel == 0;

    public bool IsRootGroup { get; set; }

    public bool IsPublic { get; set; }

    public bool IsSpecial { get; set; }

    public string BoundExpression { get; set; } = "";

    public int ActiveClaimsCount { get; set; }

    public bool First { get; set; }
    public bool Last { get; set; }

    public int ProjectId { get; set; }

    public bool IsAcceptingClaims { get; set; }

    public bool Equals(CharacterGroupListItemViewModel other) => other != null && other.CharacterGroupId == CharacterGroupId;

    public override bool Equals(object obj) => Equals(obj as CharacterGroupListItemViewModel);

    public override int GetHashCode()
    {
      return CharacterGroupId;
    }

    public override string ToString()
    {
      return $"ChGroup(Name={Name})";
    }

    int IMovableListItem.ItemId => CharacterGroupId;
  }

}
