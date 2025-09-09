
using JoinRpg.Helpers;

namespace JoinRpg.Domain;
public class CharacterBulkLoader
{
    private readonly Dictionary<CharacterGroupIdentification, CharacterGroupIdentification[]> parentGroupsCache = [];
    private readonly Dictionary<int, CharacterItem> characterCache = [];

    public CharacterItem LoadCharacter(Character character)
    {
        if (characterCache.TryGetValue(character.CharacterId, out var item))
        {
            return item;
        }
        var directParents = character.ParentCharacterGroupIds.Select(c => new CharacterGroupIdentification(character.ProjectId, c)).ToList();
        var allParents = directParents.SelectMany(g => ResolveGroupsToTop(character.Project, g)).ToList();
        var result = new CharacterItem(character, allParents);
        characterCache.Add(character.CharacterId, result);
        return result;
    }

    private CharacterGroupIdentification[] ResolveGroupsToTop(Project project, CharacterGroupIdentification groupId)
    {
        if (parentGroupsCache.TryGetValue(groupId, out var groups))
        {
            return groups;
        }
        var entity = project.CharacterGroups.Single(g => g.CharacterGroupId == groupId.CharacterGroupId);
        var all = entity.FlatTree(e => e.ParentGroups);
        var value = all.Select(e => new CharacterGroupIdentification(e.ProjectId, e.CharacterGroupId)).ToArray();
        parentGroupsCache.Add(groupId, value);
        return value;
    }
}

public record class CharacterItem(Character Character, IReadOnlyCollection<CharacterGroupIdentification> ParentGroups);
