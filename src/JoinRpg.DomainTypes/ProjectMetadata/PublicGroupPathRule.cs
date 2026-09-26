namespace JoinRpg.DomainTypes.ProjectMetadata;

/// <summary>
/// У публичной группы должен быть хотя бы один публичный путь наверх — то есть путь до корня,
/// где публичны все группы по дороге.
/// </summary>
/// <remarks>
/// <para>
/// Иначе получается конфигурация, которой не должно существовать (issue #4878): снаружи группу
/// всё равно не видно, потому что до неё не дойти по публичному дереву, а внутри системы она
/// считается публичной — и персонажи в ней выглядят доступными для заявки.
/// </para>
/// <para>
/// «Путь», а не «цепочка родителей»: группы образуют граф, а не дерево — у группы может быть
/// несколько родителей. Достаточно, чтобы публичным был хоть один путь.
/// </para>
/// <para>
/// Корневая группа проекта публична по построению (<c>ProjectService.AddProject</c>), так что
/// путь всегда есть чем закончить.
/// </para>
/// </remarks>
public static class PublicGroupPathRule
{
    /// <summary>
    /// Публичные группы дерева, до которых не дойти по публичному пути от корня.
    /// </summary>
    /// <remarks>
    /// Спецгруппы пропускаем: их публичность задаётся вариантом поля, и со страницы группы её не
    /// исправить — ошибка была бы тупиковой. Неактивные группы тоже: удалённая группа снаружи не
    /// видна, чинить в ней нечего.
    /// </remarks>
    public static IReadOnlyCollection<CharacterGroupInfo> FindViolations(ProjectGroupTree tree)
    {
        ArgumentNullException.ThrowIfNull(tree);

        var reachable = FindPubliclyReachable(tree);

        return
        [
            .. tree.AllGroups
                .Where(group => group.IsActive && group.IsPublic && !group.IsSpecial)
                .Where(group => !reachable.Contains(group.Id))
        ];
    }

    /// <summary>
    /// Есть ли публичный путь от корня хотя бы до одной из перечисленных групп.
    /// </summary>
    /// <remarks>
    /// Нужно для ещё не созданной группы: её саму в дереве не найти, а вот родителей — можно.
    /// </remarks>
    public static bool AnyHasPublicPath(
        ProjectGroupTree tree,
        IReadOnlyCollection<CharacterGroupIdentification> groupIds)
    {
        ArgumentNullException.ThrowIfNull(tree);
        ArgumentNullException.ThrowIfNull(groupIds);

        var reachable = FindPubliclyReachable(tree);
        return groupIds.Any(reachable.Contains);
    }

    /// <summary>
    /// Группы, до которых есть публичный путь от корня: обход вниз от корня по публичным активным
    /// группам. Сверху вниз, а не снизу вверх, — так цикл в графе не мешает, он просто не добавит
    /// новых достижимых групп.
    /// </summary>
    private static HashSet<CharacterGroupIdentification> FindPubliclyReachable(ProjectGroupTree tree)
    {
        var reachable = new HashSet<CharacterGroupIdentification>();

        var root = tree.GetGroupByIdOrDefault(tree.RootGroupId);
        if (root is not { IsActive: true, IsPublic: true })
        {
            return reachable;
        }

        var queue = new Queue<CharacterGroupInfo>();
        _ = reachable.Add(root.Id);
        queue.Enqueue(root);

        while (queue.Count > 0)
        {
            foreach (var childId in queue.Dequeue().DirectChildGroupIds)
            {
                if (tree.GetGroupByIdOrDefault(childId) is not { IsActive: true, IsPublic: true } child)
                {
                    continue;
                }

                if (reachable.Add(child.Id))
                {
                    queue.Enqueue(child);
                }
            }
        }

        return reachable;
    }
}
