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

public record class CharacterItem(Character Character, IReadOnlyCollection<CharacterGroupIdentification> ParentGroups);
