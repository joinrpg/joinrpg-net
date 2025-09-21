using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.PrimitiveTypes.ProjectMetadata;

namespace JoinRpg.Web.Models.Characters;

public class CharacterLinkViewModel(Character arg, ProjectInfo projectInfo) : IEquatable<CharacterLinkViewModel>
{
    public int CharacterId { get; } = arg.CharacterId;
    public string CharacterName { get; } = arg.CharacterName;

    public bool IsAvailable { get; } = arg.IsAcceptingClaims(projectInfo);

    public bool Equals(CharacterLinkViewModel? other) => other != null && CharacterId == other.CharacterId;

    public override bool Equals(object? obj) => Equals(obj as CharacterLinkViewModel);

    public override int GetHashCode() => CharacterId;

    public override string ToString() => $"Char(Name={CharacterName})";
}
