using System.ComponentModel;

namespace JoinRpg.Web.Models.Characters;

public class CharacterGroupReportItemViewModel : IEquatable<CharacterGroupReportItemViewModel>
{
    public int CharacterGroupId { get; set; }

    [DisplayName("Название группы ролей")]
    public string Name { get; set; }

    public int DeepLevel { get; set; }

    public int TotalSlots { get; set; }
    public int TotalFreeSlots { get; set; }
    public int TotalCharacters { get; set; }

    public int TotalNpcCharacters { get; set; }

    public int TotalCharactersWithPlayers { get; set; }

    public int TotalDiscussedClaims { get; set; }

    public int TotalActiveClaims { get; set; }

    public bool IsPublic { get; set; }

    public int TotalAcceptedClaims { get; set; }

    public bool Unlimited { get; set; }
    public int TotalCheckedInClaims { get; set; }
    public int TotalInGameCharacters { get; set; }

    public bool Equals(CharacterGroupReportItemViewModel? other) => other != null && other.CharacterGroupId == CharacterGroupId;

    public override bool Equals(object? obj) => Equals(obj as CharacterGroupReportItemViewModel);

    public override int GetHashCode() => CharacterGroupId;

    public override string ToString() => $"ChGroup(Name={Name})";
}

