using JoinRpg.DataModel;

namespace JoinRpg.Web.Models.Characters;

public class CharacterLinkViewModel : IEquatable<CharacterLinkViewModel>
{
    public int CharacterId { get; }
    public string CharacterName { get; }

    public bool IsAvailable { get; }

    public bool Equals(CharacterLinkViewModel? other) => other != null && CharacterId == other.CharacterId;

    public override bool Equals(object? obj) => Equals(obj as CharacterLinkViewModel);

    public override int GetHashCode() => CharacterId;

    public override string ToString() => $"Char(Name={CharacterName})";

    public CharacterLinkViewModel(Character arg)
    {
        CharacterId = arg.CharacterId;
        CharacterName = arg.CharacterName;
        IsAvailable = arg.IsAvailable;
    }
}
