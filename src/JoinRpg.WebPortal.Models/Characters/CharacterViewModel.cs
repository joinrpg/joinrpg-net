using System;
using JoinRpg.DataModel;
using JoinRpg.Helpers.Web;
using JoinRpg.Web.Models.UserProfile;

namespace JoinRpg.Web.Models.Characters
{
    public class CharacterViewModel :
        ICharacterWithPlayerViewModel,
        IEquatable<CharacterViewModel>,
        ILinkable
    {
        public int CharacterId { get; set; }
        public int ProjectId { get; set; }
        public string CharacterName { get; set; }

        public bool IsFirstCopy { get; set; }

        public bool IsAvailable { get; set; }

        public JoinHtmlString Description { get; set; }

        public bool IsPublic { get; set; }

        public bool IsActive { get; set; }

        public UserLinkViewModel? PlayerLink { get; set; }
        public bool HidePlayer { get; set; }
        public bool HasAccess => HasMasterAccess;
        public int ActiveClaimsCount { get; set; }

        public bool HasMasterAccess { get; set; }

        public bool FirstInGroup { get; set; }
        public bool LastInGroup { get; set; }

        public int ParentCharacterGroupId { get; set; }
        public int RootGroupId { get; set; }

        public bool IsHot { get; set; }

        public bool IsAcceptingClaims { get; set; }
        public bool HasEditRolesAccess { get; set; }

        LinkType ILinkable.LinkType => LinkType.ResultCharacter;

        string ILinkable.Identification => CharacterId.ToString();

        int? ILinkable.ProjectId => ProjectId;

        public bool Equals(CharacterViewModel? other) => other != null && CharacterId == other.CharacterId;

        public override bool Equals(object? obj) => Equals(obj as CharacterViewModel);

        public override int GetHashCode() => CharacterId;

        public override string ToString() => $"Char(Name={CharacterName})";
    }
}
