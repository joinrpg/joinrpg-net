using System;
using System.Collections.Generic;
using JoinRpg.DataModel;
using JoinRpg.Web.Models.CommonTypes;

namespace JoinRpg.Web.Models.Characters
{
  public class CharacterGroupLinkViewModel
  {
    public int CharacterGroupId { get; }
    public string Name { get; }
    public bool IsPublic { get; }
    public int ProjectId { get; }
    public bool IsActive { get; }

    public bool IsRoot { get; }

    public CharacterGroupLinkViewModel(CharacterGroup group)
    {
      CharacterGroupId = group.CharacterGroupId;
      Name = group.CharacterGroupName;
      IsPublic = group.IsPublic;
      ProjectId = group.ProjectId;
      IsActive = group.IsActive;
      IsRoot = group.IsRoot;
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

  public class CharacterTreeItem : CharacterGroupLinkViewModel, IEquatable<CharacterTreeItem>
  {
    public int DeepLevel { get; set; }

    public bool FirstCopy { get; set; }

    public IList<CharacterLinkViewModel> Characters { get; set; }

    public IEnumerable<CharacterTreeItem> ChildGroups { get; set; }

    public IEnumerable<CharacterTreeItem> Path { get; set; }

    public bool IsSpecial { get; set; }

    public bool Equals(CharacterTreeItem other) => other.CharacterGroupId == CharacterGroupId;

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

    public CharacterTreeItem(CharacterGroup arg) : base(arg)
    {
    }
  }

  public class CharacterLinkViewModel : IEquatable<CharacterLinkViewModel>
  {
    public int CharacterId { get; }
    public string CharacterName { get; }

    public bool IsFirstCopy { get; set; }

    public bool IsAvailable { get; }

    public bool IsPublic { get; }

    public bool IsActive { get; }

    public bool IsAcceptingClaims { get; set; }
    public int ProjectId { get; set; }

    public bool Equals(CharacterLinkViewModel other) => CharacterId == other.CharacterId;

    public override bool Equals(object obj)
    {
      var cg = obj as CharacterViewModel;
      return cg != null && Equals(cg);
    }

    public override int GetHashCode()
    {
      return CharacterId;
    }

    public override string ToString()
    {
      return $"Char(Name={CharacterName})";
    }

    public CharacterLinkViewModel(Character arg)
    {
      CharacterId = arg.CharacterId;
      CharacterName = arg.CharacterName;
      IsAvailable = arg.IsAvailable;
      IsPublic = arg.IsPublic;
      IsActive = arg.IsActive;
      ProjectId = arg.ProjectId;
      IsAcceptingClaims = arg.IsAcceptingClaims;

    }
  }
}