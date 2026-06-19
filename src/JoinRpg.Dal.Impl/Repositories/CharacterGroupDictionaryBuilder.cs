using JoinRpg.Helpers;

namespace JoinRpg.Dal.Impl.Repositories;

public static class CharacterGroupDictionaryBuilder
{
    public static IReadOnlyDictionary<CharacterGroupIdentification, CharacterGroupInfo> Build(
        Project project,
        ProjectIdentification projectId)
    {
        var childGroupsMap = new Dictionary<int, List<CharacterGroupIdentification>>();
        var parentGroupsMap = new Dictionary<int, List<CharacterGroupIdentification>>();

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

                if (!parentGroupsMap.TryGetValue(group.CharacterGroupId, out List<CharacterGroupIdentification>? parentList))
                {
                    parentList = [];
                    parentGroupsMap[group.CharacterGroupId] = parentList;
                }
                parentList.Add(new CharacterGroupIdentification(projectId, parentId));
            }
        }

        // Отсортировать дочерние группы
        foreach (var group in project.CharacterGroups)
        {
            if (string.IsNullOrEmpty(group.ChildGroupsOrdering))
            {
                continue; // Не сохранено порядка, common case
            }
            if (!childGroupsMap.TryGetValue(group.CharacterGroupId, out var unsorted))
            {
                continue; // Нет дочерних групп, возможно были безвозвратно удалены
            }
            childGroupsMap[group.CharacterGroupId] = [.. unsorted.OrderByStoredOrder(k => k.Id, group.ChildGroupsOrdering)];
        }

        // Кэши для рекурсивных обходов
        var allChildrenCache = new Dictionary<int, List<CharacterGroupIdentification>>();
        var allParentsCache = new Dictionary<int, List<CharacterGroupIdentification>>();

        List<CharacterGroupIdentification> GetAllChildGroups(int groupId)
        {
            if (allChildrenCache.TryGetValue(groupId, out var cached))
            {
                return cached;
            }

            // Упорядоченный DFS (preorder): дочерние группы берём в порядке ChildGroupsOrdering
            // (childGroupsMap уже отсортирован выше), каждую группу — по первому вхождению.
            var result = new List<CharacterGroupIdentification>();
            var seen = new HashSet<CharacterGroupIdentification>();
            if (childGroupsMap.TryGetValue(groupId, out var directChildren))
            {
                foreach (var child in directChildren)
                {
                    if (seen.Add(child))
                    {
                        result.Add(child);
                    }
                    // Рекурсивно добавляем всех детей детей, сохраняя порядок обхода
                    foreach (var grandChild in GetAllChildGroups(child.CharacterGroupId))
                    {
                        if (seen.Add(grandChild))
                        {
                            result.Add(grandChild);
                        }
                    }
                }
            }

            allChildrenCache[groupId] = result;
            return result;
        }

        List<CharacterGroupIdentification> GetAllParentGroups(int groupId)
        {
            if (allParentsCache.TryGetValue(groupId, out var cached))
            {
                return cached;
            }

            var result = new HashSet<CharacterGroupIdentification>();
            if (parentGroupsMap.TryGetValue(groupId, out var directParents))
            {
                foreach (var parent in directParents)
                {
                    result.Add(parent);
                    // Рекурсивно добавляем всех родителей родителей
                    foreach (var grandParent in GetAllParentGroups(parent.CharacterGroupId))
                    {
                        result.Add(grandParent);
                    }
                }
            }

            var list = result.ToList();
            allParentsCache[groupId] = list;
            return list;
        }

        var rootGroupId = project.RootGroup.GetId();

        var dict = new Dictionary<CharacterGroupIdentification, CharacterGroupInfo>();
        foreach (var group in project.CharacterGroups)
        {
            var groupId = group.GetId();

            var allChildGroups = GetAllChildGroups(group.CharacterGroupId);
            var allParentGroups = GetAllParentGroups(group.CharacterGroupId);

            var groupInfo = new CharacterGroupInfo(
                Id: groupId,
                Name: group.CharacterGroupName,
                IsRoot: group.IsRoot,
                IsActive: group.IsActive,
                IsPublic: group.IsPublic,
                IsSpecial: group.IsSpecial,
                IsIntresting: !group.IsRoot && group.IsActive && (!group.IsSpecial || group.ParentCharacterGroupIds.Any(parentId => parentId != rootGroupId.Id)),
                DirectChildGroupIds: childGroupsMap.GetValueOrDefault(group.CharacterGroupId, []),
                ChildCharactersOrdering: group.ChildCharactersOrdering ?? "",
                DirectParentGroupIds: parentGroupsMap.GetValueOrDefault(group.CharacterGroupId, []),
                AllChildGroups: allChildGroups,
                AllParentGroups: allParentGroups
            );
            dict[groupId] = groupInfo;
        }

        return dict;
    }
}
