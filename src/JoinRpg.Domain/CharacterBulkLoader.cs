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
/// Обёртка над EF-сущностью для проверок доступности поля. Временная: нужна только проблемам
/// заявки, которые ещё считаются по EF-графу. Проблемы персонажа уже считаются по
/// <see cref="CharacterInfo"/> (ADR013) — он реализует тот же интерфейс сам, без обёртки.
/// </summary>
public record class CharacterItem(Character Character, IReadOnlyCollection<CharacterGroupIdentification> ParentGroups)
    : IFieldAvailabilityTarget
{
    CharacterType IFieldAvailabilityTarget.CharacterType => Character.CharacterType;

    IReadOnlyCollection<CharacterGroupIdentification> IFieldAvailabilityTarget.ParentGroupIdsToTop => ParentGroups;
}
