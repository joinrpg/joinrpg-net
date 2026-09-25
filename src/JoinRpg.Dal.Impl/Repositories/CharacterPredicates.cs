using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Interfaces;

namespace JoinRpg.Dal.Impl.Repositories;

internal static class CharacterPredicates
{
    /// <summary>
    /// Зеркало <see cref="CharacterStatusSpecExtensions.Matches"/> для запроса — при изменении
    /// правила править оба места (согласованность закреплена тестом).
    /// </summary>
    internal static Expression<Func<Character, bool>> ByStatus(CharacterStatusSpec spec)
        => spec switch
        {
            CharacterStatusSpec.Any => character => true,
            CharacterStatusSpec.Active => character => character.IsActive,
            CharacterStatusSpec.Deleted => character => !character.IsActive,
            _ => throw new ArgumentOutOfRangeException(nameof(spec), spec, null),
        };

    internal static Expression<Func<Character, bool>> Hot() => character => character.IsHot && character.IsActive && character.ApprovedClaim == null;


    internal static Expression<Func<Character, bool>> ByGroup(IReadOnlyCollection<CharacterGroupIdentification> groups)
    {
        if (groups.Count == 0)
        {
            return character => false;
        }

        var projectId = groups.EnsureSameProject().First().ProjectId;
        var groupIntIds = groups.Select(x => x.CharacterGroupId).ToArray();

        return character => character.ProjectId == projectId
            && groupIntIds.Any(id => ("," + character.ParentGroupsImpl.ListIds + ",").Contains("," + id.ToString() + ","));
    }
}
