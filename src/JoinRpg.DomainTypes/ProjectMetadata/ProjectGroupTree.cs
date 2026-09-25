namespace JoinRpg.DomainTypes.ProjectMetadata;

/// <summary>
/// Дерево групп персонажей проекта: сами группы, их топология (родители/дети) и всё, что
/// из этой топологии выводится.
/// </summary>
/// <remarks>
/// Тип неизменяемый: дерево целиком собирается при загрузке метаданных проекта
/// (см. <c>CharacterGroupDictionaryBuilder</c>) и дальше только читается.
/// </remarks>
public sealed class ProjectGroupTree
{
    public ProjectGroupTree(
        CharacterGroupIdentification rootGroupId,
        IReadOnlyDictionary<CharacterGroupIdentification, CharacterGroupInfo> groups)
    {
        GroupsDictionary = groups;
        RootGroupId = rootGroupId;
        AllGroups = [.. groups.Values];

        ResponsibleMasterRules = [.. groups.Values
            .Where(g => g.ResponsibleMasterId != null)
            .OrderByDescending(g => g, Comparer<CharacterGroupInfo>.Create((x, y) =>
            {
                // Более вложенная группа — более специфичное правило, она должна идти раньше.
                if (x.AllParentGroups.Contains(y.Id))
                {
                    return 1;
                }

                if (y.AllParentGroups.Contains(x.Id))
                {
                    return -1;
                }

                return x.Id.CharacterGroupId.CompareTo(y.Id.CharacterGroupId);
            }))];

        AllowToSetGroups = groups.Values.Any(g => g.IsActive && !g.IsRoot && !g.IsSpecial);
    }

    /// <summary>Корневая группа проекта.</summary>
    public CharacterGroupIdentification RootGroupId { get; }

    /// <summary>Все группы проекта, включая корневую, спецгруппы и неактивные.</summary>
    public IReadOnlyCollection<CharacterGroupInfo> AllGroups { get; }

    /// <summary>
    /// Группы с назначенным ответственным мастером, от самых вложенных к самым верхним:
    /// первое подошедшее правило и определяет ответственного мастера.
    /// </summary>
    public IReadOnlyList<CharacterGroupInfo> ResponsibleMasterRules { get; }

    /// <summary>Есть ли в проекте хоть одна живая обычная группа, в которую можно положить персонажа.</summary>
    public bool AllowToSetGroups { get; }

    internal IReadOnlyDictionary<CharacterGroupIdentification, CharacterGroupInfo> GroupsDictionary { get; }

    public bool Contains(CharacterGroupIdentification id) => GroupsDictionary.ContainsKey(id);

    public CharacterGroupInfo GetGroupById(CharacterGroupIdentification id) => GroupsDictionary[id];

    /// <summary>Группа проекта или <c>null</c>, если такой группы нет.</summary>
    public CharacterGroupInfo? GetGroupByIdOrDefault(CharacterGroupIdentification id) => GroupsDictionary.GetValueOrDefault(id);

    public IEnumerable<CharacterGroupInfo> GetGroupsById(IReadOnlyCollection<CharacterGroupIdentification> ids)
    {
        return GroupsDictionary.Where(g => ids.Contains(g.Key)).Select(g => g.Value);
    }

    public IReadOnlyList<CharacterGroupIdentification> GetChildGroupIdsIncludingThis(CharacterGroupIdentification groupId)
    {
        return GroupsDictionary[groupId].AllChildGroupsIncludingThis;
    }

    public IReadOnlyList<CharacterGroupIdentification> GetChildGroupIdsIncludingThis(IEnumerable<CharacterGroupIdentification> groupIds)
    {
        return [.. groupIds.SelectMany(x => GetChildGroupIdsIncludingThis(x)).Distinct()];
    }

    public IEnumerable<CharacterGroupIdentification> GetParentGroupIdsIncludingThis(CharacterGroupIdentification groupId)
    {
        if (!GroupsDictionary.TryGetValue(groupId, out var groupInfo))
        {
            return [];
        }

        return [groupId, .. groupInfo.AllParentGroups];
    }

    public IEnumerable<CharacterGroupIdentification> GetParentGroupIdsIncludingThis(IEnumerable<CharacterGroupIdentification> groupIds)
    {
        return [.. groupIds.SelectMany(x => GetParentGroupIdsIncludingThis(x)).Distinct()];
    }

    public IEnumerable<CharacterGroupInfo> GetParentGroupsIncludingThis(IEnumerable<CharacterGroupIdentification> groupIds)
    {
        var ids = GetParentGroupIdsIncludingThis(groupIds);
        return ids.Select(id => GroupsDictionary[id]);
    }

    public IEnumerable<CharacterGroupInfo> GetDirectChildGroups(CharacterGroupIdentification groupId)
    {
        return GroupsDictionary[groupId].DirectChildGroupIds.Select(id => GroupsDictionary[id]);
    }

    /// <summary>
    /// Группы поддерева <paramref name="groupId"/> (включая саму группу) в порядке упорядоченного DFS.
    /// Порядок уже зашит в <see cref="CharacterGroupInfo.AllChildGroupsIncludingThis"/>: дочерние группы
    /// идут в порядке <c>ChildGroupsOrdering</c>, каждая группа — по первому вхождению. Персонажей здесь нет:
    /// чтобы получить детерминированный порядок персонажей, пройдите по этим группам и отсортируйте прямых
    /// персонажей каждой группы по её <see cref="CharacterGroupInfo.ChildCharactersOrdering"/>.
    /// </summary>
    public IReadOnlyList<CharacterGroupInfo> GetChildGroupsIncludingThis(CharacterGroupIdentification groupId)
    {
        if (!GroupsDictionary.TryGetValue(groupId, out var group))
        {
            return [];
        }
        return [.. group.AllChildGroupsIncludingThis.Select(id => GroupsDictionary[id])];
    }
}
