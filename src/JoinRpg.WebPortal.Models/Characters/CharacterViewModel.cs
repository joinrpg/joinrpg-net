using JoinRpg.DomainTypes.Interfaces;
using JoinRpg.Web.ProjectCommon;
using JoinRpg.WebComponents;

namespace JoinRpg.Web.Models.Characters;

public class CharacterViewModel :
    IEquatable<CharacterViewModel>,
    ILinkable
{
    public int CharacterId { get; set; }
    public int ProjectId { get; set; }
    public required string CharacterName { get; set; }

    public bool IsFirstCopy { get; set; }

    public required CharacterApplyViewModel ApplyStatus { get; init; }

    public bool IsAvailable => ApplyStatus.BusyStatus is CharacterBusyStatusView.Vacancy or CharacterBusyStatusView.HotVacancy
        || (ApplyStatus.IsSlot && ApplyStatus.SlotCount is > 0);

    public required JoinHtmlString Description { get; set; }

    public bool IsPublic { get; set; }

    public bool IsActive { get; set; }

    public UserLinkViewModel? PlayerLink { get; set; }
    public int ActiveClaimsCount { get; set; }

    public bool FirstInGroup { get; set; }
    public bool LastInGroup { get; set; }

    public int ParentCharacterGroupId { get; set; }
    public int RootGroupId { get; set; }

    public bool HasEditRolesAccess { get; set; }

    LinkType ILinkable.LinkType => LinkType.ResultCharacter;

    string ILinkable.Identification => CharacterId.ToString();

    int? ILinkable.ProjectId => ProjectId;

    public bool Equals(CharacterViewModel? other) => other != null && CharacterId == other.CharacterId;

    public override bool Equals(object? obj) => Equals(obj as CharacterViewModel);

    public override int GetHashCode() => CharacterId;

    public override string ToString() => $"Char(Name={CharacterName})";
}
