namespace JoinRpg.Dal.Impl.Repositories;

public static class CharacterGroupDictionaryBuilder
{
    public static IReadOnlyDictionary<CharacterGroupIdentification, CharacterGroupInfo> Build(
        Project project,
        ProjectIdentification projectId)
    {
        var childGroupsMap = new Dictionary<int, List<CharacterGroupIdentification>>();
        foreach (var group in project.CharacterGroups)
        {
            foreach (var parentId in group.ParentCharacterGroupIds)
            {
                if (!childGroupsMap.TryGetValue(parentId, out List<CharacterGroupIdentification>? value))
                {
                    value = [];
                    childGroupsMap[parentId] = value;
                }

                value.Add(group.GetId());
            }
        }

        var dict = new Dictionary<CharacterGroupIdentification, CharacterGroupInfo>();
        foreach (var group in project.CharacterGroups)
        {
            var groupId = group.GetId();

            var groupInfo = new CharacterGroupInfo(
                Id: groupId,
                Name: group.CharacterGroupName,
                IsRoot: group.IsRoot,
                IsActive: group.IsActive,
                IsPublic: group.IsPublic,
                IsSpecial: group.IsSpecial,
                ChildGroupIds: childGroupsMap.GetValueOrDefault(group.CharacterGroupId, []),
                ParentGroupIds: group.ParentCharacterGroupIds.Select(id => new CharacterGroupIdentification(projectId, id)).ToList()
            );
            dict[groupId] = groupInfo;
        }

        return dict;
    }
}
