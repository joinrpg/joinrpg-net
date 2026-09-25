namespace JoinRpg.Web.Models.Characters;

public class CharacterGroupListItemViewModel : IEquatable<CharacterGroupListItemViewModel>
{
    public int CharacterGroupId { get; set; }

    [DisplayName("Название группы ролей")]
    public string Name { get; set; }

    public int DeepLevel { get; set; }

    public bool FirstCopy { get; set; }

    public ICollection<CharacterViewModel> ActiveCharacters { get; set; }

    public IEnumerable<CharacterViewModel> PublicCharacters => ActiveCharacters.Where(c => c.IsPublic);

    public JoinHtmlString Description { get; set; }

    public IEnumerable<CharacterGroupListItemViewModel> Path { get; set; }

    public bool Equals(CharacterGroupListItemViewModel? other) => other != null && other.CharacterGroupId == CharacterGroupId;

    public override bool Equals(object? obj) => Equals(obj as CharacterGroupListItemViewModel);

    public override int GetHashCode() => CharacterGroupId;

    public override string ToString() => $"ChGroup(Name={Name})";
}

