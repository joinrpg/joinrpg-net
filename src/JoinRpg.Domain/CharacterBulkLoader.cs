using JoinRpg.DomainTypes.Characters;

namespace JoinRpg.Domain;

public class CharacterBulkLoader
{
    private readonly Dictionary<int, CharacterItem> characterCache = [];

    public CharacterItem LoadCharacter(Character character, ProjectInfo projectInfo)
    {
        if (characterCache.TryGetValue(character.CharacterId, out var item))
        {
            return item;
        }
        var result = new CharacterItem(character, [.. character.GetParentGroupIdsToTop(projectInfo)]);
        characterCache.Add(character.CharacterId, result);
        return result;
    }
}

/// <summary>
/// Обёртка над EF-сущностью для проверок доступности поля. Временная: существует, пока проверки
/// проблем не переехали на <see cref="CharacterInfo"/> (ADR013), который реализует тот же интерфейс.
/// </summary>
public record class CharacterItem(Character Character, IReadOnlyCollection<CharacterGroupIdentification> ParentGroups)
    : IFieldAvailabilityTarget
{
    CharacterType IFieldAvailabilityTarget.CharacterType => Character.CharacterType;

    IReadOnlyCollection<CharacterGroupIdentification> IFieldAvailabilityTarget.ParentGroupIdsToTop => ParentGroups;
}
