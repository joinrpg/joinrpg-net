using JoinRpg.DataModel;

namespace JoinRpg.Web.Models.Characters;

public class CharacterTreeItem : CharacterGroupLinkViewModel, IEquatable<CharacterTreeItem>
{
    public required int DeepLevel { get; set; }

    public required bool FirstCopy { get; set; }

    public required IList<CharacterLinkViewModel> Characters { get; set; }

    public required IEnumerable<CharacterTreeItem> ChildGroups { get; set; }

    public required IEnumerable<CharacterTreeItem> Path { get; set; }

    public required bool IsSpecial { get; set; }

    public bool Equals(CharacterTreeItem? other) => other != null && other.CharacterGroupId == CharacterGroupId;

    public override bool Equals(object? obj) => Equals(obj as CharacterTreeItem);

    public override int GetHashCode() => CharacterGroupId;

    public override string ToString() => $"ChGroup(Name={Name})";

    public CharacterTreeItem(CharacterGroup arg) : base(arg)
    {
    }
}
