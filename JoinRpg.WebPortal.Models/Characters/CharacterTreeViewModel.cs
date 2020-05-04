using System;
using System.Collections.Generic;
using Joinrpg.Markdown;
using JoinRpg.DataModel;
using JoinRpg.Helpers.Web;

namespace JoinRpg.Web.Models.Characters
{
    public class CharacterGroupLinkViewModel : ILinkable
    {
        public int CharacterGroupId { get; }
        public string Name { get; }
        public bool IsPublic { get; }
        public LinkType LinkType => LinkType.ResultCharacterGroup;
        public string Identification => CharacterGroupId.ToString();
        int? ILinkable.ProjectId => ProjectId;

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
        public JoinHtmlString Description { get; }


        public CharacterGroupWithDescViewModel(CharacterGroup group) : base(group)
        {
            Description = group.Description.ToHtmlString();
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

        public bool Equals(CharacterTreeItem other) => other != null && other.CharacterGroupId == CharacterGroupId;

        public override bool Equals(object obj) => Equals(obj as CharacterTreeItem);

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

        public bool Equals(CharacterLinkViewModel other) => other != null && CharacterId == other.CharacterId;

        public override bool Equals(object obj) => Equals(obj as CharacterLinkViewModel);

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
