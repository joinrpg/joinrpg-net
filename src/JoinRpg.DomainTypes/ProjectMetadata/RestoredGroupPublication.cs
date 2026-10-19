namespace JoinRpg.DomainTypes.ProjectMetadata;

/// <summary>
/// Какие группы открыть, когда восстановление кладёт в них видимого всем персонажа (DRP, #5677).
/// </summary>
/// <remarks>
/// <para>
/// Восстановление создаёт потерянные группы скрытыми: какими они были, неизвестно. Видимый всем
/// персонаж в скрытой группе — значит, группа была публичной, и восстановление её открывает.
/// Открываются только группы, созданные восстановлением: скрытые группы из резервной копии скрыл
/// мастер, их не трогаем.
/// </para>
/// <para>
/// Открытая группа не должна нарушить <see cref="PublicGroupPathRule"/>: ей нужен публичный путь от
/// корня. Если его нет, открываются и закрытые восстановленные предки группы — сверху вниз, пока
/// открывается хоть кто-то. Если и так пути нет (по дороге — скрытая группа из копии), группа
/// остаётся скрытой вместе с предками, открытыми ради неё.
/// </para>
/// </remarks>
public static class RestoredGroupPublication
{
    /// <param name="tree">Дерево групп проекта.</param>
    /// <param name="characterGroupIds">Группы персонажа (прямые).</param>
    /// <param name="restoredGroupIds">Группы, созданные восстановлением.</param>
    /// <returns>Группы, которые надо сделать публичными, — в порядке, в котором их можно открывать.</returns>
    public static IReadOnlyList<CharacterGroupIdentification> GroupsToPublish(
        ProjectGroupTree tree,
        IReadOnlyCollection<CharacterGroupIdentification> characterGroupIds,
        IReadOnlySet<CharacterGroupIdentification> restoredGroupIds)
    {
        ArgumentNullException.ThrowIfNull(tree);
        ArgumentNullException.ThrowIfNull(characterGroupIds);
        ArgumentNullException.ThrowIfNull(restoredGroupIds);

        bool CanOpen(CharacterGroupInfo group)
            => group is { IsActive: true, IsPublic: false, GroupType: CharacterGroupType.Regular }
                && restoredGroupIds.Contains(group.Id);

        var published = new List<CharacterGroupIdentification>();
        foreach (var targetId in characterGroupIds.Distinct())
        {
            if (tree.GetGroupByIdOrDefault(targetId) is not { } target || !CanOpen(target))
            {
                continue;
            }

            // Самой группе путь уже есть — предков не трогаем.
            if (PublicGroupPathRule.AnyHasPublicPath(tree, target.DirectParentGroupIds))
            {
                tree = tree.WithGroupChange(target.Id, isPublic: true, target.DirectParentGroupIds);
                published.Add(target.Id);
                continue;
            }

            var attempt = tree;
            var opened = new List<CharacterGroupIdentification>();
            // При цикле в графе группа — сама себе предок.
            var pending = target.AllParentGroups
                .Where(id => id != target.Id)
                .Distinct()
                .Select(tree.GetGroupById)
                .Where(CanOpen)
                .Append(target)
                .ToList();
            var progress = true;
            while (progress)
            {
                progress = false;
                foreach (var group in pending.ToList())
                {
                    var parents = attempt.GetGroupById(group.Id).DirectParentGroupIds;
                    if (PublicGroupPathRule.AnyHasPublicPath(attempt, parents))
                    {
                        attempt = attempt.WithGroupChange(group.Id, isPublic: true, parents);
                        opened.Add(group.Id);
                        _ = pending.Remove(group);
                        progress = true;
                    }
                }
            }

            if (!opened.Contains(target.Id))
            {
                continue;
            }

            // Оставляем только тех, через кого к группе идёт публичный путь: предок, который открылся
            // сам по себе, но от которого до группы не дойти по публичным, открывать незачем.
            var onPath = PublicAncestorsOf(attempt, target.Id);
            foreach (var groupId in opened.Where(id => id == target.Id || onPath.Contains(id)))
            {
                tree = tree.WithGroupChange(groupId, isPublic: true, tree.GetGroupById(groupId).DirectParentGroupIds);
                published.Add(groupId);
            }
        }
        return published;
    }

    /// <summary>Предки группы, до которых от неё можно подняться только по публичным группам.</summary>
    private static HashSet<CharacterGroupIdentification> PublicAncestorsOf(ProjectGroupTree tree, CharacterGroupIdentification groupId)
    {
        var visited = new HashSet<CharacterGroupIdentification>();
        var queue = new Queue<CharacterGroupIdentification>([groupId]);
        while (queue.Count > 0)
        {
            foreach (var parentId in tree.GetGroupById(queue.Dequeue()).DirectParentGroupIds)
            {
                if (tree.GetGroupByIdOrDefault(parentId) is { IsActive: true, IsPublic: true } && visited.Add(parentId))
                {
                    queue.Enqueue(parentId);
                }
            }
        }
        return visited;
    }
}
